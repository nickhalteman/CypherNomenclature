using Godot;
using Steamworks;
using System;

public partial class PostGameDirector : Button
{

    private Control directorContent;
    private Label personaName;
    private AnimatedSprite2D avatarSprite;

    private Team team;
    private CSteamID directorID = CSteamID.Nil;

    private ulong handle;
    public override void OnReady()
    {

        directorContent = GetNode<Control>("%DirectorContent");
        personaName = GetNode<Label>("%PersonaName");
        avatarSprite = GetNode<AnimatedSprite2D>("%Avatar");

        directorContent.Visible = false;

        FriendManager.FriendUpdated += FriendManager_FriendUpdated;

        byte[] updateTypes = new byte[]
        {
            (byte)ServerEventType.PingUpdate,
            (byte)GameEventType.PlayerLeave,
        };
        handle = Client.RegisterNetworkMessageCallback(updateTypes, UpdateDirectorData);
    }

    public override void _ExitTree()
    {
        FriendManager.FriendUpdated -= FriendManager_FriendUpdated;
        Client.ClearNetworkMessageCallbacks(handle);
    }

    private void UpdateDirectorData(object obj)
    {
        SetSteamID(directorID);
    }

    private void FriendManager_FriendUpdated(CSteamID steamID)
    {
        if (steamID == directorID)
        {
            SetSteamID(directorID);
        }
    }

    public override void OnPressed()
    {
        if (directorID.IsValid())
        {
            SteamFriends.ActivateGameOverlayToUser("steamid", directorID);
        }
    }

    public void SetTeam(Team team)
    {
        this.team = team;
    }


    public void SetSteamID(CSteamID steamID)
    {
        if (!steamID.IsValid())
        {
            ClearSteamID();
            return;
        }

        directorID = steamID;

        SteamFriendData friendData;
        if (!FriendManager.TryGetFriendData(steamID, out friendData))
        {
            return;
        }

        avatarSprite.SpriteFrames = friendData.avatar;
        avatarSprite.Animation = "default";
        avatarSprite.Play("default");
        avatarSprite.Show();

        if(Client.gameData.publicGameData.TryGetPlayerData(steamID, out PlayerData playerData))
        {
            personaName.Text = $"ID: {friendData.personaName}\nLatency: {playerData.ping}us";
        } else
        {
            personaName.Text = $"ID: {friendData.personaName}\nDisconnected";
        }

        directorContent.Visible = true;
    }

    public void ClearSteamID()
    {
        directorID = CSteamID.Nil;

        directorContent.Visible = false;

    }
}
