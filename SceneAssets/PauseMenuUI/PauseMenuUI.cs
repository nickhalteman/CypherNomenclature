using Godot;
using System;
using Steamworks;

public partial class PauseMenuUI : Control
{
    // Called when the node enters the scene tree for the first time.

    //private Control selectTeamButton;
    //private Control hostSettingsButton;

    private TextButton disconnectButton;
    private TextButton hostStopGameButton;

	public override void _Ready()
	{
        //selectTeamButton = GetNode<Control>("%SelectTeamButton");
        //hostSettingsButton = GetNode<Control>("%HostSettingsButton");
        disconnectButton = GetNode<TextButton>("%DisconnectButton");
        hostStopGameButton = GetNode<TextButton>("%HostStopGameButton");

        disconnectButton.Visible = !Client.isHost;
        hostStopGameButton.Visible = Client.isHost;
        switch (Client.gameData.publicGameData.gameState)
        {
            case GameState.PreGame:
                break;
            case GameState.InGame:
                hostStopGameButton.SetText("Stop Game");
                break;
            case GameState.PostGame:
                hostStopGameButton.SetText("New Game");
                break;
        }
        (new Callable(this, "grab_focus")).CallDeferred();
    }


	public void ResumeButton()
	{
        SceneManager.ClearFrontScene();
	}

	public void SelectTeamButton()
	{

	}

	public void SettingsButton()
	{

	}

	public void DisconnectButton()
	{
		Client.Disconnect();
        SceneManager.ClearFrontScene();
    }

	public void HostSettingsButton()
	{

    }
    public void HostStopGameButton()
    {
        Network.SendMessage(Client.hostConnection, new GameEvent.HostStopGame());
        SceneManager.ClearFrontScene();
    }

    public void InviteButton()
	{
		SteamFriends.ActivateGameOverlayInviteDialogConnectString(Client.connectionString);
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

	public void HomeDirectorButton()
	{
        Network.SendMessage(Client.hostConnection, new GameEvent.PlayerChangeTeamRole()
        {
            playerSteamID = SteamUser.GetSteamID(),
            team = Team.Home,
            role = Role.Director
        });
    }

	public void AwayDirectorButton()
	{
        Network.SendMessage(Client.hostConnection, new GameEvent.PlayerChangeTeamRole()
        {
            playerSteamID = SteamUser.GetSteamID(),
            team = Team.Away,
            role = Role.Director
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


}
