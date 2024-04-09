using Godot;
using Steamworks;
using System;
using System.Runtime.CompilerServices;

public partial class AgentListEntry : Button
{
	// Called when the node enters the scene tree for the first time.

	private AnimatedSprite2D profilePicture;

	private Control outline;
	private HoverController outlineHover;

	public override void OnReady()
	{

		profilePicture = GetNode<AnimatedSprite2D>("%ProfilePicture");
        outline = GetNode<Control>("%Outline");
        outlineHover = new HoverController(outline);
		outlineHover.Reset();
		outline.Hide();

        FriendManager.FriendUpdated += FriendManager_FriendUpdated;
	}

    private void FriendManager_FriendUpdated(CSteamID friendID)
    {
		if(friendID == agentID)
		{
			SetSteamID(agentID);
		}
    }

    public override void _ExitTree()
    {
        FriendManager.FriendUpdated -= FriendManager_FriendUpdated;
    }

    public override void OnUpdateDisplay(ButtonState oldState, ButtonState newState)
    {
		switch (newState)
		{
			case ButtonState.Normal:
				outlineHover.Reset();
				break;
			case ButtonState.Hover:
                outlineHover.Raise();
                break;
			case ButtonState.Pressed:
                outlineHover.Reset();
                break;
		}

		outline.Visible = newState != ButtonState.Normal;
    }


    public override void _Process(double delta)
	{
		outlineHover.Process(delta);
	}

	private CSteamID agentID = CSteamID.Nil;
	public void SetSteamID(CSteamID steamID)
	{
		if (!steamID.IsValid())
		{
			return;
		}


		SteamFriendData friendData;
		if(!FriendManager.TryGetFriendData(steamID, out friendData))
		{
			return;
        }

        agentID = steamID;
        profilePicture.SpriteFrames = friendData.avatar;
		profilePicture.Play("default");
	}

    public override void OnPressed()
    {
        if (agentID.IsValid())
        {
            SteamFriends.ActivateGameOverlayToUser("steamid", agentID);
        }
    }
}
