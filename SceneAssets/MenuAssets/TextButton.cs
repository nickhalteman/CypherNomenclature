using Godot;
using System;

public partial class TextButton : Button
{

    private Label buttonText;
    private Control foregroundControl;
    private Control backgroundControl;

    private HoverController foregroundHover;
    private HoverController backgroundHover;

    public override void OnReady()
	{
        buttonText = GetNode<Label>("%ButtonText");
        buttonText.AddThemeFontSizeOverride("font_size", GetMeta("FontSize").AsInt32());
        buttonText.Text = GetMeta("Text").AsString();

        foregroundControl = GetNode<Control>("%Foreground");
        backgroundControl = GetNode<Control>("%Background");



        foregroundHover = new HoverController(foregroundControl);
        backgroundHover = new HoverController(backgroundControl);

        backgroundControl.Hide();
    }

    public override void OnUpdateDisplay(ButtonState oldState, ButtonState newState)
    {
        switch (newState)
        {
            case ButtonState.Normal:
                foregroundHover.Reset();
                backgroundHover.Reset();
                backgroundControl.Hide();
                break;
            case ButtonState.Hover:
                foregroundHover.Raise();
                backgroundHover.Lower();
                backgroundControl.Show();
                break;
            case ButtonState.Pressed:
                foregroundHover.Reset();
                backgroundHover.Lower();
                backgroundControl.Show();
                break;
        }
    }

    public override void _Process(double delta)
    {
        foregroundHover.Process(delta);
        backgroundHover.Process(delta);
    }

    public void SetText(string text)
    {
        buttonText.Text = text;
    }
}
