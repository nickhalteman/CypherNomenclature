using Godot;
using System;
using Steamworks;
using System.Collections.Generic;
using GameEvent;

public partial class ChatMessage : Control
{

    private AnimatedSprite2D senderAvatar;
    private Label senderName;
    private VBoxContainer chatTextContainer;

    private static PackedScene chatText = ResourceLoader.Load<PackedScene>("res://SceneAssets/InGameUI/ChatText.tscn");

    private ulong handle;
    private CSteamID senderID = CSteamID.Nil;
    private Team senderTeam = Team.Spectator;

	public override void _Ready()
	{
        senderAvatar = GetNode<AnimatedSprite2D>("%SenderAvatar");
        senderName = GetNode<Label>("%SenderName");
        chatTextContainer = GetNode<VBoxContainer>("%ChatTextContainer");

        FriendManager.FriendUpdated += FriendManager_FriendUpdated;

        foreach(Label chatText in chatTextContainer.GetChildren())
        {
            chatText.QueueFree();
        }
    }

    public override void _ExitTree()
    {
        FriendManager.FriendUpdated -= FriendManager_FriendUpdated;
    }

    private void FriendManager_FriendUpdated(CSteamID steamID)
    {
        if(steamID == senderID)
        {
            UpdateSender();
        }
    }

    public bool TryAddMessage(PlayerMessage message)
    {
        if (Client.gameData.publicGameData.TryGetPlayerData(message.playerSteamID, out PlayerData playerData))
        {
            //don't add message when it's from a different sender
            if(senderID != CSteamID.Nil && message.playerSteamID != senderID)
            {
                return false;
            }

            //don't add message when it's from a different team
            if (senderTeam != Team.Spectator && playerData.team != senderTeam)
            {
                return false;
            }

            senderTeam = playerData.team;
            senderID = message.playerSteamID;

            if(senderTeam == Team.Home)
            {
                LayoutDirection = LayoutDirectionEnum.Ltr;
            } else
            {
                LayoutDirection = LayoutDirectionEnum.Rtl;
            }

            Label newChatText = chatText.Instantiate<Label>();

            newChatText.Text = message.text;

            chatTextContainer.AddChild(newChatText);

            UpdateSender();

            return true;
        }
        return false;
    }

    public void UpdateSender()
    {

        SteamFriendData friendData;
        if(!FriendManager.TryGetFriendData(senderID,out friendData))
        {
            return;
        }

        senderAvatar.SpriteFrames = friendData.avatar;
        senderAvatar.Play();
        senderName.Text = friendData.personaName;
    }


}
