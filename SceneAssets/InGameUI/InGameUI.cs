using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Steamworks;
using GameEvent;
using ServerEvent;

public partial class InGameUI : Control
{

	private GridContainer keyContainer;
	private Dictionary<Team, VBoxContainer> agentLists = new Dictionary<Team, VBoxContainer>();
    private Dictionary<Team, AnimatedSprite2D> directorProfiles = new Dictionary<Team, AnimatedSprite2D>();

	private Dictionary<Team, Label> teamStatuses = new Dictionary<Team, Label>();
    private Dictionary<Team, Label> teamHintHistories = new Dictionary<Team, Label>();

    private Dictionary<Team, HBoxContainer> teamKeyProgressContainers = new Dictionary<Team, HBoxContainer>();


    private Key[] keyCards = new Key[GameConstants.keyCount];

    private Control directorHintPanel;
    private OptionButton directorHintNumber;
    private LineEdit directorHintWord;

    private Control activeUploadPanel;
    private ProgressBar uploadProgress;
    private Key uploadKey;

    private Control inactiveUploadPanel;
    private Label inactiveUploadLabel;

    private ColorRect endGameShadow;
    private Control endGamePopup;
    private VBoxContainer winTextScroll;
    private VBoxContainer lossTextScroll;
    private VBoxContainer endGameTextScroll;

    private Button transmitHintButton;
    private Label transmitHintLabel;

    private ProgressBar endGameProgressBar;
    private Button endGameButton;
    private bool isEndGame = false;
    private float endGameProgressCurrent = 0f;
    private float endGameProgressTotal = 0f;


    private LineEdit chatLineEdit;
    private VBoxContainer chatContainer;
    private ScrollContainer chatScroll;
    private bool autoScrollChat = false;


    private static PackedScene agentListEntry = ResourceLoader.Load<PackedScene>("res://SceneAssets/InGameUI/AgentListEntry.tscn");
    private static PackedScene chatMessage = ResourceLoader.Load<PackedScene>("res://SceneAssets/InGameUI/ChatMessage.tscn");
    private static PackedScene chatNotice = ResourceLoader.Load<PackedScene>("res://SceneAssets/InGameUI/ChatNotice.tscn");
    private static PackedScene keyProgressSegment = ResourceLoader.Load<PackedScene>("res://SceneAssets/InGameUI/KeyProgressSegment.tscn");



