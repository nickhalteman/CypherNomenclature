using Godot;
using System;
using Steamworks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GameEvent;
using System.Threading.Tasks.Dataflow;


public class ClientConnectionData
{
    public HSteamNetConnection connection;
    public CSteamID steamID;
    public bool isAuthenticated;
    public ulong connectedTime;
    public ulong acceptTime;
}

[Autoload(10)]
public static class Server
{

    internal static GameData gameData = new GameData();

    internal static bool isRunning { get; private set; } = false;

    internal static HSteamListenSocket hSteamListenSocket = HSteamListenSocket.Invalid;
    internal static HSteamNetPollGroup hSteamNetPollGroup = HSteamNetPollGroup.Invalid;

    internal static Dictionary<CSteamID,ClientConnectionData> clientConnectionData = new Dictionary<CSteamID,ClientConnectionData>();

    internal static HashSet<CSteamID> bannedClients = new HashSet<CSteamID>();

    internal static List<string> wordList = new List<string>();


    internal static ulong password = 42069;

    internal static void EmitMessage(object message, IEnumerable<CSteamID> recipients = null)
    {
        if (recipients == null)
        {
            recipients = gameData.publicGameData.playerData.Keys;
        }
        List<HSteamNetConnection> recipientConnections = new List<HSteamNetConnection>();

        foreach (CSteamID recipient in recipients)
        {
            ClientConnectionData connectionData;
            if (!clientConnectionData.TryGetValue(recipient, out connectionData))
            {
                continue;
            }

            recipientConnections.Add(connectionData.connection);
        }

        Network.SendMessage(recipientConnections, message);
    }

    internal static void UpdateGameData(IEnumerable<CSteamID> clients)
    {
        byte[] publicGameData = Network.SerializeMessage(gameData.publicGameData);
        Dictionary<Team, byte[]> teamGameData = new Dictionary<Team, byte[]>()
        {
            {Team.Home, Network.SerializeMessage(gameData.teamGameData[Team.Home]) },
            {Team.Away, Network.SerializeMessage(gameData.teamGameData[Team.Away]) }
        };

        byte[] directorGameData = Network.SerializeMessage(gameData.directorGameData);

        byte[] gameRecapData = Network.SerializeMessage(gameData.gameRecapData);

        clients = clients == null ? gameData.publicGameData.players : clients;

        foreach (CSteamID client in clients)
        {

            ClientConnectionData connectionData;
            if (!clientConnectionData.TryGetValue(client, out connectionData) || !connectionData.isAuthenticated)
            {
                continue;
            }

            Network.SendBytes(connectionData.connection, publicGameData);

            PlayerData playerData;
            if (!Client.gameData.publicGameData.TryGetPlayerData(client, out playerData))
            {
                continue;
            }

            if (playerData.team != Team.Spectator)
            {
                Network.SendBytes(connectionData.connection, teamGameData[playerData.team]);
            }
            if(playerData.role == Role.Director)
            {
                Network.SendBytes(connectionData.connection, directorGameData);
            }

            if(gameData.publicGameData.gameState == GameState.PostGame)
            {
                Network.SendBytes(connectionData.connection, gameRecapData);
            }
        }
    }

    internal static void StartServer()
    {

        if (isRunning)
        {
            return;
        }

        isRunning = true;

        if (hSteamListenSocket == HSteamListenSocket.Invalid)
        {
            hSteamListenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, new SteamNetworkingConfigValue_t[] { });
        }

        gameData = new GameData();

        hSteamNetPollGroup = SteamNetworkingSockets.CreatePollGroup();

        password = Extensions.RandomULong();

