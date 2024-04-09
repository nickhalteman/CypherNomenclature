using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum GameRecapEventType : byte
{
    UploadKey = 0x0,
    UploadVirus = 0x1,
    UploadBomb = 0x2,
    Hint = 0x3,
}


[Serialize]
public class GameRecapEvent : IComparable<GameRecapEvent>
{
    [Serialize]
    public float timestamp;

    [Serialize]
    public Team team;

    [Serialize]
    public GameRecapEventType eventType;

    [Serialize]
    public string eventString;

    public int CompareTo(GameRecapEvent other)
    {
        return timestamp.CompareTo(other.timestamp);
    }
}

[NetworkMessage(NetworkDataType.GameRecapData)]
public class GameRecapData : IServerEvent
{
    [Serialize]
    public Dictionary<Team, CSteamID> directors = new Dictionary<Team, CSteamID>()
    {
        {Team.Home,CSteamID.Nil },
        {Team.Away,CSteamID.Nil },
    };

    [Serialize]
    public Dictionary<Team,List<CSteamID>> agents = new Dictionary<Team, List<CSteamID>>()
    {
        {Team.Home, new List<CSteamID>(){ } },
        {Team.Away, new List<CSteamID>(){ } },
    };

    [Serialize]
    public List<GameRecapEvent> gameRecapEvents = new List<GameRecapEvent>();

    [Serialize]
    public Team winningTeam;

    //unix time
    [Serialize]
    public double gameStartTime;

    //unix time
    [Serialize]
    public double gameEndTime;

    [Serialize]
    public Team[] keyOwners = new Team[GameConstants.keyCount]
    {
        Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
        Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
        Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
        Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
        Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
    };

    [Serialize]
    public PublicKeyState[] keyStates = new PublicKeyState[GameConstants.keyCount]
    {
        PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden,
        PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden,
        PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden,
        PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden,
        PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden,
    };

    [Serialize]
    public string[] keyWords = new string[GameConstants.keyCount]
    {
        "Aaron Judge","Aaron Judge","Aaron Judge","Aaron Judge","Aaron Judge",
        "Aaron Judge","Aaron Judge","Aaron Judge","Aaron Judge","Aaron Judge",
        "Aaron Judge","Aaron Judge","Aaron Judge","Aaron Judge","Aaron Judge",
        "Aaron Judge","Aaron Judge","Aaron Judge","Aaron Judge","Aaron Judge",
        "Aaron Judge","Aaron Judge","Aaron Judge","Aaron Judge","Aaron Judge",
    };
    
    [Serialize]
    public Dictionary<Team, int> keysFound = new Dictionary<Team, int>()
    {
        { Team.Home, 0 },
        { Team.Away, 0 }
    };

    public void OnReceiveClient()
    {
    }

    public void UpdateGameData(GameData gameData)
    {
        gameData.publicGameData.gameState = GameState.PostGame;

        gameData.gameRecapData = this;

        gameData.directorGameData.keyOwners = keyOwners;
        gameData.publicGameData.keyWords = keyWords;
        gameData.publicGameData.keyStates = keyStates;
        gameData.publicGameData.keysFound = keysFound;


    }
}