    private List<ulong> handles = new List<ulong>();
    public override void _Ready()
	{

		keyContainer = GetNode<GridContainer>("%KeyContainer");

		agentLists[Team.Home] = GetNode<VBoxContainer>("%HomeAgentList");
        agentLists[Team.Away] = GetNode<VBoxContainer>("%AwayAgentList");

		directorProfiles[Team.Home] = GetNode<AnimatedSprite2D>("%HomeDirector");
        directorProfiles[Team.Away] = GetNode<AnimatedSprite2D>("%AwayDirector");

        teamStatuses[Team.Home] = GetNode<Label>("%HomeDirectorStatus");
        teamStatuses[Team.Away] = GetNode<Label>("%AwayDirectorStatus");

        teamHintHistories[Team.Home] = GetNode<Label>("%HomeHintHistory");
        teamHintHistories[Team.Away] = GetNode<Label>("%AwayHintHistory");

        teamKeyProgressContainers[Team.Home] = GetNode<HBoxContainer>("%HomeKeyProgress");
        teamKeyProgressContainers[Team.Away] = GetNode<HBoxContainer>("%AwayKeyProgress");

        directorHintPanel = GetNode<Control>("%DirectorHintPanel");
        directorHintNumber = GetNode<OptionButton>("%DirectorHintNumber");
        directorHintWord = GetNode<LineEdit>("%DirectorHintWord");

        activeUploadPanel = GetNode<Control>("%ActiveUploadPanel");
        uploadProgress = GetNode<ProgressBar>("%UploadProgress");
        uploadKey = GetNode<Key>("%UploadKey");

        inactiveUploadPanel = GetNode<Control>("%InactiveUploadPanel");
        inactiveUploadLabel = GetNode<Label>("%InactiveUploadLabel");

        endGameShadow = GetNode<ColorRect>("%EndGameShadow");
        endGamePopup = GetNode<Control>("%EndGamePopup");
        winTextScroll = GetNode<VBoxContainer>("%WinTextScroll");
        lossTextScroll = GetNode<VBoxContainer>("%LossTextScroll");
        endGameProgressBar = GetNode<ProgressBar>("%EndGameProgressBar");
        endGameButton = GetNode<Button>("%EndGameButton");

        chatLineEdit = GetNode<LineEdit>("%ChatLineEdit");
        chatContainer = GetNode<VBoxContainer>("%ChatContainer");
        chatScroll = GetNode<ScrollContainer>("%ChatScroll");

        transmitHintButton = GetNode<Button>("%TransmitHintButton");
        transmitHintLabel = GetNode<Label>("%TransmitHintLabel");

        Node[] nodes = keyContainer.GetChildren().ToArray();
        for(int i = 0; i < GameConstants.keyCount; i++)
        {
            keyCards[i] = nodes[i] as Key;
            keyCards[i].SetKey(i);
        }

        byte[] updateStatusMonitorTypes = new byte[]
        {
            (byte)GameEventType.PlayerChangeTeamRole,
            (byte)GameEventType.DirectorGiveHint,

            (byte)ServerEventType.TimerUpdate,
            (byte)ServerEventType.KeyUploadComplete,

            (byte)NetworkDataType.PublicGameData,
        };

        byte[] updatePlayersTypes = new byte[]
        {
            (byte)GameEventType.PlayerChangeTeamRole,
            (byte)NetworkDataType.PublicGameData,
        };

        byte[] updateNetworkUplinkTypes = new byte[]
        {
            (byte)GameEventType.PlayerChangeTeamRole,
            (byte)GameEventType.AgentStartUpload,
            (byte)GameEventType.AgentCancelUpload,

            (byte)ServerEventType.KeyUploadProgress,
            (byte)ServerEventType.KeyUploadComplete,

            (byte)GameEventType.DirectorGiveHint,
            (byte)ServerEventType.TimerUpdate,

            (byte)NetworkDataType.PublicGameData,
            (byte)NetworkDataType.TeamGameData,
        };

        byte[] updateEndGameTypes = new byte[]
        {
            (byte)GameEventType.HostStartGame,
            (byte)GameEventType.HostStopGame,
            (byte)NetworkDataType.PublicGameData,
            (byte)NetworkDataType.GameRecapData,
        };


        handles.Add(Client.RegisterNetworkMessageCallback(updateStatusMonitorTypes, UpdateStatusMonitor));
        handles.Add(Client.RegisterNetworkMessageCallback(updatePlayersTypes, UpdatePlayers));
        handles.Add(Client.RegisterNetworkMessageCallback(updateNetworkUplinkTypes, UpdateNetworkUplink));
        handles.Add(Client.RegisterNetworkMessageCallback(updateEndGameTypes, UpdateEndGame));
        handles.Add(Client.RegisterNetworkMessageCallback((byte)GameEventType.PlayerMessage, OnPlayerMessage));

        handles.Add(Client.RegisterNetworkMessageCallback((byte)ServerEventType.KeyUploadComplete, OnKeyUploadComplete));
        handles.Add(Client.RegisterNetworkMessageCallback((byte)GameEventType.AgentStartUpload, OnAgentStartUpload));
        handles.Add(Client.RegisterNetworkMessageCallback((byte)GameEventType.AgentCancelUpload, OnAgentCancelUpload));
        handles.Add(Client.RegisterNetworkMessageCallback((byte)GameEventType.DirectorGiveHint, OnDirectorGiveHint));

        UpdateStatusMonitor(null);
        UpdatePlayers(null);
        UpdateNetworkUplink(null);
        UpdateEndGame(null);

        foreach(Node child in chatContainer.GetChildren())
        {
            child.QueueFree();
        }

    }

    private ChatNotice SendChatNotice(string message, Team team, bool isImportant)
    {

        ChatNotice newChatNotice = chatNotice.Instantiate<ChatNotice>();
        chatContainer.AddChild(newChatNotice);
        newChatNotice.SetText(message, team, isImportant);
        return newChatNotice;
    }

