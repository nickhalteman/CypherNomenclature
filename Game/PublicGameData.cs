using Godot;
using Godot.NativeInterop;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


[Serialize]
public class PlayerData
{
    [Serialize]
    public Team team = Team.Spectator;

    [Serialize]
    public Role role = Role.Spectator;

    [Serialize]
    public int ping = -1;
}


[NetworkMessage(NetworkDataType.PublicGameData)]
public class PublicGameData : IServerMessage, IOnDeserialize
{
    [Serialize]
    public GameState gameState = GameState.PreGame;

    public Dictionary<Team, CSteamID> directors = new Dictionary<Team, CSteamID>()
    {
        {Team.Home, CSteamID.Nil },
        {Team.Away, CSteamID.Nil }
    };

    [Serialize]
    public Dictionary<Team, int> hintsAvailable = new Dictionary<Team, int>()
    {
        { Team.Home, 0 },
        { Team.Away, 0 }
    };

    [Serialize]
    public Dictionary<Team, int> keysFound = new Dictionary<Team, int>()
    {
        { Team.Home, 0 },
        { Team.Away, 0 }
    };

    [Serialize]
    public Dictionary<Team, float> hintTimer = new Dictionary<Team, float>()
    {
        { Team.Home, 60f },
        { Team.Away, 60f }
    };

    [Serialize]
    public Dictionary<Team, float> uploadCooldown = new Dictionary<Team, float>()
    {
        { Team.Home, 0f },
        { Team.Away, 0f }
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

    public HashSet<CSteamID> spectators = new HashSet<CSteamID>();

    public HashSet<CSteamID> players = new HashSet<CSteamID>();

    public Dictionary<Team, HashSet<CSteamID>> agents = new Dictionary<Team, HashSet<CSteamID>>()
    {
        { Team.Home, new HashSet<CSteamID>() },
        { Team.Away, new HashSet<CSteamID>() }
    };

    public Dictionary<Team, HashSet<CSteamID>> teamPlayers = new Dictionary<Team, HashSet<CSteamID>>()
    {
        { Team.Home, new HashSet<CSteamID>() },
        { Team.Away, new HashSet<CSteamID>() }
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
    public Dictionary<Team, List<string>> hints = new Dictionary<Team, List<string>>()
    {
        {Team.Home, new List<string>(){ "DNIYM_2", "CGDCT_4" } },
        {Team.Away, new List<string>(){ "NOU_4", "AARONJ_3" } },
    };

    [Serialize]
    public Dictionary<CSteamID, PlayerData> playerData = new Dictionary<CSteamID, PlayerData>();
    public void OnReceiveClient()
    {
        GD.Print($"Got new public game data");
        Client.gameData.publicGameData = this;
        //Client.GameDataChanged();
    }

    public object OnDeserialize()
    {
        spectators.Clear();
        agents[Team.Home].Clear();
        agents[Team.Away].Clear();

        directors[Team.Home] = CSteamID.Nil;
        directors[Team.Away] = CSteamID.Nil;

        players.Clear();
        teamPlayers[Team.Home].Clear();
        teamPlayers[Team.Away].Clear();

        foreach (var player in playerData)
        {
            players.Add(player.Key);
            switch (player.Value.role)
            {
                case Role.Spectator:
                    spectators.Add(player.Key);
                    break;
                case Role.Agent:
                    agents[player.Value.team].Add(player.Key);
                    teamPlayers[player.Value.team].Add(player.Key);
                    break;
                case Role.Director:
                    directors[player.Value.team] = player.Key;
                    teamPlayers[player.Value.team].Add(player.Key);
                    break;
            }
        }

        return null;
    }

    public bool TryGetPlayerData(CSteamID steamID, out PlayerData playerDataOut)
    {
        return playerData.TryGetValue(steamID, out playerDataOut);
    }

}