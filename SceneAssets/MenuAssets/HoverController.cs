using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public class HoverController
{
    private Control control;
    private static float hoverSpeed = 25f;
    public enum Height
    {
        Raised,
        Base,
        Lowered
    }

    public Height height = Height.Base;
    private Vector2 basePosition;

    public HoverController(Control control)
    {
        this.control = control;
        basePosition = control.Position;
    }

    public void Process(double delta)
    {
        Vector2 targetPosition = basePosition;
        switch (height)
        {
            case Height.Base:
                targetPosition = basePosition;
                break;
            case Height.Lowered:
                targetPosition = basePosition + new Vector2(2f, 2f);
                break;
            case Height.Raised:
                targetPosition = basePosition - new Vector2(2f, 2f);
                break;
        }

        control.Position += (targetPosition - control.Position).LimitLength((float)delta * hoverSpeed);

    }

    public void Raise()
    {
        height = Height.Raised;
    }

    public void Lower()
    {
        height = Height.Lowered;
    }

    public void Reset() {
        height = Height.Base;
    }
}