    private void OnKeyUploadComplete(object obj)
    {
        KeyUploadComplete keyUpload = (KeyUploadComplete)obj;
        if (keyUpload == null) { return; }

        if (Client.gameData.publicGameData.TryGetPlayerData(SteamUser.GetSteamID(),out PlayerData playerData))
        {
            ChatNotice newChatNotice;
            if (keyUpload.team == playerData.team || playerData.team == Team.Spectator)
            {
                newChatNotice = SendChatNotice($"Key Uploaded: [{Client.gameData.publicGameData.keyWords[keyUpload.keyIndex]}]", keyUpload.team, false);
            } else
            {
                newChatNotice = SendChatNotice($"Key Uploaded: [{Client.gameData.publicGameData.keyWords[keyUpload.keyIndex]}]", keyUpload.team, false);
            }
        }
    }

    private void OnAgentStartUpload(object obj)
    {
        AgentStartUpload agentStartUpload = (AgentStartUpload)obj;
        if(agentStartUpload == null) { return; }


        SteamFriendData friendData;
        if(!FriendManager.TryGetFriendData(agentStartUpload.playerSteamID, out friendData))
        {
            return;
        }

        PlayerData playerData;
        if(!Client.gameData.publicGameData.TryGetPlayerData(agentStartUpload.playerSteamID,out playerData))
        {
            return;
        }

        ChatNotice newChatNotice = SendChatNotice($"{friendData.personaName}: Uploading Key [{Client.gameData.publicGameData.keyWords[agentStartUpload.uploadingKeyIndex]}]", playerData.team, false);
    }

    private void OnAgentCancelUpload(object obj)
    {
        AgentCancelUpload agentCancelUpload = (AgentCancelUpload)obj;
        if (agentCancelUpload == null) { return; }

        SteamFriendData friendData;
        if (!FriendManager.TryGetFriendData(agentCancelUpload.playerSteamID, out friendData))
        {
            return;
        }

        PlayerData playerData;
        if (!Client.gameData.publicGameData.TryGetPlayerData(agentCancelUpload.playerSteamID, out playerData))
        {
            return;
        }

        ChatNotice newChatNotice = SendChatNotice($"{friendData.personaName}: Upload Cancelled", playerData.team, false);
    }

    private void OnDirectorGiveHint(object obj)
    {
        DirectorGiveHint directorGiveHint = (DirectorGiveHint)obj;
        if (directorGiveHint == null) { return; }

        if (Client.gameData.publicGameData.TryGetPlayerData(SteamUser.GetSteamID(), out PlayerData playerData) &&
            Client.gameData.publicGameData.TryGetPlayerData(directorGiveHint.playerSteamID, out PlayerData directorPlayerData))
        {
            ChatNotice newChatNotice;

            SteamFriendData steamFriendData;
            if (!FriendManager.TryGetFriendData(directorGiveHint.playerSteamID,out steamFriendData))
            {
                return;
            }

            if (directorPlayerData.team == playerData.team)
            {
                if(playerData.role == Role.Agent)
                {
                    newChatNotice = SendChatNotice($"New Hint: [{directorGiveHint.hintWord}_{directorGiveHint.hintNumber}]", directorPlayerData.team, true);
                } else
                {
                    newChatNotice = SendChatNotice($"Transmitted Hint: [{directorGiveHint.hintWord}_{directorGiveHint.hintNumber}]", directorPlayerData.team, true);
                }
                //newChatNotice.ShowIcon(directorPlayerData.team, true, true);
            }
            else if (playerData.team != Team.Spectator)
            {
                newChatNotice = SendChatNotice($"Enemy Hint: [{directorGiveHint.hintWord}_{directorGiveHint.hintNumber}]", directorPlayerData.team, true);
                //newChatNotice.ShowIcon(directorPlayerData.team, true, true);
            } else
            {
                newChatNotice = SendChatNotice($"New Hint: [{directorGiveHint.hintWord}_{directorGiveHint.hintNumber}]", directorPlayerData.team, true);
                //newChatNotice.ShowIcon(directorPlayerData.team, true, true);
            }
        }
    }
    private void UpdateStatusMonitor(object obj)
    {
        foreach (Team team in new Team[] { Team.Home, Team.Away })
        {
            int nextHintTime = Mathf.CeilToInt(Client.gameData.publicGameData.hintTimer[team]);


            int uploadCooldown = Mathf.CeilToInt(Client.gameData.publicGameData.uploadCooldown[team]);
            string networkStatus = uploadCooldown == 0 ? "Network Status: Connected" : $"Network Status: Cooldown ({uploadCooldown / 60}:{uploadCooldown % 60:d2})";

            teamStatuses[team].Text = $"Hints Available: {Client.gameData.publicGameData.hintsAvailable[team]}/{GameConstants.maxHints}\nNext Hint Time: {nextHintTime / 60}:{nextHintTime % 60:d2}\n{networkStatus}";

            string hintString = "";
            bool first = true;
            foreach (string hint in Client.gameData.publicGameData.hints[team])
            {
                if (first)
                {
                    first = false;
                    hintString = hint;
                    continue;
                }
                hintString += $", {hint}";
            }
            teamHintHistories[team].Text = hintString;

            teamKeyProgressContainers[team].AddRemoveChildren(keyProgressSegment, GameConstants.winKeyCount);

            for (int i = 0; i < GameConstants.winKeyCount; i++)
            {
                teamKeyProgressContainers[team].GetChild<KeyProgressSegment>(i).SetFound(Client.gameData.publicGameData.keysFound[team] > i);
            }
        }
    }

