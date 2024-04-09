using Godot;
using Steamworks;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

namespace GameEvent;

[NetworkMessage(GameEventType.AgentStartUpload)]
public class AgentStartUpload : IGameEvent
{
    [Serialize]
    public CSteamID playerSteamID = CSteamID.Nil;

    [Serialize]
    public int uploadingKeyIndex = -1;

    public void OnReceiveClient()
    {
    }

    public void OnReceiveServer(CSteamID senderID)
    {

        if (Server.gameData.publicGameData.gameState != GameState.InGame)
        {
            GD.PrintErr($"Server: AgentStartUpload: Not in Game");
            return;
        }

        PlayerData playerData;
        if (senderID != playerSteamID || !Server.gameData.publicGameData.TryGetPlayerData(playerSteamID, out playerData))
        {
            GD.PrintErr($"Server: AgentStartUpload: invalid playerSteamID");
            return;
        }

        if(uploadingKeyIndex < 0 || uploadingKeyIndex >= GameConstants.keyCount)
        {
            GD.PrintErr($"Server: AgentStartUpload: invalid key index");
            return;
        }

        if (Server.gameData.publicGameData.keyStates[uploadingKeyIndex] != PublicKeyState.Hidden)
        {
            GD.PrintErr($"Server: AgentStartUpload: key has already been uploaded");
            return;
        }


        if (playerData.role != Role.Agent)
        {
            GD.PrintErr($"Server: AgentStartUpload: only agents can upload");
            return;
        }

        if (Server.gameData.publicGameData.uploadCooldown[playerData.team] > 0f)
        {
            GD.PrintErr($"Server: AgentStartUpload: team has upload cooldown");
            return;
        }

        if (Server.gameData.teamGameData[playerData.team].uploadingKeyIndex >= 0)
        {
            GD.PrintErr($"Server: AgentStartUpload: upload already in progress");
            return;
        }

        UpdateGameData(Server.gameData);
        Server.EmitMessage(this, Server.gameData.publicGameData.teamPlayers[playerData.team]);
    }

    public void UpdateGameData(GameData gameData)
    {
        Team team = gameData.publicGameData.playerData[playerSteamID].team;

        gameData.teamGameData[team].uploadingKeyIndex = uploadingKeyIndex;
        gameData.teamGameData[team].uploadingProgress = 0f;

    }
}

[NetworkMessage(GameEventType.AgentCancelUpload)]
public class AgentCancelUpload : IGameEvent
{
    [Serialize]
    public CSteamID playerSteamID;

    public void OnReceiveClient()
    {
    }

    public void OnReceiveServer(CSteamID senderID)
    {
        if (Server.gameData.publicGameData.gameState != GameState.InGame)
        {
            GD.PrintErr($"Server: AgentCancelUpload: Not in Game");
            return;
        }


        PlayerData playerData;
        if (senderID != playerSteamID || !Server.gameData.publicGameData.TryGetPlayerData(playerSteamID, out playerData))
        {
            GD.PrintErr($"Server: AgentStartUpload: invalid playerSteamID");
            return;
        }


        if (playerData.role != Role.Agent)
        {
            GD.PrintErr($"Server: AgentCancelUpload: role must be agent");
            return;
        }

        if (Server.gameData.teamGameData[playerData.team].uploadingKeyIndex < 0)
        {
            GD.PrintErr($"Server: AgentCancelUpload: team is not uploading");
            return;
        }

        UpdateGameData(Server.gameData);
        Server.EmitMessage(this, Server.gameData.publicGameData.teamPlayers[playerData.team]);
    }

    public void UpdateGameData(GameData gameData)
    {
        Team team = gameData.publicGameData.playerData[playerSteamID].team;
        gameData.teamGameData[team].uploadingKeyIndex = -1;
        gameData.teamGameData[team].uploadingProgress = 0f;
    }
}

[NetworkMessage(GameEventType.DirectorGiveHint)]
public class DirectorGiveHint : IGameEvent
{
    [Serialize]
    public string hintWord;

    [Serialize]
    public int hintNumber;

    [Serialize]
    public CSteamID playerSteamID;

    public static Regex hintWordRegex => new Regex($"^[A-Z]{{{GameConstants.minHintLength},{GameConstants.maxHintLength}}}$");

