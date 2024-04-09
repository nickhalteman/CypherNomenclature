using Godot;
using Godot.NativeInterop;
using Steamworks;
using System;
using System.Collections.Generic;

public partial class PreGameUI : Control
{
	// Called when the node enters the scene tree for the first time.



    private Dictionary<Team,PreGameDirector> directors = new Dictionary<Team,PreGameDirector>();
    private Dictionary<Team,Control> agentLists = new Dictionary<Team, Control>();

    private Dictionary<Team,Control> agentButtons = new Dictionary<Team,Control>();

    private Control spectatorButton;
    private Control spectatorList;

    private static PackedScene preGameAgent = ResourceLoader.Load<PackedScene>("res://SceneAssets/PreGameUI/PreGameAgent.tscn");

    private TextButton disconnectButton;
    private TextButton inviteButton;
    private TextButton shuffleButton;
    private TextButton startButton;

    private ulong handle;
    public override void _Ready()
	{

        directors[Team.Home] = GetNode<PreGameDirector>("%HomeDirector");
        directors[Team.Away] = GetNode<PreGameDirector>("%AwayDirector");

        agentLists[Team.Home] = GetNode<Control>("%HomeAgentList");
        agentLists[Team.Away] = GetNode<Control>("%AwayAgentList");

        agentButtons[Team.Home] = GetNode<Control>("%HomeAgentButton");
        agentButtons[Team.Away] = GetNode<Control>("%AwayAgentButton");

        spectatorButton = GetNode<Control>("%SpectatorButton");
        spectatorList = GetNode<Control>("%SpectatorList");

        disconnectButton = GetNode<TextButton>("%DisconnectButton");
        inviteButton = GetNode<TextButton>("%InviteButton");
        shuffleButton = GetNode<TextButton>("%ShuffleButton");
        startButton = GetNode<TextButton>("%StartButton");

        shuffleButton.Visible = Client.isHost;
        startButton.Visible = Client.isHost;


        foreach (var director in directors)
        {
            director.Value.SetTeam(director.Key);
        }


        if (Client.isHost)
        {
            disconnectButton.SetText("Delete Server");
        }
        else
        {
            disconnectButton.SetText("Disconnect");
        }

        byte[] updateTeamsTypes = new byte[]
        {
            (byte)NetworkDataType.PublicGameData,

            (byte)GameEventType.PlayerJoin,
            (byte)GameEventType.PlayerLeave,
            (byte)GameEventType.PlayerChangeTeamRole,

            (byte)GameEventType.HostStartGame,
            (byte)GameEventType.HostStopGame,
            (byte)GameEventType.HostBanUnbanPlayer,
        };

        handle = Client.RegisterNetworkMessageCallback(updateTeamsTypes, UpdateTeams);
        UpdateTeams(null);
    }

    public override void _ExitTree()
    {
        Client.ClearNetworkMessageCallbacks(handle);
    }