    private void UpdatePlayers(object obj)
    {
        foreach (Team team in new Team[] { Team.Home, Team.Away })
        {
            VBoxContainer agentList = agentLists[team];

            List<CSteamID> agentIDs = Client.gameData.publicGameData.agents[team].ToList();

            agentList.AddRemoveChildren(agentListEntry, agentIDs.Count);

            if (agentList.GetChildCount() == agentIDs.Count)
            {
                for (int i = 0; i < agentIDs.Count; i++)
                {
                    (agentList.GetChild(i) as AgentListEntry).SetSteamID(agentIDs[i]);
                }
            }

            CSteamID directorID = Client.gameData.publicGameData.directors[team];
            if (directorID.IsValid())
            {

                SteamFriendData friendData;
                if (!FriendManager.TryGetFriendData(directorID, out friendData))
                {
                    continue;
                }

                directorProfiles[team].SpriteFrames = friendData.avatar;
                directorProfiles[team].Play("default");
            }
            else
            {
                directorProfiles[team].SpriteFrames = new SpriteFrames();
            }
        }

        if(Client.gameData.publicGameData.TryGetPlayerData(SteamUser.GetSteamID(),out PlayerData playerData))
        {
            chatLineEdit.Editable = playerData.role != Role.Spectator;
            switch (playerData.role)
            {
                case Role.Spectator:
                    chatLineEdit.PlaceholderText = "Spectators cannot send messages.";
                    break;
                case Role.Agent:
                    chatLineEdit.PlaceholderText = "Type here to message your team.";
                    break;
                case Role.Director:
                    chatLineEdit.PlaceholderText = "Type here to message the other Director.";
                    break;
            }
        }
    }


    public void OnHintBoxUpdate(string s)
    {
        UpdateNetworkUplink(null);
    }


    private void UpdateNetworkUplink(object obj)
    {
        PlayerData playerData;
        if(!Client.gameData.publicGameData.TryGetPlayerData(SteamUser.GetSteamID(), out playerData))
        {
            return;
        }


        if (playerData.role == Role.Director)
        {
            directorHintPanel.Show();
            activeUploadPanel.Hide();
            inactiveUploadPanel.Hide();

            string hintWord = directorHintWord.Text.ToUpper().Trim();

            //check hints
            if (Client.gameData.publicGameData.hintsAvailable[playerData.team] <= 0)
            {
                int hintTimer = Mathf.CeilToInt(Client.gameData.publicGameData.hintTimer[playerData.team]);
                transmitHintLabel.Text = $"Time until next hint: {hintTimer / 60:0}:{hintTimer % 60:00}";
                transmitHintButton.Hide();
                transmitHintLabel.Show();
            }
            else if (hintWord.Length < GameConstants.minHintLength || hintWord.Length > GameConstants.maxHintLength)
            {
                transmitHintLabel.Text = $"Hint must have {GameConstants.minHintLength}-{GameConstants.maxHintLength} characters";
                transmitHintButton.Hide();
                transmitHintLabel.Show();
            }
            else if (!DirectorGiveHint.hintWordRegex.IsMatch(hintWord))
            {
                transmitHintLabel.Text = "Hint contains invalid characters";
                transmitHintButton.Hide();
                transmitHintLabel.Show();
            }
            else
            {
                transmitHintButton.Show();
                transmitHintLabel.Hide();
            }
        }
        else if (playerData.role == Role.Agent)
        {
            directorHintPanel.Hide();

            float uploadCooldown;
            List<string> hintHistory;
            if (Client.gameData.teamGameData[playerData.team].uploadingKeyIndex >= 0)
            {
                activeUploadPanel.Show();
                inactiveUploadPanel.Hide();

                uploadKey.SetKey(Client.gameData.teamGameData[playerData.team].uploadingKeyIndex);
                uploadProgress.Value = 100f * Client.gameData.teamGameData[playerData.team].uploadingProgress;

            } else if ((uploadCooldown = Client.gameData.publicGameData.uploadCooldown[playerData.team]) > 0f)
            {
                int uploadCooldownInt = Mathf.CeilToInt(uploadCooldown);
                activeUploadPanel.Hide();
                inactiveUploadLabel.Text = $"Upload Service Cooldown\n\n{uploadCooldownInt / 60:0}:{uploadCooldownInt % 60:00}";
                inactiveUploadPanel.Show();
            } else if ((hintHistory = Client.gameData.publicGameData.hints[playerData.team]).Count > 0)
            {
                activeUploadPanel.Hide();
                inactiveUploadLabel.Text = $"Upload Service Available\n\nLatest Hint: [{hintHistory.Last()}]";
                inactiveUploadPanel.Show();
            } else
            {
                activeUploadPanel.Hide();
                inactiveUploadLabel.Text = $"Upload Service Available\n\nHint Cache Empty";
                inactiveUploadPanel.Show();
            }
        }
        else
        {
            directorHintPanel.Hide();
            activeUploadPanel.Hide();
            inactiveUploadPanel.Hide();
        }


    }

