using System;
using System.Collections.Generic;
using Godot;

public partial class IconButton : Button
{

    private TextureRect solidIcon;
    private TextureRect outlineIcon;

    private HoverController outlineHover;
    private HoverController solidHover;

    public override void OnReady()
    {
        solidIcon = GetNode<TextureRect>("%SolidIcon");
        outlineIcon = GetNode<TextureRect>("%OutlineIcon");

        solidIcon.Texture = GetMeta("SolidIcon").As<CompressedTexture2D>();
        outlineIcon.Texture = GetMeta("OutlineIcon").As<CompressedTexture2D>();

        outlineHover = new HoverController(outlineIcon);
        solidHover = new HoverController(solidIcon);

        solidIcon.Hide();
    }

    public override void OnUpdateDisplay(ButtonState oldState, ButtonState newState)
    {
        switch (newState)
        {
            case ButtonState.Normal:
                solidHover.Reset();
                outlineHover.Reset();
                solidIcon.Hide();
                break;
            case ButtonState.Hover:
                solidHover.Lower();
                outlineHover.Raise();
                solidIcon.Show();
                break;
            case ButtonState.Pressed:
                solidHover.Lower();
                outlineHover.Reset();
                solidIcon.Show();
                break;
        }
    }

    public override void _Process(double delta)
    {
        solidHover.Process(delta);
        outlineHover.Process(delta);
    }


}
