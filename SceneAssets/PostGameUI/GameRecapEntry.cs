using Godot;
using System;
using Steamworks;
using System.Collections.Generic;
public partial class GameRecapEntry : Control
{

	private Dictionary<Team,Control> icon = new Dictionary<Team,Control>();

    private Dictionary<Team, TextureRect> keyIcon = new Dictionary<Team, TextureRect>();
    private Dictionary<Team, TextureRect> skullIcon = new Dictionary<Team, TextureRect>();
    private Dictionary<Team, TextureRect> bombIcon = new Dictionary<Team, TextureRect>();

    private Dictionary<Team, AnimatedSprite2D> directorIcon = new Dictionary<Team, AnimatedSprite2D>();
	
	private Dictionary<Team, Label> label = new Dictionary<Team, Label>();
	private Label timestamp;

	// Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		icon[Team.Home] = GetNode<Control>("%HomeIcon");
        icon[Team.Away] = GetNode<Control>("%AwayIcon");

		keyIcon[Team.Home] = GetNode<TextureRect>("%HomeKeyIcon");
        keyIcon[Team.Away] = GetNode<TextureRect>("%AwayKeyIcon");

		skullIcon[Team.Home] = GetNode<TextureRect>("%HomeSkullIcon");
        skullIcon[Team.Away] = GetNode<TextureRect>("%AwaySkullIcon");

        bombIcon[Team.Home] = GetNode<TextureRect>("%HomeBombIcon");
        bombIcon[Team.Away] = GetNode<TextureRect>("%AwayBombIcon");

        label[Team.Home] = GetNode<Label>("%HomeLabel");
        label[Team.Away] = GetNode<Label>("%AwayLabel");

        directorIcon[Team.Home] = GetNode<AnimatedSprite2D>("%HomeDirector");
        directorIcon[Team.Away] = GetNode<AnimatedSprite2D>("%AwayDirector");

        timestamp = GetNode<Label>("%Timestamp");
        
    }

	public void SetRecapEvent(GameRecapEvent recapEvent)
	{
		foreach(Team team in new Team[]{ Team.Home,Team.Away})
        {

            bool isEventTeam = team == recapEvent.team;


            switch (recapEvent.eventType)
            {
                case GameRecapEventType.Hint:
                    label[team].Visible = isEventTeam;
                    label[team].SizeFlagsHorizontal = SizeFlags.Fill;

                    if(icon[team].Visible = isEventTeam)
                    {
                        skullIcon[team].Hide();
                        bombIcon[team].Hide();
                        keyIcon[team].Hide();

                        if (Client.gameData.gameRecapData.directors.TryGetValue(recapEvent.team, out CSteamID directorID) && directorID.IsValid() && FriendManager.TryGetFriendData(directorID, out SteamFriendData steamFriendData))
                        {
                            GD.Print($"DirectorID: {directorID}");
                            AnimatedSprite2D teamDirectorIcon = directorIcon[team];

                            teamDirectorIcon.Show();
                            teamDirectorIcon.SpriteFrames = steamFriendData.avatar;
                            teamDirectorIcon.Play();
                        }
                        else
                        {
                            directorIcon[team].Hide();
                        }
                    }


                    break;
                case GameRecapEventType.UploadKey:
                    icon[team].Visible = isEventTeam;
                    keyIcon[team].Visible = isEventTeam;
                    skullIcon[team].Hide();
                    bombIcon[team].Hide();
                    directorIcon[team].Hide();
                    label[team].Visible = isEventTeam;
                    label[team].SizeFlagsHorizontal = SizeFlags.Fill;

                    break;
                case GameRecapEventType.UploadVirus:
                    icon[team].Visible = isEventTeam;
                    keyIcon[team].Hide();
                    skullIcon[team].Visible = isEventTeam;
                    bombIcon[team].Hide();
                    directorIcon[team].Hide();
                    label[team].Visible = isEventTeam;
                    label[team].SizeFlagsHorizontal = SizeFlags.Fill;
                    break;
                case GameRecapEventType.UploadBomb:
                    icon[team].Visible = isEventTeam;
                    keyIcon[team].Hide();
                    skullIcon[team].Hide();
                    directorIcon[team].Hide();
                    bombIcon[team].Visible = isEventTeam;
                    label[team].Visible = isEventTeam;
                    label[team].SizeFlagsHorizontal = SizeFlags.Fill;
                    break;
            }
        }

        label[recapEvent.team].Text = recapEvent.eventString;

        int milliseconds = (int)(recapEvent.timestamp * 1000f);

        int minutes = milliseconds / 60000;

        float seconds = (milliseconds - minutes * 60000) / 1000f;

        timestamp.Text = $"+{minutes}:{seconds:00.000}";

	}
}
