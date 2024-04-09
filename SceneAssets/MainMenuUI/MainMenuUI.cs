using Godot;
using System;

public partial class MainMenuUI : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void HostButton()
	{
		Server.StartServer();
	}

    public void JoinButton()
    {
		//NetworkSerializer.Test();
    }

    public void SettingsButton()
    {

    }

    public void QuitButton()
    {
		Autoload.instance.GetTree().Quit();
    }
}
