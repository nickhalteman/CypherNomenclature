using Godot;
using Steamworks;
using System;
using System.Runtime.CompilerServices;

public partial class KeyHover : Control
{

	private AnimatedSprite2D avatar;

	private CSteamID steamID = CSteamID.Nil;


	public override void _Ready()
	{
		avatar = GetNode<AnimatedSprite2D>("%Avatar");

        FriendManager.FriendUpdated += FriendManager_FriendUpdated;
	}
    public override void _ExitTree()
    {
        FriendManager.FriendUpdated -= FriendManager_FriendUpdated;
    }


    private void FriendManager_FriendUpdated(CSteamID updatedSteamID)
    {
		if(updatedSteamID == steamID)
		{
			SetSteamID(updatedSteamID);
		}
    }

    public void SetSteamID(CSteamID steamID)
	{
		this.steamID = steamID;

		if(FriendManager.TryGetFriendData(steamID,out SteamFriendData steamFriendData))
		{
			avatar.SpriteFrames = steamFriendData.avatar;
			avatar.Play();
		}

	}

}