    private void UpdateEndGame(object obj)
    {
        if (Client.gameData.publicGameData.gameState == GameState.PostGame)
        {
            if (!isEndGame)
            {
                isEndGame = true;
                endGameProgressCurrent = 0f;

                endGameShadow.Show();
                endGamePopup.Show();

                Team clientTeam = Client.gameData.publicGameData.playerData[SteamUser.GetSteamID()].team;

                if (Client.gameData.gameRecapData.winningTeam == clientTeam || clientTeam == Team.Spectator)
                {
                    winTextScroll.Show();
                    lossTextScroll.Hide();
                    endGameProgressBar.Show();
                    endGameTextScroll = winTextScroll;
                }
                else
                {
                    winTextScroll.Hide();
                    lossTextScroll.Show();
                    endGameProgressBar.Hide();
                    endGameTextScroll = lossTextScroll;
                }


                endGameProgressBar.Value = 0f;
                endGameProgressTotal = endGameTextScroll.GetChildCount();

                endGameButton.Hide();

                foreach (Node textScrollChild in endGameTextScroll.GetChildren())
                {
                    Label textScrollLabel = textScrollChild as Label;

                    textScrollLabel.VisibleRatio = 0;
                    textScrollLabel.Hide();
                }
            }
        }
        else
        {
            isEndGame = false;
            endGameProgressCurrent = 0f;


            endGameShadow.Hide();
            endGamePopup.Hide();
        }
    }

    private void OnPlayerMessage(object obj)
    {
        PlayerMessage playerMessage = obj as PlayerMessage;
        if(playerMessage == null) { return; }

        ChatMessage lastChatMessage;
        Node lastChatEntry;
        if (    chatContainer.GetChildCount() <= 0 ||                       //no chat messages yet
                (lastChatEntry = chatContainer.GetChild(-1)) == null ||     //get last child
                (lastChatMessage = lastChatEntry as ChatMessage) == null || //check if it's a chat message
                !lastChatMessage.TryAddMessage(playerMessage)               //try appending to exsisting message
            )
        {
            lastChatMessage = chatMessage.Instantiate<ChatMessage>();
            chatContainer.AddChild(lastChatMessage);
            lastChatMessage.TryAddMessage(playerMessage);
        }

        //chatScroll.SetDeferred("scroll_vertical", int.MaxValue);
    }



