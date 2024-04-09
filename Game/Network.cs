using Godot;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;

public enum NetworkDataType : byte
{
    Ping = 0x01,

    GameEventMin = 0x10,
    GameEventMax = 0x3F,

    ServerEventMin = 0x40,
    ServerEventMax = 0x6F,

    PublicGameData = 0x77,
    TeamGameData = 0x88,
    DirectorGameData = 0x99,

    GameRecapData = 0xAA,
}
public enum GameEventType : byte
{
    None = NetworkDataType.GameEventMin + 0x00,
    PlayerJoin = NetworkDataType.GameEventMin + 0x01,
    PlayerLeave = NetworkDataType.GameEventMin + 0x02,
    PlayerChangeTeamRole = NetworkDataType.GameEventMin + 0x03,

    HostStartGame = NetworkDataType.GameEventMin + 0x04,
    HostStopGame = NetworkDataType.GameEventMin + 0x05,
    HostBanUnbanPlayer = NetworkDataType.GameEventMin + 0x06,

    DirectorGiveHint = NetworkDataType.GameEventMin + 0x07,

    AgentStartUpload = NetworkDataType.GameEventMin + 0x08,
    AgentCancelUpload = NetworkDataType.GameEventMin + 0x09,
    AgentHover = NetworkDataType.GameEventMin + 0x10,

    PlayerMessage = NetworkDataType.GameEventMin + 0x11,

    DirectorChooseKey = NetworkDataType.GameEventMin + 0x20,
}


public enum ServerEventType : byte
{
    None = NetworkDataType.ServerEventMin + 0x00,
    TimerUpdate = NetworkDataType.ServerEventMin + 0x01,
    KeyUploadProgress = NetworkDataType.ServerEventMin + 0x02,
    KeyUploadComplete = NetworkDataType.ServerEventMin + 0x03,
    PingUpdate = NetworkDataType.ServerEventMin + 0x04,
}

public interface IServerMessage
{
    public void OnReceiveClient();
}

public interface IClientMessage
{
    public void OnReceiveServer(CSteamID senderID);

}

public interface IGameDataUpdater
{
    public void UpdateGameData(GameData gameData);
}
public interface IGameEvent : IClientMessage, IServerMessage, IGameDataUpdater
{
}

public interface IServerEvent : IGameDataUpdater, IServerMessage
{
}

public interface IOnDeserialize
{
    public object OnDeserialize();
}



public class NetworkMessageAttribute : SerializeAttribute
{
    public byte typeID;
    public NetworkMessageAttribute(object id)
    {
        typeID = (byte)id;
    }
}

public struct NetworkMessageData
{
    public object messageObject;
    public byte typeID;
    public string messageString;
}

[Autoload(400)]
public static class Network
{

    private static Dictionary<Type,byte> networkTypeIDs = new Dictionary<Type,byte>();
    private static Dictionary<byte, Serializer> networkSerializers = new Dictionary<byte, Serializer>();

    private static Serializer typeIDSerializer = new PrimitiveSerializer(typeof(byte));
    public static void _Ready()
    {
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            NetworkMessageAttribute networkSerializableAttribute = type.GetCustomAttribute<NetworkMessageAttribute>();
            if (networkSerializableAttribute == null)
            {
                continue;
            }

            if (networkSerializers.ContainsKey(networkSerializableAttribute.typeID))
            {
                throw new Exception($"NetworkSerializer: Multiple types use ID: {networkSerializableAttribute.typeID}");
            }
            networkTypeIDs[type] = networkSerializableAttribute.typeID;
            networkSerializers[networkSerializableAttribute.typeID] = Serializer.GetSerializer(type);
        }
    }

    public static byte[] SerializeMessage(object obj)
    {
        byte typeID;
        if (!networkTypeIDs.TryGetValue(obj.GetType(), out typeID))
        {
            return null;
        }

        Serializer serializer;
        if (!networkSerializers.TryGetValue(typeID, out serializer))
        {
            return null;
        }

        MemoryStream messageStream = new MemoryStream();

        typeIDSerializer.Serialize(typeID, messageStream);
        serializer.Serialize(obj, messageStream);
        return messageStream.ToArray();
    }

    public static void SendBytes(HSteamNetConnection hSteamNetConnection, byte[] messageBytes)
    {
        GCHandle messageHandle = GCHandle.Alloc(messageBytes, GCHandleType.Pinned);
        long output;
        SteamNetworkingSockets.SendMessageToConnection(hSteamNetConnection, messageHandle.AddrOfPinnedObject(), (uint)messageBytes.Length, 8, out output);
        messageHandle.Free();
    }

    public static void SendBytes(IEnumerable<HSteamNetConnection> hSteamNetConnections, byte[] messageBytes)
    {
        GCHandle messageHandle = GCHandle.Alloc(messageBytes, GCHandleType.Pinned);
        long output;

        foreach (HSteamNetConnection hSteamNetConnection in hSteamNetConnections)
        {
            SteamNetworkingSockets.SendMessageToConnection(hSteamNetConnection, messageHandle.AddrOfPinnedObject(), (uint)messageBytes.Length, 8, out output);
        }

        messageHandle.Free();
    }


    public static void SendMessage(HSteamNetConnection hSteamNetConnection, object obj)
    {
        SendBytes(hSteamNetConnection,SerializeMessage(obj));
    }

    public static void SendMessage(IEnumerable<HSteamNetConnection> hSteamNetConnections, object obj)
    {
        SendBytes(hSteamNetConnections,SerializeMessage(obj));
    }

    public static NetworkMessageData ReadMessage(IntPtr messagePointer)
    {
        SteamNetworkingMessage_t message = SteamNetworkingMessage_t.FromIntPtr(messagePointer);

        if (message.m_cbSize <= 0 || message.m_cbSize > 512 * 1024)
        {
            SteamNetworkingMessage_t.Release(messagePointer);
            throw new Exception($"Network: Bad Message Size: {message.m_cbSize}");
        }

        Span<byte> data;
        unsafe
        {
            data = new Span<byte>(message.m_pData.ToPointer(), message.m_cbSize);
        }

        MemoryStream stream = new MemoryStream(data.ToArray());

        byte typeID = (byte)typeIDSerializer.Deserialize(stream);

        Serializer serializer;
        if(!networkSerializers.TryGetValue(typeID,out serializer))
        {
            SteamNetworkingMessage_t.Release(messagePointer);
            throw new Exception($"Network: Couldn't Find Serializer for typeID: {typeID:X02}");
        }

        SteamNetworkingMessage_t.Release(messagePointer);
        object messageObject = serializer.Deserialize(stream);
        return new NetworkMessageData
        {
            messageObject = messageObject,
            typeID = typeID,
            messageString = serializer.SerializeString(messageObject,"")
        };
    }
}
