using Godot;
using System;

public partial class KeyProgressSegment : Control
{
	private Control outline;
	private Control missingKeyIcon;
	private Control foundKeyIcon;

	public override void _Ready()
	{
		outline = GetNode<Control>("%Outline");
		missingKeyIcon = GetNode<Control>("%MissingKeyIcon");
		foundKeyIcon = GetNode<Control>("%FoundKeyIcon");
        SetFound(false);


    }

    public void SetFound(bool found)
    {
        outline.Visible = found;
        missingKeyIcon.Visible = !found;
        foundKeyIcon.Visible = found;
    }
}
