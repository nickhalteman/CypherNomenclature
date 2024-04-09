using Godot;
using System;
using Steamworks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

[Autoload(10)]
public static class Client
{

    internal static GameData gameData = new GameData();


    internal const int k_MessageBatchSize = 32;

    private static ulong reconnectPassword = 0;
    private static CSteamID reconnectID = CSteamID.Nil;
    private static SteamNetworkingIdentity hostIdentity = new SteamNetworkingIdentity();
    public static CSteamID hostSteamID => hostIdentity.GetSteamID();

    public static bool isHost => hostIdentity.GetSteamID() == SteamUser.GetSteamID();

    internal static HSteamNetConnection hostConnection { get; private set; } = HSteamNetConnection.Invalid;

    internal static event Action OnSecond;
    internal static event Action OnMinute;

    private static Dictionary<byte, HashSet<Action<object>>> networkMessageCallbacks = new Dictionary<byte, HashSet<Action<object>>>();
    private static Dictionary<ulong, Tuple<HashSet<byte>, Action<object>>> networkMessageCallbackHandles = new Dictionary<ulong, Tuple<HashSet<byte>, Action<object>>>();
    private static ulong networkMessageCallbackHandle = 42069;




    public static ulong RegisterNetworkMessageCallback(IEnumerable<byte> typeIDs, Action<object> callback)
    {
        while (networkMessageCallbackHandles.ContainsKey(networkMessageCallbackHandle))
        {
            networkMessageCallbackHandle++;
        }

        HashSet<byte> typeIDSet = new HashSet<byte>(typeIDs);

        networkMessageCallbackHandles[networkMessageCallbackHandle] = new Tuple<HashSet<byte>, Action<object>>(typeIDSet, callback);
        
        foreach(byte typeID in typeIDSet)
        {
            if (networkMessageCallbacks.ContainsKey(typeID))
            {
                networkMessageCallbacks[typeID].Add(callback);
            } else
            {
                networkMessageCallbacks[typeID] = new HashSet<Action<object>>()
                {
                    callback
                };
            }
        }
        return networkMessageCallbackHandle++;
    }

    public static ulong RegisterNetworkMessageCallback(byte typeIDs, Action<object> callback)
    {
        return RegisterNetworkMessageCallback(new HashSet<byte>() { typeIDs }, callback);
    }

    public static void ClearNetworkMessageCallbacks(ulong handle)
    {
        if (!networkMessageCallbackHandles.ContainsKey(handle)){
            return;
        }

        Tuple<HashSet<byte>, Action<object>> callbackData = networkMessageCallbackHandles[handle];

        foreach(byte typeID in callbackData.Item1)
        {
            if(networkMessageCallbacks.TryGetValue(typeID,out HashSet<Action<object>> callbacks)){
                callbacks.Remove(callbackData.Item2);
            }
        }
    }

