using Godot;
using Steamworks;
using System;
using System.Collections.Generic;

public partial class ChatNotice : Control
{


	private Label greenText;
    private Label whiteText;

    private Control blocks;
    private Control outline;

    private Team noticeTeam = Team.Spectator;
    private bool important = false;

	public override void _Ready()
	{
        GD.PrintErr("ChatNotice._Ready()");

        greenText = GetNode<Label>("%GreenText");
        whiteText = GetNode<Label>("%WhiteText");

        blocks = GetNode<Control>("%Blocks");
        outline = GetNode<Control>("%Outline");

        UpdateDisplay();
    }


    public void UpdateDisplay()
    {
        switch (noticeTeam)
        {
            case Team.Spectator:
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
                break;
            case Team.Home:
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
                break;
            case Team.Away:
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
                break;
        }

        greenText.Visible = (blocks.Visible = !important);
        outline.Visible = (whiteText.Visible = important);
    }

	public void SetText(string text, Team team, bool isImportant)
    {
        greenText.Text = (whiteText.Text = text);
        noticeTeam = team;
        important = isImportant;

        UpdateDisplay();
    }
}
