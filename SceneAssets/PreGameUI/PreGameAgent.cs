using Godot;
using Steamworks;
using System;

public partial class PreGameAgent : Button
{
	private Label agentInfo;
    private AnimatedSprite2D avatarSprite;

    private Control outline;
    private HoverController outlineHover;

    private CSteamID agentID = CSteamID.Nil;
    private ulong clientUpdateHandle;
    public override void OnReady()
    {
        agentInfo = GetNode<Label>("%AgentInfo");
        avatarSprite = GetNode<AnimatedSprite2D>("%Avatar");
        outline = GetNode<Control>("%Outline");
        outlineHover = new HoverController(outline);
        outline.Hide();

        FriendManager.FriendUpdated += FriendManager_FriendUpdated;

        byte[] updateTypes = new byte[]
        { 
            (byte)ServerEventType.PingUpdate,
        };

        clientUpdateHandle = Client.RegisterNetworkMessageCallback(updateTypes, UpdateAgentData);



    }

    public override void _Process(double delta)
    {
        outlineHover.Process(delta);
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

    public override void _ExitTree()
    {
        FriendManager.FriendUpdated -= FriendManager_FriendUpdated;
        Client.ClearNetworkMessageCallbacks(clientUpdateHandle);
    }

    private void UpdateAgentData(object obj)
    {
        SetSteamID(agentID);
    }

    private void FriendManager_FriendUpdated(CSteamID updateFriendID)
    {
        if (updateFriendID == agentID)
        {
            GD.Print($"{updateFriendID} == {agentID}");
            SetSteamID(agentID);
        }
    }
    public void SetSteamID(CSteamID steamID)
	{

        if (!steamID.IsValid())
        {
            return;
        }

		agentID = steamID;

        SteamFriendData friendData;
        if(!FriendManager.TryGetFriendData(steamID,out friendData))
        {
            return;
        }

        avatarSprite.SpriteFrames = friendData.avatar;
        avatarSprite.Animation = "default";
        avatarSprite.Play("default");
        avatarSprite.Show();

        agentInfo.Text = $"ID: {friendData.personaName}";

        PlayerData playerData;
        if(!Client.gameData.publicGameData.TryGetPlayerData(agentID,out playerData))
        {
            return;
        }

		agentInfo.Text = $"ID: {friendData.personaName}\nLatency: {playerData.ping}us";

    }

    public override void OnPressed()
    {
		if (agentID.IsValid())
		{
			SteamFriends.ActivateGameOverlayToUser("steamid", agentID);
		}
    }
}
