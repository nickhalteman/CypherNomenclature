using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[NetworkMessage(NetworkDataType.DirectorGameData)]
public class DirectorGameData : IServerMessage
{
    [Serialize]
    public Team[] keyOwners = new Team[GameConstants.keyCount]
    {
        Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
        Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
        Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
        Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
        Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator, Team.Spectator,
    };

    public void OnReceiveClient()
    {
        GD.Print("DirectorGameData: OnReceiveClient");
        Client.gameData.directorGameData = this;
        //Client.GameDataChanged();
    }
}