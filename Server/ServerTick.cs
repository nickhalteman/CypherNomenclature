using System;
using System.Collections.Generic;
using Godot;
using ServerEvent;
using Steamworks;

public static class ServerTick
{
    public static void OnSecond()
    {
        Ping.EmitServer();
    }

    public static void CheckConnections()
    {
        ulong tickTime = Time.GetTicksUsec();
        HashSet<CSteamID> disconnectedSteamIDs = new HashSet<CSteamID>();
        foreach (ClientConnectionData connectionData in Server.clientConnectionData.Values)
        {
            if (connectionData.isAuthenticated)
            {
                continue;
            }

            if (connectionData.connectedTime != ulong.MaxValue && tickTime - connectionData.connectedTime > 1000000)
            {
                SteamNetworkingSockets.SetConnectionPollGroup(connectionData.connection, HSteamNetPollGroup.Invalid);
                SteamNetworkingSockets.CloseConnection(connectionData.connection, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "User didn't authenticate in time", false);
                disconnectedSteamIDs.Add(connectionData.steamID);
            }

            if (connectionData.connectedTime == ulong.MaxValue && tickTime - connectionData.acceptTime > 1000000)
            {
                SteamNetworkingSockets.SetConnectionPollGroup(connectionData.connection, HSteamNetPollGroup.Invalid);
                SteamNetworkingSockets.CloseConnection(connectionData.connection, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "User didn't authenticate in time", false);
                disconnectedSteamIDs.Add(connectionData.steamID);
            }
        }

        foreach (CSteamID steamID in disconnectedSteamIDs)
        {
            Server.clientConnectionData.Remove(steamID);
        }
    }


    public static void OnTick()
    {
        //everything below here only happens in game
        if(Server.gameData.publicGameData.gameState != GameState.InGame) { return; }

        PublicGameData publicGameData = Server.gameData.publicGameData;
        DirectorGameData directorGameData = Server.gameData.directorGameData;

        TimerUpdate timerUpdate = new TimerUpdate();

        foreach (Team team in new Team[] { Team.Home, Team.Away })
        {
            TeamGameData teamGameData = Server.gameData.teamGameData[team];

            //process the hint timer
            if(publicGameData.hintsAvailable[team] < GameConstants.maxHints)
            {
                float hintTimer = publicGameData.hintTimer[team] - GameConstants.tickInterval;

                if (hintTimer <= 0f)
                {
                    timerUpdate.hintTimer[team] = GameConstants.hintTimer;
                    timerUpdate.hintsAvailable[team] = Math.Min(publicGameData.hintsAvailable[team] + 1, GameConstants.maxHints);
                }
                else
                {
                    timerUpdate.hintTimer[team] = hintTimer;
                    timerUpdate.hintsAvailable[team] = publicGameData.hintsAvailable[team];
                }
            } else
            {
                timerUpdate.hintTimer[team] = GameConstants.hintTimer;
                timerUpdate.hintsAvailable[team] = GameConstants.maxHints;
            }

            //process the upload cooldown
            float uploadCooldown = publicGameData.uploadCooldown[team];
            if (uploadCooldown > 0f)
            {
                timerUpdate.uploadCooldown[team] = Math.Max(uploadCooldown - GameConstants.tickInterval, 0f);
            } else
            {
                timerUpdate.uploadCooldown[team] = 0f;
            }

            //process key uploads
            if (teamGameData.uploadingKeyIndex >= 0)
            {
                KeyUploadProgress keyUploadProgress;

                //check if the key they are uploading is still available
                if (Server.gameData.publicGameData.keyStates[teamGameData.uploadingKeyIndex] == PublicKeyState.Hidden)
                {
                    keyUploadProgress = new KeyUploadProgress()
                    {
                        team = team,
                        uploadingKeyIndex = teamGameData.uploadingKeyIndex,
                        uploadingProgress = teamGameData.uploadingProgress + GameConstants.uploadProgressPerTick
                    };
                } else
                {

                    //the other team already uploaded this key first
                    keyUploadProgress = new KeyUploadProgress()
                    {
                        team = team,
                        uploadingKeyIndex = -1,
                        uploadingProgress = 0f
                    };
                }

                //upload complete
                if (keyUploadProgress.uploadingProgress >= 1f)
                {
                    PublicKeyState publicKeyState;
                    Team keyOwner = directorGameData.keyOwners[teamGameData.uploadingKeyIndex];
                    switch (keyOwner)
                    {
                        case Team.Home:
                            publicKeyState = PublicKeyState.ShownHome;
                            break;
                        case Team.Away:
                            publicKeyState = PublicKeyState.ShownAway;
                            break;
                        default:
                            publicKeyState = PublicKeyState.ShownNone;
                            break;
                    }

                    if(keyOwner == Team.Spectator)
                    {
                        timerUpdate.uploadCooldown[team] = GameConstants.uploadCooldown;
                    }

                    //add recap event
                    GameRecapEventType gameRecapEventType;
                    if (keyOwner == Team.Spectator)
                    {
                        gameRecapEventType = GameRecapEventType.UploadBomb;
                    }
                    else if (team == keyOwner)
                    {
                        gameRecapEventType = GameRecapEventType.UploadKey;
                    }
                    else
                    {
                        gameRecapEventType = GameRecapEventType.UploadVirus;
                    }

                    Server.gameData.gameRecapData.gameRecapEvents.Add(new GameRecapEvent()
                    {
                        eventString = Server.gameData.publicGameData.keyWords[teamGameData.uploadingKeyIndex],
                        eventType = gameRecapEventType,
                        team = team,
                        timestamp = (Time.GetTicksUsec() - Server.gameData.gameStartTime) / 1000000f
                    });

                    //send event
                    IServerEvent uploadComplete = new KeyUploadComplete()
                    {
                        keyIndex = teamGameData.uploadingKeyIndex,
                        publicKeyState = publicKeyState,
                        team = team
                    };

                    uploadComplete.UpdateGameData(Server.gameData);
                    Server.EmitMessage(uploadComplete);


                    CheckWinCondition();

                } else
                {
                    keyUploadProgress.UpdateGameData(Server.gameData);
                    Server.EmitMessage(keyUploadProgress, publicGameData.teamPlayers[team]);
                }
            }
        }

        timerUpdate.UpdateGameData(Server.gameData);
        Server.EmitMessage(timerUpdate);
    }