        Client.Connect(SteamUser.GetSteamID(), password);

    }

    internal static void StopServer()
    {

        GD.Print("ServerManager: Stopping Server!");

        isRunning = false;

        if (hSteamListenSocket != HSteamListenSocket.Invalid)
        {
            SteamNetworkingSockets.CloseListenSocket(hSteamListenSocket);
            hSteamListenSocket = HSteamListenSocket.Invalid;
        }

        if(hSteamNetPollGroup != HSteamNetPollGroup.Invalid)
        {
            SteamNetworkingSockets.DestroyPollGroup(hSteamNetPollGroup);
            hSteamNetPollGroup = HSteamNetPollGroup.Invalid;
        }

        foreach (ClientConnectionData connectionData in clientConnectionData.Values)
        {
            SteamNetConnectionInfo_t connectionInfo;
            if (SteamNetworkingSockets.GetConnectionInfo(connectionData.connection, out connectionInfo))
            {
                SteamNetworkingSockets.SetConnectionPollGroup(connectionData.connection, HSteamNetPollGroup.Invalid);
                SteamNetworkingSockets.CloseConnection(connectionData.connection, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "Server is closing", false);
            }
        }
        clientConnectionData.Clear();
    }

    private static Callback<SteamNetConnectionStatusChangedCallback_t> m_SteamNetConnectionStatusChangedCallback;
    private static void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t pCallback)
    {
        //ignore the client's changes
        if (pCallback.m_hConn == Client.hostConnection)
        {
            return;
        }

        CSteamID remoteID = (CSteamID)pCallback.m_info.m_identityRemote.GetSteamID64();

        if (!remoteID.BIndividualAccount())
        {
            SteamNetworkingSockets.SetConnectionPollGroup(pCallback.m_hConn, HSteamNetPollGroup.Invalid);
            SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "User is not a valid", false);
            clientConnectionData.Remove(remoteID);
            return;
        }

        if (bannedClients.Contains(remoteID))
        {
            SteamNetworkingSockets.SetConnectionPollGroup(pCallback.m_hConn, HSteamNetPollGroup.Invalid);
            SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "User has been banned", false);
            clientConnectionData.Remove(remoteID);
            return;
        }

        //new connection
        if (pCallback.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None &&
            pCallback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
        {

            //if there is an existing connection, close it before starting a new one
            if (clientConnectionData.TryGetValue(remoteID, out ClientConnectionData oldConnectionData))
            {
                SteamNetConnectionInfo_t connectionInfo;
                if (SteamNetworkingSockets.GetConnectionInfo(oldConnectionData.connection, out connectionInfo))
                {
                    SteamNetworkingSockets.SetConnectionPollGroup(oldConnectionData.connection, HSteamNetPollGroup.Invalid);
                    SteamNetworkingSockets.CloseConnection(oldConnectionData.connection, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "Establishing New Connnection", false);
                    oldConnectionData.connection = HSteamNetConnection.Invalid;
                }
                GD.Print($"ServerManager: Client is reconnecting.\n\tSteamID: [{remoteID}]\n\tConnection: [{pCallback.m_hConn}]");
            }
            else
            {
                GD.Print($"ServerManager: New client.\n\tSteamID: [{remoteID}]\n\tConnection: [{pCallback.m_hConn}]");
            }

            //new connection
            SteamNetworkingSockets.SetConnectionPollGroup(pCallback.m_hConn, hSteamNetPollGroup);
            SteamNetworkingSockets.AcceptConnection(pCallback.m_hConn);

            clientConnectionData[remoteID] = new ClientConnectionData()
            {
                connection = pCallback.m_hConn,
                steamID = remoteID,
                isAuthenticated = false,
                acceptTime = Time.GetTicksUsec(),
                connectedTime = ulong.MaxValue,
            };
        }

        ClientConnectionData connectionData;
        if (!clientConnectionData.TryGetValue(remoteID, out connectionData))
        {
            SteamNetworkingSockets.SetConnectionPollGroup(pCallback.m_hConn, HSteamNetPollGroup.Invalid);
            SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "Connection: State Closed By Peer", false);
            return;
        }


        switch (pCallback.m_info.m_eState)
        {
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None:
                GD.Print($"ServerManager: Connection Updated!\n\tSteamID: [{remoteID}]\n\tConnection: [{pCallback.m_hConn}]\n\tState: None");
                SteamNetworkingSockets.SetConnectionPollGroup(pCallback.m_hConn, HSteamNetPollGroup.Invalid);
                SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_AppException_Generic, "Connection State None", false);
                if (connectionData.isAuthenticated)
                {
                    (new GameEvent.PlayerLeave() { playerSteamID = remoteID }).OnReceiveServer(remoteID);
                }
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                GD.Print($"ServerManager: Connection Updated!\n\tSteamID: [{remoteID}]\n\tConnection: [{pCallback.m_hConn}]\n\tState: Connecting");
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
                GD.Print($"ServerManager: Connection Updated!\n\tSteamID: [{remoteID}]\n\tConnection: [{pCallback.m_hConn}]\n\tState: Finding Route");
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                GD.Print($"ServerManager: Connection Updated!\n\tSteamID: [{remoteID}]\n\tConnection: [{pCallback.m_hConn}]\n\tState: Connected");
                connectionData.connectedTime = Time.GetTicksUsec();
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                GD.Print($"ServerManager: Connection Updated!\n\tSteamID: [{remoteID}]\n\tConnection: [{pCallback.m_hConn}]\n\tState: Closed By Peer");
                SteamNetworkingSockets.SetConnectionPollGroup(pCallback.m_hConn, HSteamNetPollGroup.Invalid);
                SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "Connection: State Closed By Peer", false);
                if (connectionData.isAuthenticated)
                {
                    (new GameEvent.PlayerLeave() { playerSteamID = remoteID }).OnReceiveServer(remoteID);
                }
                clientConnectionData.Remove(remoteID);
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                GD.Print($"ServerManager: Connection Updated!\n\tSteamID: [{remoteID}]\n\tConnection: [{pCallback.m_hConn}]\n\tState: Problem Detected Locally");
                SteamNetworkingSockets.SetConnectionPollGroup(pCallback.m_hConn, HSteamNetPollGroup.Invalid);
                SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "Connection State: Problem Detected Locally", false);
                if (connectionData.isAuthenticated)
                {
                    (new GameEvent.PlayerLeave() { playerSteamID = remoteID }).OnReceiveServer(remoteID);
                }
                clientConnectionData.Remove(remoteID);
                break;
            default:
                GD.Print($"ServerManager: Connection Updated!\n\tSteamID: [{remoteID}]\n\tConnection: [{pCallback.m_hConn}]\n\tState: Invalid");
                SteamNetworkingSockets.SetConnectionPollGroup(pCallback.m_hConn, HSteamNetPollGroup.Invalid);
                SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_AppException_Generic, "Connection State: Invalid", false);
                if (connectionData.isAuthenticated)
                {
                    (new GameEvent.PlayerLeave() { playerSteamID = remoteID }).OnReceiveServer(remoteID);
                }
                clientConnectionData.Remove(remoteID);
                break;
        }
    }

    public static void _Ready()
    {
        m_SteamNetConnectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnSteamNetConnectionStatusChanged);
        SteamNetworkingUtils.InitRelayNetworkAccess();

        FileAccess wordListFile = FileAccess.Open("res://Game/wordlist.txt", Godot.FileAccess.ModeFlags.Read);

        string line;
        while((line = wordListFile.GetLine()) != null && line.Length != 0)
        {
            if((line = line.Trim()).Length == 0)
            {
                continue;
            }
            wordList.Add(line);
        }
    }

    private static ulong nextTick = ulong.MaxValue;
    private static ulong nextSecond = ulong.MaxValue;


    private static HashSet<byte> noLogTypes = new HashSet<byte>() {
        (byte)NetworkDataType.Ping,
    };
    public static void _Process(double delta)
    {
        if (!isRunning)
        {
            return;
        }

        ulong currentTime = Time.GetTicksUsec();

        IntPtr[] newMessages = new IntPtr[GameConstants.maxMessagesPerTick];

        int messageCount = SteamNetworkingSockets.ReceiveMessagesOnPollGroup(hSteamNetPollGroup, newMessages, GameConstants.maxMessagesPerTick);

        foreach (IntPtr messagePointer in newMessages)
        {
            if (messageCount < 1)
            {
                break;
            }
            messageCount--;

            NetworkMessageData message;
            CSteamID senderID;
            try
            {
                senderID = SteamNetworkingMessage_t.FromIntPtr(messagePointer).m_identityPeer.GetSteamID();
                message = Network.ReadMessage(messagePointer);
            }
            catch (Exception e)
            {
                GD.PrintErr("Client: Error Deserializing Message");
                GD.PrintErr(e);
                break;
            }

            if (message.messageObject == null || message.messageString == null)
            {
                continue;
            }

            Type type = message.messageObject.GetType();


            if (!noLogTypes.Contains(message.typeID))
            {
                GD.Print($"Server Received Message: [{type.Name}]\n" + message.messageString);
            }




            if (type.GetInterface(nameof(IClientMessage)) != null)
            {
                (message.messageObject as IClientMessage).OnReceiveServer(senderID);
            }
        }

        if (nextTick == ulong.MaxValue || currentTime >= nextTick)
        {
            nextTick += (ulong)(GameConstants.tickInterval * 1000000f);
            ServerTick.OnTick();
        }

        if(nextSecond == ulong.MaxValue || currentTime >= nextSecond)
        {
            nextSecond += 1000000;
            ServerTick.OnSecond();
        }
    }
}