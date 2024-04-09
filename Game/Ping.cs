using System;
using System.Collections.Generic;
using Steamworks;
using Godot;

[NetworkMessage(NetworkDataType.Ping)]
public class Ping : IClientMessage, IServerMessage
{
    static ulong nextData = 42069;

    [Serialize]
    public ulong data;

    private struct PingData
    {
        public CSteamID steamID;
        public ulong startTimestamp;
    }

    private static Dictionary<ulong, PingData> pingDataCache = new Dictionary<ulong, PingData>();

    static public void EmitServer()
    {
        foreach (CSteamID steamID in Server.gameData.publicGameData.players)
        {
            if (Server.clientConnectionData.TryGetValue(steamID, out ClientConnectionData connectionData))
            {
                Network.SendMessage(connectionData.connection, new Ping() { data = nextData });
                pingDataCache[nextData] = new PingData() {
                    steamID = steamID,
                    startTimestamp = Time.GetTicksUsec()
                };
                nextData++;
            }
            else
            {
                PingUpdate pingUpdate = new PingUpdate() {
                    ping = -1,
                    steamID = steamID
                };

                pingUpdate.UpdateGameData(Server.gameData);
                Server.EmitMessage(pingUpdate);
            }
        }
    }

    public void OnReceiveClient()
    {
        Network.SendMessage(Client.hostConnection, this);
    }

    public void OnReceiveServer(CSteamID senderID)
    {
        if (pingDataCache.TryGetValue(data,out PingData pingData))
        {
            if(pingData.steamID == senderID)
            {
                PingUpdate pingUpdate = new PingUpdate()
                {
                    steamID = senderID,
                    ping = (int)(Time.GetTicksUsec() - pingData.startTimestamp)
                };

                pingUpdate.UpdateGameData(Server.gameData);
                Server.EmitMessage(pingUpdate);
                
                pingDataCache.Remove(data);
            }
        }
    }
}

[NetworkMessage(ServerEventType.PingUpdate)]
public class PingUpdate : IServerEvent
{
    [Serialize]
    public int ping;

    [Serialize]
    public CSteamID steamID;

    public void UpdateGameData(GameData gameData)
    {
        if(gameData.publicGameData.TryGetPlayerData(steamID,out PlayerData playerData))
        {
            playerData.ping = ping;
        }
    }

    public void OnReceiveClient()
    {
    }
}