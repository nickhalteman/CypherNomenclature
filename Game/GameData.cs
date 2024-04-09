using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;

public enum Team : byte
{
    Spectator = 0,
    Home = 1,
    Away = 2
}

public enum Role : byte
{
    Spectator = 0,
    Agent = 1,
    Director = 2
}

public enum PublicKeyState : byte
{
    Hidden = 0,
    ShownHome = 1,
    ShownAway = 2,
    ShownNone = 3
}

public enum GameState : byte
{
    PreGame = 0,
    InGame = 1,
    PostGame = 2
}

public static class GameConstants
{
    public const int keyCount = 25;
    public const int minHintLength = 2;
    public const int maxHintLength = 16;

    public const int maxMessagesPerTick = 32;

    public const int winKeyCount = 9;

    public const float hintTimer = 90f;

    public const int maxHints = 2;

    public const float tickInterval = 0.05f;

    public const float uploadProgressPerTick = tickInterval / 3f;

    public const float uploadCooldown = 60f;

    public const int maxHovers = 6;

    public const ulong version = 1;
}

public class GameData
{
    public GameRecapData gameRecapData = new GameRecapData();

    public PublicGameData publicGameData = new PublicGameData();
    public Dictionary<Team, TeamGameData> teamGameData = new Dictionary<Team, TeamGameData>() {
        { Team.Home, new TeamGameData(){ team = Team.Home } },
        { Team.Away, new TeamGameData(){ team = Team.Away } },
    };

    public Dictionary<Team, List<GameEvent.PlayerMessage>> teamMessageData = new Dictionary<Team, List<GameEvent.PlayerMessage>>()
    {
        { Team.Home, new List<GameEvent.PlayerMessage> () },
        { Team.Away, new List<GameEvent.PlayerMessage> () }
    };
    public List<GameEvent.PlayerMessage> directorMessageData = new List<GameEvent.PlayerMessage>();

    public DirectorGameData directorGameData = new DirectorGameData();

    //microseconds since engine start
    public ulong gameStartTime = 0;
}