    public void OnReceiveClient()
    {
    }

    public void OnReceiveServer(CSteamID senderID)
    {
        //check gameState
        if (Server.gameData.publicGameData.gameState != GameState.InGame)
        {
            GD.PrintErr($"Server: DirectorGiveHint: Not in Game");
            return;
        }

        //check player
        PlayerData playerData;
        if (senderID != playerSteamID || !Server.gameData.publicGameData.TryGetPlayerData(playerSteamID, out playerData))
        {
            GD.PrintErr($"Server: AgentStartUpload: invalid playerSteamID");
            return;
        }


        //check role
        if (playerData.role != Role.Director)
        {
            GD.PrintErr($"Server: DirectorGiveHint: role must be director");
            return;
        }

        //check hints
        if(Server.gameData.publicGameData.hintsAvailable[playerData.team] <= 0)
        {
            GD.PrintErr($"Server: DirectorGiveHint: no hints available");
            return;
        }

        //check word
        if (!hintWordRegex.IsMatch(hintWord))
        {
            GD.PrintErr($"Server: DirectorGiveHint: word is invaid");
            return;
        }

        //check number
        if (hintNumber < 0 || hintNumber > 10)
        {
            GD.PrintErr($"Server: DirectorGiveHint: number is invaid");
            return;
        }

        UpdateGameData(Server.gameData);

        Server.gameData.gameRecapData.gameRecapEvents.Add(new GameRecapEvent()
        {
            eventString = $"{hintWord}_{hintNumber}",
            eventType = GameRecapEventType.Hint,
            team = playerData.team,
            timestamp = (Time.GetTicksUsec() - Server.gameData.gameStartTime) / 1000000f
        });

        Server.EmitMessage(this, Server.gameData.publicGameData.players);

    }

    public void UpdateGameData(GameData gameData)
    {
        PlayerData playerData = gameData.publicGameData.playerData[playerSteamID];

        gameData.publicGameData.hintsAvailable[playerData.team] -= 1;
        gameData.publicGameData.hints[playerData.team].Add($"{hintWord}_{hintNumber}");
    }
}

[NetworkMessage(GameEventType.AgentHover)]
public class AgentHover : IGameEvent
{
    [Serialize]
    public CSteamID playerSteamID;

    [Serialize]
    public int keyHoverIndex;

    public void OnReceiveClient()
    {
    }

    public void OnReceiveServer(CSteamID senderID)
    {
        if (Server.gameData.publicGameData.gameState != GameState.InGame)
        {
            GD.PrintErr($"Server: AgentHover: Not in Game");
            return;
        }

        PlayerData playerData;
        if (senderID != playerSteamID || !Server.gameData.publicGameData.TryGetPlayerData(playerSteamID, out playerData))
        {
            GD.PrintErr($"Server: AgentHover: invalid playerSteamID");
            return;
        }

        if (keyHoverIndex < 0 || keyHoverIndex >= GameConstants.keyCount)
        {
            GD.PrintErr($"Server: AgentHover: invalid key index");
            return;
        }

        if (Server.gameData.publicGameData.keyStates[keyHoverIndex] != PublicKeyState.Hidden)
        {
            GD.PrintErr($"Server: AgentHover: key has already been uploaded");
            return;
        }

        if (playerData.role != Role.Agent || playerData.team == Team.Spectator)
        {
            GD.PrintErr($"Server: AgentHover: only agents can hover");
            return;
        }

        if (Server.gameData.teamGameData[playerData.team].keyHovers[keyHoverIndex].Count >= GameConstants.maxHovers)
        {
            GD.PrintErr($"Server: AgentHover: maximum hovers already placed on key");
            return;
        }

        UpdateGameData(Server.gameData);

        Server.EmitMessage(this, Server.gameData.publicGameData.teamPlayers[playerData.team]);
    }

    public void UpdateGameData(GameData gameData)
    {
        if (gameData.publicGameData.TryGetPlayerData(playerSteamID, out PlayerData playerData))
        {
            HashSet<CSteamID> keyHovers = gameData.teamGameData[playerData.team].keyHovers[keyHoverIndex];

            if (keyHovers.Contains(playerSteamID))
            {
                keyHovers.Remove(playerSteamID);
            } else
            {
                keyHovers.Add(playerSteamID);
            }
        }
    }
}