using Godot;
using System;
using Steamworks;
using System.Net.Http;
using System.Collections.Generic;

public partial class Key : Button
{


    private string cardWord;
	private int keyIndex = -1;
	private Label cardWordLabel;
    private Control fancyLines;


	private TextureRect keyIcon;
	private TextureRect skullIcon;
    private TextureRect bombIcon;
    private ColorRect tintOverlay;

    private static PackedScene keyHover = ResourceLoader.Load<PackedScene>("res://SceneAssets/InGameUI/KeyHover.tscn");
    private HFlowContainer keyHovers;

    private HoverController foregroundHover;
    private HoverController backgroundHover;

    private Control background;
    private Control foreground;

    private Button uploadButton;

    private HashSet<ulong> handles = new HashSet<ulong>();

    public PublicKeyState publicKeyState => Client.gameData.publicGameData.keyStates[keyIndex];


    public override void OnReady()
	{

		cardWordLabel = GetNode<Label>("%CardWord");
        fancyLines = GetNode<Control>("%FancyLines");
        uploadButton = GetNode<Button>("%UploadButton");

		keyIcon = GetNode<TextureRect>("%KeyIcon");
        skullIcon = GetNode<TextureRect>("%SkullIcon");
        bombIcon = GetNode<TextureRect>("%BombIcon");

		tintOverlay = GetNode<ColorRect>("%TintOverlay");

        keyHovers = GetNode<HFlowContainer>("%KeyHovers");


        background = GetNode<Control>("%Background");
        foreground = GetNode<Control>("%Foreground");

        foregroundHover = new HoverController(foreground);
        backgroundHover = new HoverController(background);

        foreground.Hide();

        byte[] updateDisplayTypes = new byte[]
        {
            (byte)NetworkDataType.PublicGameData,
            (byte)NetworkDataType.DirectorGameData,

            (byte)GameEventType.PlayerChangeTeamRole,
            (byte)GameEventType.AgentStartUpload,
            (byte)GameEventType.AgentCancelUpload,
            (byte)GameEventType.HostStartGame,
            (byte)GameEventType.AgentHover,



            (byte)ServerEventType.KeyUploadProgress,
            (byte)ServerEventType.KeyUploadComplete,
            (byte)ServerEventType.TimerUpdate,
        };

        handles.Add(Client.RegisterNetworkMessageCallback(updateDisplayTypes, OnNetworkMessage));
    }

    public override void _ExitTree()
    {
        foreach(ulong handle in handles)
        {
            Client.ClearNetworkMessageCallbacks(handle);
        }
    }

    public void SetKey(int keyIndex)
    {
        this.keyIndex = keyIndex;

        OnNetworkMessage(null);
    }

    public override void OnUpdateDisplay(ButtonState oldState, ButtonState newState)
    {

        if (keyIndex >= GameConstants.keyCount || keyIndex < 0)
        {
            return;
        }

        if (publicKeyState != PublicKeyState.Hidden || Client.gameData.publicGameData.gameState != GameState.InGame)
        {
            backgroundHover.Reset();
            foregroundHover.Reset();
            return;
        }

        if (!Client.gameData.publicGameData.TryGetPlayerData(SteamUser.GetSteamID(), out PlayerData playerData) || playerData.role != Role.Agent)
        {
            return;
        }

        switch (newState)
        {
            case ButtonState.Normal:
                backgroundHover.Reset();
                foregroundHover.Reset();
                break;
            case ButtonState.Hover:
                backgroundHover.Lower();
                foregroundHover.Raise();
                break;
            case ButtonState.Pressed:
                backgroundHover.Lower();
                foregroundHover.Reset();
                break;
        }
    }