    public static void CheckWinCondition()
    {
        foreach (Team winTeam in new Team[] { Team.Home, Team.Away })
        {
            if (Server.gameData.publicGameData.keysFound[winTeam] >= GameConstants.winKeyCount)
            {
                //mark win
                Server.gameData.gameRecapData.gameEndTime = Time.GetUnixTimeFromSystem();
                Server.gameData.gameRecapData.winningTeam = winTeam;

                //fill in recap players
                Server.gameData.gameRecapData.directors[Team.Home] = Server.gameData.publicGameData.directors[Team.Home];
                Server.gameData.gameRecapData.directors[Team.Away] = Server.gameData.publicGameData.directors[Team.Away];

                foreach (CSteamID homeAgent in Server.gameData.publicGameData.agents[Team.Home])
                {
                    Server.gameData.gameRecapData.agents[Team.Home].Add(homeAgent);
                }

                foreach (CSteamID awayAgent in Server.gameData.publicGameData.agents[Team.Away])
                {
                    Server.gameData.gameRecapData.agents[Team.Away].Add(awayAgent);
                }

                //fill in board data
                Server.gameData.gameRecapData.keyOwners = Server.gameData.directorGameData.keyOwners;
                Server.gameData.gameRecapData.keyStates = Server.gameData.publicGameData.keyStates;
                Server.gameData.gameRecapData.keyWords = Server.gameData.publicGameData.keyWords;
                Server.gameData.gameRecapData.keysFound = Server.gameData.publicGameData.keysFound;

                //send message
                Server.gameData.gameRecapData.UpdateGameData(Server.gameData);
                Server.EmitMessage(Server.gameData.gameRecapData);

                //list players who disconnected during game
                HashSet<CSteamID> disconnectedClients = new HashSet<CSteamID>();
                foreach (CSteamID playerID in Server.gameData.publicGameData.players)
                {
                    if (!Server.clientConnectionData.ContainsKey(playerID))
                    {
                        disconnectedClients.Add(playerID);
                    }
                }

                //disconnect players
                foreach (CSteamID playerID in disconnectedClients)
                {
                    GameEvent.PlayerLeave leaveEvent = new GameEvent.PlayerLeave()
                    {
                        playerSteamID = playerID,
                    };
                    leaveEvent.UpdateGameData(Server.gameData);
                    Server.EmitMessage(leaveEvent);
                }
            }
        }
    }

}
