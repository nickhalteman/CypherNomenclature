using Godot;
using Steamworks;
using System;

public partial class PreGameDirector : Button
{

	private Control directorContent;
	private Control buttonContent;
	private Label personaName;
	private AnimatedSprite2D avatarSprite;

    private Team team;
    private CSteamID directorID = CSteamID.Nil;

    private ulong handle;
    public override void OnReady()
	{
        directorContent = GetNode<Control>("%DirectorContent");
        buttonContent = GetNode<Control>("%ButtonContent");
        personaName = GetNode<Label>("%PersonaName");
        avatarSprite = GetNode<AnimatedSprite2D>("%Avatar");

        directorContent.Visible = false;
        buttonContent.Visible = true;

        FriendManager.FriendUpdated += FriendManager_FriendUpdated;

        byte[] updateTypes = new byte[]
        {
            (byte)ServerEventType.PingUpdate,
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
		if(steamID == directorID)
		{
			SetSteamID(directorID);
        }
    }

    public void JoinDirector()
    {
		if (!directorID.IsValid())
		{
            Network.SendMessage(Client.hostConnection, new GameEvent.PlayerChangeTeamRole()
            {
                playerSteamID = SteamUser.GetSteamID(),
                role = Role.Director,
                team = team
            });
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


		SteamFriendData friendData;
		if(!FriendManager.TryGetFriendData(steamID, out friendData))
        {
            ClearSteamID();
            return;
        }


        PlayerData playerData;
        if (!Client.gameData.publicGameData.TryGetPlayerData(steamID, out playerData))
        {
            ClearSteamID();
            return;
        }

        directorID = steamID;

        avatarSprite.SpriteFrames = friendData.avatar;
        avatarSprite.Animation = "default";
        avatarSprite.Play("default");
        avatarSprite.Show();

        personaName.Text = $"ID: {friendData.personaName}";

        personaName.Text = $"ID: {friendData.personaName}\nLatency: {playerData.ping}us";

        directorContent.Visible = true;
		buttonContent.Visible = false;

        if(Client.gameData.publicGameData.gameState == GameState.PreGame)
        {
            MouseDefaultCursorShape = CursorShape.PointingHand;
        } else
        {
            MouseDefaultCursorShape = CursorShape.Arrow;
        }
    }

	public void ClearSteamID()
	{
		directorID = CSteamID.Nil;

		directorContent.Visible = false;
		buttonContent.Visible = Client.gameData.publicGameData.gameState == GameState.PreGame;

        MouseDefaultCursorShape = CursorShape.Arrow;
	
	}
}
