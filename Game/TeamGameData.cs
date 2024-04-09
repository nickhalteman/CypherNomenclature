using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

[NetworkMessage(NetworkDataType.TeamGameData)]
public class TeamGameData : IServerMessage
{
    [Serialize]
    public Team team = Team.Spectator;

    [Serialize]
    public int uploadingKeyIndex = -1;

    [Serialize]
    public float uploadingProgress = 0f;

    [Serialize]
    public HashSet<CSteamID>[] keyHovers = new HashSet<CSteamID>[GameConstants.keyCount]
    {
        new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(),
        new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(),
        new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(),
        new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(),
        new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>(), new HashSet<CSteamID>()
    };

    public void OnReceiveClient()
    {
        GD.Print($"Got new team game data");
        Client.gameData.teamGameData[team] = this;
        //Client.GameDataChanged();
    }

}
