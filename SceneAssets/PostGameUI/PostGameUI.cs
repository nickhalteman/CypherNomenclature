using Godot;
using System;
using Steamworks;
using System.Collections.Generic;

public partial class PostGameUI : Control
{
    // Called when the node enters the scene tree for the first time.

    private Dictionary<Team, PreGameDirector> teamDirectors = new Dictionary<Team, PreGameDirector>();
    private Dictionary<Team, VBoxContainer> teamAgents = new Dictionary<Team, VBoxContainer>();
    private Dictionary<Team, Label> teamWinLossLabels = new Dictionary<Team, Label>();
    private VBoxContainer gameRecapList;
    private Button newGameButton;

    private static PackedScene agentListEntry = ResourceLoader.Load<PackedScene>("res://SceneAssets/InGameUI/AgentListEntry.tscn");
    private static PackedScene gameRecapEntry = ResourceLoader.Load<PackedScene>("res://SceneAssets/PostGameUI/GameRecapEntry.tscn");

    private GridContainer keyContainer;

    public override void _Ready()
	{

        teamDirectors[Team.Home] = GetNode<PreGameDirector>("%HomeDirector");
        teamDirectors[Team.Away] = GetNode<PreGameDirector>("%AwayDirector");

        teamAgents[Team.Home] = GetNode<VBoxContainer>("%HomeAgentList");
        teamAgents[Team.Away] = GetNode<VBoxContainer>("%AwayAgentList");

        teamWinLossLabels[Team.Home] = GetNode<Label>("%HomeWinLossLabel");
        teamWinLossLabels[Team.Away] = GetNode<Label>("%AwayWinLossLabel");

        gameRecapList = GetNode<VBoxContainer>("%GameRecapList");

        newGameButton = GetNode<Button>("%NewGameButton");

        keyContainer = GetNode<GridContainer>("%KeyContainer");

        foreach (Team team in new Team[] { Team.Home, Team.Away })
        {
            teamDirectors[team].SetSteamID(Client.gameData.gameRecapData.directors[team]);

            int agentCount = Client.gameData.gameRecapData.agents[team].Count;

            teamAgents[team].AddRemoveChildren(agentListEntry, agentCount);

            for(int i = 0; i < agentCount; i++)
            {
                AgentListEntry agent = (teamAgents[team].GetChild(i) as AgentListEntry);

                agent?.Show();
                agent?.SetSteamID(Client.gameData.gameRecapData.agents[team][i]);
            }

            if (team == Client.gameData.gameRecapData.winningTeam)
            {
                teamWinLossLabels[team].Text = "Win";
            }
            else
            {
                teamWinLossLabels[team].Text = "Loss";
            }
        }

        int recapEventCount = Client.gameData.gameRecapData.gameRecapEvents.Count;

        gameRecapList.AddRemoveChildren(gameRecapEntry, recapEventCount);

        for (int i = 0; i < gameRecapList.GetChildCount(); i++)
        {
            GameRecapEntry recapEntry = (gameRecapList.GetChild(i) as GameRecapEntry);

            if (i < recapEventCount)
            {
                recapEntry.SetRecapEvent(Client.gameData.gameRecapData.gameRecapEvents[i]);
            }
            else
            {
                recapEntry.Hide();
            }
        }

        int keyIndex = 0;
        foreach(Key key in keyContainer.GetChildren())
        {
            key.SetKey(keyIndex++);
        }

        newGameButton.Visible = Client.isHost;
    }

    public void NewGameButton()
    {
        Network.SendMessage(Client.hostConnection, new GameEvent.HostStopGame());
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
}
