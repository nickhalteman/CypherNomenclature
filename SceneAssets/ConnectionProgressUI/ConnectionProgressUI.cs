using Godot;
using System;
using Steamworks;
using System.Collections.Generic;

public partial class ConnectionProgressUI : Control
{
	private ProgressBar progressBar;
	private Label connectionStatus;

	private double connectionProgress;
	private double connectionProgressDisplay;

    private Button retryButton;

    private static HashSet<ESteamNetworkingConnectionState> goodConnectionStates = new HashSet<ESteamNetworkingConnectionState>()
	{
		ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting,
		ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute,
		ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected
	};

	public override void _Ready()
	{
		progressBar = GetNode<ProgressBar>("%ProgressBar");
		connectionStatus = GetNode<Label>("%ConnectionStatus");
		
		progressBar.Value = (connectionProgress = 1);

        retryButton = GetNode<Button>("%RetryButton");

        retryButton.Visible = false;
	}

	public override void _Process(double delta)
	{

        progressBar.Value += Math.Clamp(connectionProgress - progressBar.Value, -delta * 100, delta * 100);

    }

    public override void _PhysicsProcess(double delta)
    {

        switch (Client.connectionState)
        {
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                connectionProgress = 33;
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
                connectionProgress = 67;
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                connectionProgress = 100;
                break;
            default:
                connectionProgress = 0;
                break;
        }

        if (progressBar.Value <= 33)
        {
            connectionStatus.Text = "Connecting...";
        }
        else if (progressBar.Value <= 67)
        {
            connectionStatus.Text = "Establishing Route...";
        }
        else
        {
            connectionStatus.Text = "Finalizing...";
        }

        if (progressBar.Value == 100)
        {

            switch (Client.gameData.publicGameData.gameState)
            {
                case GameState.PreGame:
                    SceneManager.SetBackScene(SceneManager.Scene.PreGameUI);
                    break;
                case GameState.InGame:
                    SceneManager.SetBackScene(SceneManager.Scene.InGameUI);
                    break;
                case GameState.PostGame:
                    SceneManager.SetBackScene(SceneManager.Scene.PostGameUI);
                    break;
                default:
                    Client.Disconnect();
                    SceneManager.SetBackScene(SceneManager.Scene.MainMenu);
                    break;
            }

        }

        if (connectionProgress == 0 && progressBar.Value == 0 && !goodConnectionStates.Contains(Client.connectionState))
        {
            retryButton.Visible = true;
            connectionStatus.Text = "Connection Failed!";
            progressBar.Visible = false;
        } else
        {
            retryButton.Visible = false;
        }
    }

    public void RetryButton()
    {
        Client.Reconnect();
    }

    public void MainMenuButton()
    {
        Client.Disconnect();
        SceneManager.SetBackScene(SceneManager.Scene.MainMenu);
    }
}
