using Godot;
using System;
using System.Collections.Generic;

public partial class MainScene : Control
{
    public override void _Ready()
    {
        SceneManager.SetMainScene(this);
    }
}
