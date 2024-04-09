using GameEvent;
using Godot;
using Steamworks;
using System;

public partial class PostGameAgent : Button
{
    private Label agentInfo;
    private AnimatedSprite2D avatarSprite;

    private CSteamID agentID = CSteamID.Nil;
    ulong clientUpdateHandle;
    public override void OnReady()
    {
        agentInfo = GetNode<Label>("%AgentInfo");
        avatarSprite = GetNode<AnimatedSprite2D>("%Avatar");


        FriendManager.FriendUpdated += FriendManager_FriendUpdated;

        byte[] updateTypes = new byte[]
        {
            (byte)ServerEventType.PingUpdate,
            (byte)GameEventType.PlayerLeave,
        };

        clientUpdateHandle = Client.RegisterNetworkMessageCallback(updateTypes, UpdateAgentData);

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
        if (!FriendManager.TryGetFriendData(steamID, out friendData))
        {
            return;
        }


        if (Client.gameData.publicGameData.TryGetPlayerData(steamID, out PlayerData playerData))
        {
            agentInfo.Text = $"ID: {friendData.personaName}\nLatency: {playerData.ping}us";
        }
        else
        {
            agentInfo.Text = $"ID: {friendData.personaName}\nDisconnected";
        }

        avatarSprite.SpriteFrames = friendData.avatar;
        avatarSprite.Animation = "default";
        avatarSprite.Play("default");
        avatarSprite.Show();
    }

    public override void OnPressed()
    {
        if (agentID.IsValid())
        {
            SteamFriends.ActivateGameOverlayToUser("steamid", agentID);
        }
    }
}