    private void UpdateTeams(object obj)
    {
        foreach(Team team in new Team[] { Team.Home, Team.Away })
        {
            directors[team].SetSteamID(Client.gameData.publicGameData.directors[team]);

            int agentCount = Client.gameData.publicGameData.agents[team].Count;

            Node agentList = agentLists[team];
            int agentNodeCount = agentList.GetChildCount();

            for (int i = 0; i <= agentCount - agentNodeCount; i++)
            {
                agentList.AddChild(preGameAgent.Instantiate<PreGameAgent>());
            }

            agentNodeCount = agentList.GetChildCount();
            for (int i = 0; i < agentNodeCount; i++)
            {
                if(i < agentCount)
                {
                    (agentList.GetChild(i) as Control).Show();
                } else
                {
                    (agentList.GetChild(i) as Control).Hide();
                }
            }

            int agentNodeIndex = 0;
            foreach(CSteamID agent in Client.gameData.publicGameData.agents[team])
            {
                agentList.GetChild<PreGameAgent>(agentNodeIndex++).SetSteamID(agent);
            }
            
        }

        PlayerData playerData;
        if (!Client.gameData.publicGameData.TryGetPlayerData(SteamUser.GetSteamID(), out playerData))
        {
            return;
        }

        if (playerData.role != Role.Agent)
        {
            foreach(Control agentButton in agentButtons.Values)
            {
                agentButton.Visible = true;
            }
        }
        else
        {
            foreach (var agentButtonPair in agentButtons)
            {
                agentButtonPair.Value.Visible = agentButtonPair.Key != playerData.team;
            }
        }

        spectatorButton.Visible = playerData.role != Role.Spectator;

        int spectatorCount = Client.gameData.publicGameData.spectators.Count;
        int spectatorNodeCount = spectatorList.GetChildCount();

        for(int i = 0; i <= spectatorCount - spectatorNodeCount; i++)
        {
            spectatorList.AddChild(preGameAgent.Instantiate<PreGameAgent>());
        }

        spectatorNodeCount = spectatorList.GetChildCount();
        for (int i = 0; i < spectatorNodeCount; i++)
        {
            if(i < spectatorCount)
            {
                (spectatorList.GetChild(i) as Control).Show();
            } else
            {
                (spectatorList.GetChild(i) as Control).Hide();
            }
        }

        int spectatorNodeIndex = 0;
        foreach (CSteamID steamID in Client.gameData.publicGameData.spectators)
        {
            spectatorList.GetChild<PreGameAgent>(spectatorNodeIndex++).SetSteamID(steamID);
        }

    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("PauseMenu"))
        {
            switch (SceneManager.frontScene)
            {
                case SceneManager.Scene.None:
                    if (!SteamworksManager.overlayOpen)
                    {
                        SceneManager.SetFrontScene(SceneManager.Scene.PauseMenu);
                    }
                    break;
                case SceneManager.Scene.PauseMenu:
                    SceneManager.ClearFrontScene();
                    break;
            }
        }
    }

    public void ShuffleTeamsButton()
    {
        GD.Print("ShuffleTeamsButton");
        List<PlayerData> playerRoles = new List<PlayerData>()
        {
            new PlayerData(){role = Role.Agent, team = Team.Away},
            new PlayerData(){role = Role.Agent, team = Team.Home},
            new PlayerData(){role = Role.Director, team = Team.Away},
            new PlayerData(){role = Role.Director, team = Team.Home},
        };

        playerRoles.Shuffle();

        int removeCount = playerRoles.Count - Client.gameData.publicGameData.playerData.Count;
        if(removeCount > 0)
        {
            playerRoles.RemoveRange(0, removeCount);
        } else
        {
            for(int i = 0; i < -removeCount; i++)
            {
                if((i & 0x1) != 0)
                {
                    playerRoles.Add(new PlayerData() { role = Role.Agent, team = Team.Home });
                } else
                {
                    playerRoles.Add(new PlayerData() { role = Role.Agent, team = Team.Away });
                }
            }
        }

        playerRoles.Shuffle();

        int playerRoleIndex = 0;
        foreach (var player in Client.gameData.publicGameData.playerData)
        {
            PlayerData playerRole = playerRoles[playerRoleIndex++];
            Network.SendMessage(Client.hostConnection, new GameEvent.PlayerChangeTeamRole()
            {
                playerSteamID = player.Key,
                role = playerRole.role,
                team = playerRole.team
            });
        }
    }

    public void ClearTeamsButton()
    {
        GD.Print("ClearTeamsButton");
        foreach (var player in Client.gameData.publicGameData.playerData)
        {
            if(player.Value.role == Role.Spectator)
            {
                continue;
            }

            Network.SendMessage(Client.hostConnection, new GameEvent.PlayerChangeTeamRole()
            {
                playerSteamID = player.Key,
                role = Role.Spectator,
                team = Team.Spectator
            });
        }
    }

    public void DisconnectButton()
    {
        Client.Disconnect();
    }

    public void StartGameButton()
    {
        Network.SendMessage(Client.hostConnection, new GameEvent.HostStartGame());
    }

    public void HomeAgentButton()
    {
        Network.SendMessage(Client.hostConnection, new GameEvent.PlayerChangeTeamRole()
        {
            playerSteamID = SteamUser.GetSteamID(),
            team = Team.Home,
            role = Role.Agent
        });
    }

    public void AwayAgentButton()
    {
        Network.SendMessage(Client.hostConnection, new GameEvent.PlayerChangeTeamRole()
        {
            playerSteamID = SteamUser.GetSteamID(),
            team = Team.Away,
            role = Role.Agent
        });
    }

    public void SpectatorButton()
    {
        Network.SendMessage(Client.hostConnection, new GameEvent.PlayerChangeTeamRole()
        {
            playerSteamID = SteamUser.GetSteamID(),
            team = Team.Spectator,
            role = Role.Spectator
        });
    }

    public void InviteButton()
    {
        SteamFriends.ActivateGameOverlayInviteDialogConnectString(Client.connectionString);
    }
}

