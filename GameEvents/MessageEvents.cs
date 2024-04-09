using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEvent;

[NetworkMessage(GameEventType.PlayerMessage)]
public class PlayerMessage : IGameEvent
{
    [Serialize]
    public CSteamID playerSteamID;

    [Serialize]
    public double time;

    [Serialize]
    public string text;

    public void OnReceiveClient()
    {
    }

    public void OnReceiveServer(CSteamID senderID)
    {
        PlayerData playerData;
        if (senderID != playerSteamID || !Server.gameData.publicGameData.TryGetPlayerData(playerSteamID, out playerData))
        {
            GD.PrintErr($"Server: PlayerMessage: Invalid playerSteamID");
            return;
        }


        text = text.Trim();

        if(text.Length <= 0 || text.Length > 256)
        {
            GD.PrintErr($"Server: PlayerMessage: Invalid messageLength");
            return;
        }

        if (playerData.team == Team.Spectator)
        {
            GD.PrintErr($"Server: PlayerMessage: Spectators cannot message");
            return;
        }

        time = Time.GetUnixTimeFromSystem();

        UpdateGameData(Server.gameData);
        
        if(playerData.role == Role.Agent)
        {
            Server.EmitMessage(this, Server.gameData.publicGameData.teamPlayers[playerData.team]);
        } else if (playerData.role == Role.Director)
        {
            Server.EmitMessage(this, Server.gameData.publicGameData.directors.Values);
        }
    }

    public void UpdateGameData(GameData gameData)
    {
        if(gameData.publicGameData.TryGetPlayerData(playerSteamID,out PlayerData playerData))
        {
            if(playerData.role == Role.Director)
            {
                gameData.directorMessageData.Add(this);
            } else if (playerData.role == Role.Agent)
            {
                gameData.teamMessageData[playerData.team].Add(this);
            }
        }
    }
}