    public static string connectionString {
        get {
            if(connectionState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
            {
                return $"+connect {hostIdentity.GetSteamID()} +pass {reconnectPassword:X}";
            } else
            {
                return "";
            }
        }
    }

    public static ESteamNetworkingConnectionState connectionState
    {
        get
        {
            SteamNetConnectionInfo_t clientConnectionInfo;
            if (SteamNetworkingSockets.GetConnectionInfo(hostConnection, out clientConnectionInfo))
            {
                return clientConnectionInfo.m_eState;
            } else
            {
                return ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None;
            }
        }
    }


    private static Callback<SteamNetConnectionStatusChangedCallback_t> m_SteamNetConnectionStatusChangedCallback;
    private static void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t pCallback)
    {
        if(pCallback.m_hConn != hostConnection)
        {
            return;
        }

        switch (pCallback.m_info.m_eState)
        {
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None:
                GD.Print($"ClientManager: Connection Updated!\n\tConnection: [{hostConnection}]\n\tState: None");
                SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_AppException_Generic, "Connection State: None", false);
                hostConnection = HSteamNetConnection.Invalid;
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
                GD.Print($"ClientManager: Connection Updated!\n\tConnection: [{hostConnection}]\n\tState: Finding Route");
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                GD.Print($"ClientManager: Connection Updated!\n\tConnection: [{hostConnection}]\n\tState: Connecting");
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                GD.Print($"ClientManager: Connection Updated!\n\tConnection: [{hostConnection}]\n\tState: Connected");
                Network.SendMessage(hostConnection, new GameEvent.PlayerJoin() { playerSteamID = SteamUser.GetSteamID(), password = reconnectPassword }) ;
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                GD.Print($"ClientManager: Connection Updated!\n\tConnection: [{hostConnection}]\n\tState: Closed By Peer");
                SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "Connection State: Closed By Peer", false);
                hostConnection = HSteamNetConnection.Invalid;
                HandleDisconnect();
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                GD.Print($"ClientManager: Connection Updated!\n\tConnection: [{hostConnection}]\n\tState: Problem Detected Locally");
                SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_AppException_Generic, "Connection State: Problem Detected Locally", false);
                hostConnection = HSteamNetConnection.Invalid;
                HandleDisconnect();
                break;
            default:
                GD.Print($"ClientManager: Connection Updated!\n\tConnection: [{hostConnection}]\n\tState: Invalid");
                SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_AppException_Generic, "Connection State: Invalid", false);
                hostConnection = HSteamNetConnection.Invalid;
                HandleDisconnect();
                break;
        }

        UpdateSteamRichPresence();
    }

    private static void HandleDisconnect()
    {
        switch (SceneManager.backScene)
        {
            case SceneManager.Scene.ConnectionProgress:
                break;
            case SceneManager.Scene.PreGameUI:
            case SceneManager.Scene.InGameUI:
            case SceneManager.Scene.PostGameUI:
                SceneManager.SetFrontScene(SceneManager.Scene.ConnectionLost);
                break;
        }
    }


    private static Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequestedCallback;
    private static void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t pCallback)
    {
        GD.Print($"ClientManager: Rich Pressence Join Requested\n\tm_rgchConnect: [{pCallback.m_rgchConnect}]");

        CheckConnectCommandLine(pCallback.m_rgchConnect);
    }

    private static Callback<NewUrlLaunchParameters_t> m_NewUrlLaunchParametersCallback;
    private static void OnNewUrlLaunchParameters(NewUrlLaunchParameters_t pCallback)
    {

        CheckLaunchCommandLine();
    }

    public static void Reconnect()
    {
        Connect(reconnectID,reconnectPassword);
    }

    public static void Connect(CSteamID host, ulong password)
    {


        if(!host.IsValid() || !host.BIndividualAccount())
        {
            return;
        }

        if(SteamUser.GetSteamID() != host && Server.isRunning)
        {
            Server.StopServer();
        }

        gameData = new GameData();

        hostIdentity.SetSteamID(host);
        reconnectID = host;
        reconnectPassword = password;

        //if there is an active client connection, close it.
        SteamNetConnectionInfo_t clientConnectionInfo;
        if (SteamNetworkingSockets.GetConnectionInfo(hostConnection, out clientConnectionInfo)) {
            SteamNetworkingSockets.CloseConnection(hostConnection, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "Connecting to new host", false);
            hostConnection = HSteamNetConnection.Invalid;
        }
        SteamNetworkingIdentity _hostIdentity = hostIdentity;
        hostConnection = SteamNetworkingSockets.ConnectP2P(ref _hostIdentity, 0, 0, new SteamNetworkingConfigValue_t[] { });

        GD.Print($"ClientManager: Connecting to remote host.\n\tSteamID: [{host}]\n\tConnection: [{hostConnection}]");

        SceneManager.SetBackScene(SceneManager.Scene.ConnectionProgress);

    }

    public static void Disconnect()
    {
        if(connectionState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
        {
            Network.SendMessage(hostConnection, new GameEvent.PlayerLeave() { playerSteamID = SteamUser.GetSteamID() });
            SteamNetworkingSockets.FlushMessagesOnConnection(hostConnection);
        }

        SteamNetConnectionInfo_t clientConnectionInfo;
        if (SteamNetworkingSockets.GetConnectionInfo(hostConnection, out clientConnectionInfo))
        {
            SteamNetworkingSockets.CloseConnection(hostConnection, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic, "Disconnecting", false);
            hostConnection = HSteamNetConnection.Invalid;
        }

        hostIdentity.Clear();
        Server.StopServer();


        SceneManager.SetBackScene(SceneManager.Scene.MainMenu);
        SceneManager.ClearFrontScene();
    }

    public static void UpdateSteamRichPresence()
    {

        if (!SteamFriends.SetRichPresence("status", $"Developing..."))
        {
            GD.PrintErr("ClientManager: Failed to set status key of rich presence");
        }

        if (!SteamFriends.SetRichPresence("steam_display", $"Developing..."))
        {
            GD.PrintErr("ClientManager: Failed to set status key of rich presence");
        }

        if (connectionState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
        {
            if (!SteamFriends.SetRichPresence("connect", connectionString))
            {
                GD.PrintErr("ClientManager: Failed to set connect key of rich presence");
            }
            if (!SteamFriends.SetRichPresence("steam_player_group", hostSteamID.ToString()))
            {
                GD.PrintErr("ClientManager: Failed to set connect key of rich presence");
            }
            if (!SteamFriends.SetRichPresence("steam_player_group_size", gameData.publicGameData.players.Count.ToString()))
            {
                GD.PrintErr("ClientManager: Failed to set connect key of rich presence");
            }
        } else
        {
            if (!SteamFriends.SetRichPresence("connect", ""))
            {
                GD.PrintErr("ClientManager: Failed to set connect key of rich presence");
            }
        }
    }


    private static void CheckLaunchCommandLine()
    {
        string commandLine;
        SteamApps.GetLaunchCommandLine(out commandLine, 128);
        GD.Print($"ClientManager: Launch Command Line:\n\t{commandLine}");

        CheckConnectCommandLine(commandLine);
    }

    private static void CheckConnectCommandLine(string commandLine)
    {
        Match connectMatch;

        if ((connectMatch = Regex.Match(commandLine, @"\+connect\s*(\d+)\s*\+pass\s*([0123456789aAbBcCdDeEfF]+)")).Success)
        {
            CSteamID hostID = (CSteamID)Convert.ToUInt64(connectMatch.Groups[1].Value);

            try
            {
                ulong pass = Convert.ToUInt64(connectMatch.Groups[2].Value, 16);
                GD.Print($"ClientManager: connect found in command line argument!\nAttempting to connect to [{hostID}] with password [{pass:X}]");
                Connect(hostID, pass);
            } catch(Exception e)
            {
                GD.PrintErr(e);
            }
        }
    }


    public static void _Ready()
    {
        m_SteamNetConnectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnSteamNetConnectionStatusChanged);
        m_GameRichPresenceJoinRequestedCallback = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);
        m_NewUrlLaunchParametersCallback = Callback<NewUrlLaunchParameters_t>.Create(OnNewUrlLaunchParameters);

        OnSecond += UpdateSteamRichPresence;

        SceneManager.SetBackScene(SceneManager.Scene.MainMenu);

        CheckLaunchCommandLine();
    }

    private static double lastMinute = 0;
    private static double lastSecond = 0;
    private static double deltaAccumulator = 0;

    private static HashSet<byte> noLogTypes = new HashSet<byte>() {
        (byte)ServerEventType.TimerUpdate,
        (byte)ServerEventType.KeyUploadProgress,
        (byte)NetworkDataType.Ping,
        (byte)ServerEventType.PingUpdate,
    };

    public static void _Process(double delta)
    {
        deltaAccumulator += delta;

        if(deltaAccumulator - lastMinute > 60)
        {
            lastMinute = deltaAccumulator;
            deltaAccumulator -= 60;
            OnMinute?.Invoke();
        }

        if (deltaAccumulator - lastSecond > 1)
        {
            lastSecond = deltaAccumulator;
            OnSecond?.Invoke();
        }


        if (connectionState != ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
        {
            return;
        }

        IntPtr[] newMessages = new IntPtr[k_MessageBatchSize];
        int newMessageCount = SteamNetworkingSockets.ReceiveMessagesOnConnection(hostConnection, newMessages, k_MessageBatchSize);

        foreach (IntPtr messagePointer in newMessages)
        {

            if (newMessageCount < 1)
            {
                break;
            }
            newMessageCount--;


            NetworkMessageData message;
            try
            {
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

            if(!noLogTypes.Contains(message.typeID))
            {
                GD.Print($"Client Received Message: [{type.Name}]\n" + message.messageString);
            }

            if (type.GetInterface(nameof(IGameDataUpdater)) != null)
            {
                (message.messageObject as IGameDataUpdater).UpdateGameData(gameData);
            }
            if (type.GetInterface(nameof(IServerMessage)) != null)
            {
                (message.messageObject as IServerMessage).OnReceiveClient();
            }

            if (networkMessageCallbacks.ContainsKey(message.typeID))
            {
                foreach(Action<object> callback in networkMessageCallbacks[message.typeID])
                {
                    callback(message.messageObject);
                }
            }

        }
    }
}