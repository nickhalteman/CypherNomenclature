using System;
using System.Collections.Generic;
using Steamworks;


namespace ServerEvent;

[NetworkMessage(ServerEventType.TimerUpdate)]
public class TimerUpdate : IServerEvent
{
    [Serialize]
    public Dictionary<Team, float> hintTimer = new Dictionary<Team, float>() {
        {Team.Home, 0 },
        {Team.Away, 0},
    };

    [Serialize]
    public Dictionary<Team, float> uploadCooldown = new Dictionary<Team, float>() {
        {Team.Home, 0 },
        {Team.Away, 0},
    };

    [Serialize]
    public Dictionary<Team, int> hintsAvailable = new Dictionary<Team, int>() {
        {Team.Home, 0 },
        {Team.Away, 0},
    };

    public void OnReceiveClient()
    {
    }

    public void UpdateGameData(GameData gameData)
    {
        foreach(Team team in new Team[] { Team.Home, Team.Away })
        {
            gameData.publicGameData.hintTimer[team] = hintTimer[team];
            gameData.publicGameData.uploadCooldown[team] = uploadCooldown[team];
            gameData.publicGameData.hintsAvailable[team] = hintsAvailable[team];
        }
    }
}

[NetworkMessage(ServerEventType.KeyUploadProgress)]
public class KeyUploadProgress : IServerEvent
{
    [Serialize]
    public Team team;

    [Serialize]
    public float uploadingProgress;

    [Serialize]
    public int uploadingKeyIndex;

    public void OnReceiveClient()
    {
    }

    public void UpdateGameData(GameData gameData)
    {
        gameData.teamGameData[team].uploadingProgress = uploadingProgress;
        gameData.teamGameData[team].uploadingKeyIndex = uploadingKeyIndex;
    }
}

[NetworkMessage(ServerEventType.KeyUploadComplete)]
public class KeyUploadComplete : IServerEvent
{
    [Serialize]
    public Team team;

    [Serialize]
    public int keyIndex;

    [Serialize]
    public PublicKeyState publicKeyState;

    public void OnReceiveClient()
    {
    }

    public void UpdateGameData(GameData gameData)
    {
        if (gameData.teamGameData.ContainsKey(team))
        {
            gameData.teamGameData[team].uploadingProgress = 0;
            gameData.teamGameData[team].uploadingKeyIndex = -1;
        }

        gameData.publicGameData.keyStates[keyIndex] = publicKeyState;

        if(publicKeyState == PublicKeyState.ShownNone)
        {
            gameData.publicGameData.uploadCooldown[team] = GameConstants.uploadCooldown;
        } else
        {
            gameData.publicGameData.keysFound[publicKeyState == PublicKeyState.ShownHome ? Team.Home : Team.Away]++;
        }

        foreach(Team iTeam in new Team[] {Team.Home, Team.Away })
        {
            if (gameData.teamGameData.TryGetValue(iTeam, out TeamGameData teamGameData))
            {
                teamGameData.keyHovers[keyIndex].Clear();
            }
        }
    }
}