    public void OnNetworkMessage(object obj)
    {
        if(keyIndex >= GameConstants.keyCount || keyIndex < 0)
        {
            return;
        }

        cardWord = Client.gameData.publicGameData.keyWords[keyIndex];
        cardWordLabel.Text = cardWord;
        PlayerData playerData;
        if (!Client.gameData.publicGameData.TryGetPlayerData(SteamUser.GetSteamID(), out playerData))
        {
            return;
        }

        PublicKeyState publicKeyState = this.publicKeyState;

        //set cursor shape
        if (playerData.role == Role.Agent && publicKeyState == PublicKeyState.Hidden && Client.gameData.publicGameData.gameState == GameState.InGame)
        {
            MouseDefaultCursorShape = CursorShape.PointingHand;
        } else
        {
            MouseDefaultCursorShape = CursorShape.Arrow;
        }

        //set upload button visibility
        if(Client.gameData.publicGameData.gameState == GameState.PostGame)
        {
            uploadButton.Hide();
        } else if(
            playerData.role == Role.Agent && publicKeyState == PublicKeyState.Hidden &&
            Client.gameData.teamGameData.TryGetValue(playerData.team,out TeamGameData teamGameData) && teamGameData.uploadingKeyIndex < 0 &&
            Client.gameData.publicGameData.uploadCooldown[playerData.team] <= 0
        ) {
            uploadButton.Show();
        } else
        {
            uploadButton.Hide();
        }

        bool isHidden = publicKeyState == PublicKeyState.Hidden;
        fancyLines.Visible = isHidden;
        tintOverlay.Visible = !isHidden;
        foreground.Visible = isHidden;

        switch (publicKeyState)
        {
            case PublicKeyState.Hidden:
                if (playerData.role == Role.Director || Client.gameData.publicGameData.gameState == GameState.PostGame)
                {
                    if (Client.gameData.directorGameData.keyOwners[keyIndex] == playerData.team)
                    {
                        keyIcon.Show();
                        skullIcon.Hide();
                        bombIcon.Hide();
                    }
                    else if (Client.gameData.directorGameData.keyOwners[keyIndex] != Team.Spectator)
                    {
                        keyIcon.Hide();
                        skullIcon.Show();
                        bombIcon.Hide();
                    }
                    else
                    {
                        keyIcon.Hide();
                        skullIcon.Hide();
                        bombIcon.Show();
                    }
                }
                else
                {
                    keyIcon.Hide();
                    skullIcon.Hide();
                    bombIcon.Hide();
                }

                break;
            case PublicKeyState.ShownHome:

                if (playerData.team == Team.Home || playerData.team == Team.Spectator)
                {
                    keyIcon.Show();
                    skullIcon.Hide();
                    bombIcon.Hide();
                }
                else if (playerData.team == Team.Away)
                {
                    keyIcon.Hide();
                    skullIcon.Show();
                    bombIcon.Hide();
                }
                break;
            case PublicKeyState.ShownAway:
                if (playerData.team == Team.Away)
                {
                    keyIcon.Show();
                    skullIcon.Hide();
                    bombIcon.Hide();
                }
                else if (playerData.team == Team.Home || playerData.team == Team.Spectator)
                {
                    keyIcon.Hide();
                    skullIcon.Show();
                    bombIcon.Hide();
                }

                break;
            case PublicKeyState.ShownNone:
                keyIcon.Hide();
                skullIcon.Hide();
                bombIcon.Show();
                break;
        }

        if(Client.gameData.publicGameData.gameState == GameState.InGame && playerData.team != Team.Spectator)
        {
            HashSet<CSteamID> hovers = Client.gameData.teamGameData[playerData.team].keyHovers[keyIndex];
            int hoverCount = keyHovers.GetChildCount();

            for (int i = 0; i <= hovers.Count - hoverCount; i++)
            {
                keyHovers.AddChild(keyHover.Instantiate<KeyHover>());
            }

            hoverCount = keyHovers.GetChildCount();

            for (int i = 0; i < hoverCount; i++)
            {
                if(i < hovers.Count)
                {
                    keyHovers.GetChild<Control>(i).Show();
                } else
                {
                    keyHovers.GetChild<Control>(i).Hide();
                }
            }

            int hoverIndex = 0;
            foreach(CSteamID hoverID in hovers)
            {
                if(hoverIndex < hoverCount)
                {
                    keyHovers.GetChild<KeyHover>(hoverIndex++).SetSteamID(hoverID);
                }
            }
        } else
        {
            foreach(Control childKeyHover in keyHovers.GetChildren())
            {
                childKeyHover.Hide();
            }
        }
    }

    public void UploadButton()
    {
        if(keyIndex < 0 && keyIndex >= GameConstants.keyCount)
        {
            return;
        }

        Network.SendMessage(Client.hostConnection, new GameEvent.AgentStartUpload()
        {
            playerSteamID = SteamUser.GetSteamID(),
            uploadingKeyIndex = keyIndex
        });
    }


    public override void OnPressed()
    {
        if(Client.gameData.publicGameData.TryGetPlayerData(SteamUser.GetSteamID(),out PlayerData playerData) && playerData.role == Role.Agent)
        {
            if(Client.gameData.publicGameData.keyStates[keyIndex] != PublicKeyState.Hidden)
            {
                return;
            }


            Network.SendMessage(Client.hostConnection, new GameEvent.AgentHover()
            {
                playerSteamID = SteamUser.GetSteamID(),
                keyHoverIndex = keyIndex
            });
        }
    }


    public override void _Process(double delta)
    {
        foregroundHover.Process(delta);
        backgroundHover.Process(delta);
    }
}
