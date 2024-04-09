using Godot;
using System;

public partial class DisconnectUI : Control
{

    public void ReconnectButton()
    {
        Client.Reconnect();
        SceneManager.ClearFrontScene();
    }

    public void MainMenuButton()
    {
        Client.Disconnect();
        SceneManager.SetBackScene(SceneManager.Scene.MainMenu);
        SceneManager.ClearFrontScene();
    }
}
