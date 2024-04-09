using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Steamworks;


namespace GameEvent;

[NetworkMessage(GameEventType.HostStartGame)]
public class HostStartGame : IGameEvent
{
    [Serialize]
    public string[] keyWords = new string[GameConstants.keyCount]
    {
        "AARON_JUDGE","AARON_JUDGE","AARON_JUDGE","AARON_JUDGE","AARON_JUDGE",
        "AARON_JUDGE","AARON_JUDGE","AARON_JUDGE","AARON_JUDGE","AARON_JUDGE",
        "AARON_JUDGE","AARON_JUDGE","AARON_JUDGE","AARON_JUDGE","AARON_JUDGE",
        "AARON_JUDGE","AARON_JUDGE","AARON_JUDGE","AARON_JUDGE","AARON_JUDGE",
        "AARON_JUDGE","AARON_JUDGE","AARON_JUDGE","AARON_JUDGE","AARON_JUDGE",
    };

    public void OnReceiveClient()
    {
        SceneManager.SetBackScene(SceneManager.Scene.InGameUI);
        SceneManager.ClearFrontScene();
    }

    public void OnReceiveServer(CSteamID senderID)
    {

        if(senderID != SteamUser.GetSteamID())
        {
            GD.PrintErr($"HostStartGame: senderID != hostID");
            return;
        }

        keyWords = Server.wordList.RandomChoice(GameConstants.keyCount);
        UpdateGameData(Server.gameData);

        HashSet<int> keys = new HashSet<int>();
        for(int i = 0; i < GameConstants.keyCount; i++)
        {
            keys.Add(i);
        }

        int[] homeKeys = keys.PopRandom(GameConstants.winKeyCount);
        int[] awayKeys = keys.PopRandom(GameConstants.winKeyCount);

        Team[] keyOwners = new Team[GameConstants.keyCount]
        {
            Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
            Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
            Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
            Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
            Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
        };

        foreach(int homeIndex in homeKeys)
        {
            keyOwners[homeIndex] = Team.Home;
        }

        foreach(int awayIndex in awayKeys)
        {
            keyOwners[awayIndex] = Team.Away;
        }

        
        Server.gameData.directorGameData = new DirectorGameData()
        {
            keyOwners = keyOwners,
        };

        Server.gameData.gameStartTime = Time.GetTicksUsec();

        Server.EmitMessage(this);
        Server.EmitMessage(Server.gameData.directorGameData, Server.gameData.publicGameData.directors.Values);

        GD.Print($"Sending directorGameData:\n{Serializer.GetSerializer<DirectorGameData>().SerializeString(Server.gameData.directorGameData, "")}");
        


        Server.EmitMessage(Server.gameData.teamGameData[Team.Home], Server.gameData.publicGameData.teamPlayers[Team.Home]);
        Server.EmitMessage(Server.gameData.teamGameData[Team.Away], Server.gameData.publicGameData.teamPlayers[Team.Away]);

    }

    public void UpdateGameData(GameData gameData)
    {
        gameData.publicGameData.gameState = GameState.InGame;
        gameData.publicGameData.keyWords = keyWords;
        gameData.publicGameData.keysFound = new Dictionary<Team, int>()
        {
            { Team.Home, 0 },
            { Team.Away, 0 },
        };
        gameData.publicGameData.hints = new Dictionary<Team, List<string>>()
        {
            { Team.Home, new List<string>() },
            { Team.Away, new List<string>() },
        };
        gameData.publicGameData.hintsAvailable = new Dictionary<Team, int>
        {
            { Team.Home, 0 },
            { Team.Away, 0 },
        };
        gameData.publicGameData.hintTimer = new Dictionary<Team, float>()
        {
            { Team.Home, GameConstants.hintTimer },
            { Team.Away, GameConstants.hintTimer },
        };
        gameData.publicGameData.keyStates = new PublicKeyState[]
        {
            PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden,
            PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden,
            PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden,
            PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden,
            PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden, PublicKeyState.Hidden,
        };
        gameData.publicGameData.uploadCooldown = new Dictionary<Team, float>()
        {
            { Team.Home, 0 },
            { Team.Away, 0 },
        };

        gameData.directorMessageData = new List<PlayerMessage>();

        gameData.teamMessageData = new Dictionary<Team, List<PlayerMessage>>()
        {
            {Team.Home, new List<PlayerMessage>() },
            {Team.Away, new List<PlayerMessage>() },
        };

        gameData.teamGameData = new Dictionary<Team, TeamGameData>() {
            { Team.Home, new TeamGameData(){ team = Team.Home } },
            { Team.Away, new TeamGameData(){ team = Team.Away } },
        };

        gameData.directorGameData = new DirectorGameData();

        gameData.gameRecapData = new GameRecapData() { gameStartTime = Time.GetUnixTimeFromSystem() };

    }
}


[NetworkMessage(GameEventType.HostStopGame)]
public class HostStopGame : IGameEvent
{

    public void OnReceiveClient()
    {
        SceneManager.SetBackScene(SceneManager.Scene.PreGameUI);
        SceneManager.ClearFrontScene();
    }

    public void OnReceiveServer(CSteamID senderID)
    {
        if (senderID != SteamUser.GetSteamID())
        {
            GD.PrintErr($"HostStopGame: senderID != hostID");
            return;
        }

        UpdateGameData(Server.gameData);

        Server.EmitMessage(this);
    }

    public void UpdateGameData(GameData gameData)
    {
        gameData.publicGameData.gameState = GameState.PreGame;
    }
}