    public override void _ExitTree()
    {
        foreach(ulong handle in handles)
        {
            Client.ClearNetworkMessageCallbacks(handle);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{

        const float textScrollSpeed = 3f;

        if (isEndGame && !endGameButton.Visible)
        {
            bool doneTextScroll = true;

            float progressDiff = textScrollSpeed * (float)delta;

            foreach (Node textScrollChild in endGameTextScroll.GetChildren())
            {
                Label textScrollLabel = textScrollChild as Label;
                
                if (textScrollLabel.Visible)
                {
                    if(textScrollLabel.VisibleRatio >= 1.0f)
                    {
                        continue;
                    }
                    endGameProgressCurrent += progressDiff;
                    textScrollLabel.VisibleRatio += progressDiff;
                    doneTextScroll = false;
                    break;                    
                } else
                {
                    textScrollLabel.Show();
                    endGameProgressCurrent += progressDiff;
                    textScrollLabel.VisibleRatio += progressDiff;
                    doneTextScroll = false;
                    break;
                }
            }

            endGameProgressBar.Value = Mathf.Clamp(100f * endGameProgressCurrent / endGameProgressTotal,0,100);

            if (doneTextScroll)
            {
                endGameButton.Show();
                endGameProgressBar.Hide();
            }

        }


    }

    public void OnChatChangeSize()
    {
        if(chatContainer == null || chatScroll == null)
        {
            return;
        }

        int maxScroll = (int)(chatContainer.Size.Y - chatScroll.Size.Y);
        if(Math.Abs(maxScroll - chatScroll.ScrollVertical) < 300)
        {
            chatScroll.SetDeferred("scroll_vertical", int.MaxValue);
        }
    }

    public override void _Input(InputEvent @event)
    {
		if (@event.IsActionPressed("PauseMenu"))
		{
			switch (SceneManager.frontScene)
			{
				case SceneManager.Scene.None:
                    if (!SteamworksManager.overlayOpen)
                    {
                        SceneManager.SetFrontScene(SceneManager.Scene.PauseMenu);
                    }
                    break;
				case SceneManager.Scene.PauseMenu:
                    SceneManager.ClearFrontScene();
					break;
			}
		}
    }
    
    public void CancelUploadButton()
    {

        PlayerData playerData;
        if(!Client.gameData.publicGameData.TryGetPlayerData(SteamUser.GetSteamID(), out playerData))
        {
            return;
        }


        if (playerData.role != Role.Agent)
        {
            return;
        }

        if (Client.gameData.teamGameData[playerData.team].uploadingKeyIndex < 0)
        {
            return;
        }

        Network.SendMessage(Client.hostConnection, new GameEvent.AgentCancelUpload()
        {
            playerSteamID = SteamUser.GetSteamID(),
        });

    }

    public void TransmitHintButton()
    {

        PlayerData playerData;
        if (!Client.gameData.publicGameData.TryGetPlayerData(SteamUser.GetSteamID(), out playerData))
        {
            return;
        }

        //check role
        if (playerData.role != Role.Director)
        {
            return;
        }

        //check hints
        if (Client.gameData.publicGameData.hintsAvailable[playerData.team] <= 0)
        {
            return;
        }

        string hintWord = directorHintWord.Text.ToUpper();

        //check word
        if (!GameEvent.DirectorGiveHint.hintWordRegex.IsMatch(hintWord))
        {
            GD.Print(hintWord);
            return;
        }

        int hintNumber = directorHintNumber.GetItemId(directorHintNumber.Selected);

        //check number
        if (hintNumber < 0 || hintNumber > 10)
        {
            return;
        }

        Network.SendMessage(Client.hostConnection, new GameEvent.DirectorGiveHint()
        {
            hintNumber = hintNumber,
            hintWord = hintWord,
            playerSteamID = SteamUser.GetSteamID()
        });

        directorHintWord.Text = "";

    }

    public void EndGameButton()
    {
        SceneManager.SetBackScene(SceneManager.Scene.PostGameUI);
        SceneManager.ClearFrontScene();
    }

    public void OnMessageSubmit(string chatMessage)
    {
        if(Client.gameData.publicGameData.TryGetPlayerData(SteamUser.GetSteamID(),out PlayerData playerData))
        {
            if(playerData.role == Role.Spectator)
            {
                return;
            }
            chatMessage = chatMessage.Trim();
            if(chatMessage.Length > 256 || chatMessage.Length <= 0)
            {
                return;
            }

            PlayerMessage playerMessage = new PlayerMessage()
            {
                playerSteamID = SteamUser.GetSteamID(),
                time = Time.GetUnixTimeFromSystem(),
                text = chatMessage,
            };

            Network.SendMessage(Client.hostConnection, playerMessage);

            chatLineEdit.Text = "";

        }
    }

}
