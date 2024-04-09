
using System;
using System.Collections.Generic;
using Godot;
using Steamworks;

namespace GameEvent;

[NetworkMessage(GameEventType.PlayerJoin)]
public sealed class PlayerJoin : IGameEvent
{
    [Serialize]
    public CSteamID playerSteamID = CSteamID.Nil;

    [Serialize]
    public ulong password = 0;

    public void OnReceiveClient()
    {
        GD.Print($"Client: PlayerJoin: [{playerSteamID}]");
    }

    public void OnReceiveServer(CSteamID senderID)
    {
        if (senderID != playerSteamID)
        {
            GD.PrintErr($"Server: PlayerJoin: senderID != playerID");
            return;
        }

        if(password != Server.password)
        {
            GD.PrintErr($"Server: PlayerJoin: password is incorrect");
            return;
        }

        if(Server.clientConnectionData.TryGetValue(senderID, out ClientConnectionData connectionData))
        {
            connectionData.isAuthenticated = true;


            if (Server.gameData.publicGameData.gameState != GameState.InGame || !Server.gameData.publicGameData.players.Contains(playerSteamID))
            {
                UpdateGameData(Server.gameData);
                Server.UpdateGameData(new CSteamID[] { playerSteamID });
                Server.EmitMessage(this);
            }
            else
            {
                Server.UpdateGameData(new CSteamID[] { playerSteamID });
            }
        } else
        {
            GD.PrintErr("Server: PlayerJoin: couldn't find player ClientConnectionData");
        }


    }

    public void UpdateGameData(GameData gameData)
    {
        if (!gameData.publicGameData.players.Contains(playerSteamID))
        {
            gameData.publicGameData.players.Add(playerSteamID);
            gameData.publicGameData.spectators.Add(playerSteamID);

            gameData.publicGameData.playerData[playerSteamID] = new PlayerData()
            {
                role = Role.Spectator,
                team = Team.Spectator,
            };
        }
    }
}

[NetworkMessage(GameEventType.PlayerLeave)]
public sealed class PlayerLeave : IGameEvent
{
    [Serialize]
    public CSteamID playerSteamID = CSteamID.Nil;

    public void OnReceiveClient()
    {
        GD.Print($"Client: PlayerLeave: [{playerSteamID}]");
    }

    public void OnReceiveServer(CSteamID senderID)
    {
        if (senderID != playerSteamID)
        {
            GD.PrintErr($"Server: PlayerLeave: senderID != playerID");
            return;
        }

        GD.Print($"Server: PlayerLeave: [{playerSteamID}]");

        if (Server.gameData.publicGameData.gameState != GameState.InGame)
        {
            UpdateGameData(Server.gameData);
            Server.EmitMessage(this);
        }

    }

    public void UpdateGameData(GameData gameData)
    {
        if (gameData.publicGameData.TryGetPlayerData(playerSteamID, out PlayerData playerData))
        {
            gameData.publicGameData.playerData.Remove(playerSteamID);
            gameData.publicGameData.players.Remove(playerSteamID);
            switch (playerData.role)
            {
                case Role.Spectator:
                    gameData.publicGameData.spectators.Remove(playerSteamID);
                    break;
                case Role.Agent:
                    gameData.publicGameData.agents[playerData.team].Remove(playerSteamID);
                    gameData.publicGameData.teamPlayers[playerData.team].Remove(playerSteamID);
                    break;
                case Role.Director:
                    gameData.publicGameData.directors[playerData.team] = CSteamID.Nil;
                    gameData.publicGameData.teamPlayers[playerData.team].Remove(playerSteamID);
                    break;
            }
        }
    }
}

[NetworkMessage(GameEventType.PlayerChangeTeamRole)]
public sealed class PlayerChangeTeamRole : IGameEvent
{
    [Serialize]
    public CSteamID playerSteamID = CSteamID.Nil;

    [Serialize]
    public Team team = Team.Spectator;

    [Serialize]
    public Role role = Role.Spectator;

    public void OnReceiveClient()
    {
        GD.Print($"Client: PlayerChangeTeamRole: [{playerSteamID}]");
    }

    public void OnReceiveServer(CSteamID senderID)
    {
        PlayerData playerData;
        if ((senderID != playerSteamID && senderID != SteamUser.GetSteamID()) || !Server.gameData.publicGameData.TryGetPlayerData(playerSteamID, out playerData))
        {
            GD.PrintErr($"Server: PlayerChangeTeamRole: Invalid playerSteamID");
            return;
        }

        //limit in game team swaps to spectators becoming agents
        if (Server.gameData.publicGameData.gameState == GameState.InGame && (role != Role.Agent || team == Team.Spectator || playerData.role != Role.Spectator || playerData.team != Team.Spectator))
        {
            GD.PrintErr($"Server: PlayerChangeTeamRole: Can only join Agent from Spectator during active Game");
            return;
        }

        if (Server.gameData.publicGameData.gameState == GameState.PostGame)
        {
            GD.PrintErr($"Server: PlayerChangeTeamRole: Not allowed after game.");
            return;
        }

        if (role == Role.Director && Server.gameData.publicGameData.directors[team].IsValid())
        {
            GD.PrintErr($"Server: PlayerChangeTeamRole: Director Spot Already Filled");
            return;
        }

        if(role == Role.Spectator ^ team == Team.Spectator)
        {
            GD.PrintErr($"Server: PlayerChangeTeamRole: Spectator must be consistent");
            return;
        }

        GD.Print($"Server: PlayerChangeTeamRole: [{playerSteamID}]");

        UpdateGameData(Server.gameData);
        Server.UpdateGameData(new CSteamID[] { playerSteamID });
        Server.EmitMessage(this);
    }

    public void UpdateGameData(GameData gameData)
    {
        PlayerData playerData;
        if (gameData.publicGameData.TryGetPlayerData(playerSteamID, out playerData))
        {
            switch (playerData.role)
            {
                case Role.Spectator:
                    gameData.publicGameData.spectators.Remove(playerSteamID);
                    break;
                case Role.Agent:
                    gameData.publicGameData.agents[playerData.team].Remove(playerSteamID);
                    gameData.publicGameData.teamPlayers[playerData.team].Remove(playerSteamID);
                    break;
                case Role.Director:
                    gameData.publicGameData.directors[playerData.team] = CSteamID.Nil;
                    gameData.publicGameData.teamPlayers[playerData.team].Remove(playerSteamID);
                    break;
            }

            playerData.team = team;
            playerData.role = role;

            switch (playerData.role)
            {
                case Role.Spectator:
                    gameData.publicGameData.spectators.Add(playerSteamID);
                    break;
                case Role.Agent:
                    gameData.publicGameData.agents[playerData.team].Add(playerSteamID);
                    gameData.publicGameData.teamPlayers[playerData.team].Add(playerSteamID);
                    break;
                case Role.Director:
                    gameData.publicGameData.directors[playerData.team] = playerSteamID;
                    gameData.publicGameData.teamPlayers[playerData.team].Add(playerSteamID);
                    break;
            }
        }
    }
}

