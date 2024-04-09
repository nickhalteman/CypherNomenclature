using Godot;
using System;
using Steamworks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

[Autoload(100)]
public static class SteamworksManager
{
	// Called when the node enters the scene tree for the first time.

	public static bool overlayOpen { get; private set; }

    private static Callback<GameOverlayActivated_t> m_GameOverlayActivated;
	private static void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
	{
		overlayOpen = pCallback.m_bActive != 0;
	}
	public static void _Ready()
	{

		GD.Print("SteamworksManager: searching for steam_api64.dll");
		if(File.Exists(Path.Join(AppContext.BaseDirectory, "steam_api64.dll")))
		{
			GD.Print($"SteamworksManager: found at {Path.Join(AppContext.BaseDirectory, "steam_api64.dll")}");
			NativeLibrary.Load(Path.Join(AppContext.BaseDirectory, "steam_api64.dll"));
		} else
        {
            GD.PrintErr($"SteamworksManager: Couldn't find steam_api64.dll");
			Autoload.instance.GetTree().Quit();
			return;
        }

		try
		{
			GD.Print("SteamworksManager: begin init.");
			if (SteamAPI.Init() && SteamAPI.IsSteamRunning())
			{
				GD.Print("SteamworksManager: found Steam");
			} else
			{
				GD.PrintErr("SteamworksManager: Steam is not running :(");
                Autoload.instance.GetTree().Quit();
                return;
			}
		} catch (Exception e)
		{
			GD.PrintErr("SteamworksManager: Error occurred during initilization");
			GD.PrintErr(e);
            Autoload.instance.GetTree().Quit();
            return;
		}

		m_GameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);

    }

	public static void _ExitTree()
	{
		GD.Print("SteamworksManager: Shutting down steamworks");
		try
		{
			SteamAPI.Shutdown();
			GD.Print("SteamworksManager: Shutdown Success!");
		}
		catch (Exception e)
		{
			GD.Print("SteamworksManager: Error occurred during shutdown");
			GD.Print(e);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public static void _Process(double delta)
	{
		SteamAPI.RunCallbacks();
	}
}
