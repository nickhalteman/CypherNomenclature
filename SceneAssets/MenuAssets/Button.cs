using Godot;
using System;

public partial class Button : Control
{
    [Signal]
    public delegate void ButtonPressedEventHandler();
	public sealed override void _Ready()
	{
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        VisibilityChanged += UpdateDisplay;

        OnReady();
    }
    public virtual void OnReady() { }
    public enum ButtonState
    {
        Normal,
        Hover,
        Pressed
    };

    private ButtonState lastState = ButtonState.Normal;
    public ButtonState buttonState
    {
        get
        {
            ButtonState state;

            if (pressed)
            {
                state = ButtonState.Pressed;
            }
            else if (hover)
            {
                state = ButtonState.Hover;
            }
            else
            {
                state = ButtonState.Normal;
            }

            if (!IsVisibleInTree())
            {
                state = ButtonState.Normal;
            }


            return state;
        }
    }
    


    private void UpdateDisplay()
    {

        if (!IsVisibleInTree())
        {
            hover = false;
            pressed = false;
        }

        ButtonState newState = buttonState;

        if (newState != lastState)
        {
            OnUpdateDisplay(lastState, newState);
            lastState = newState;
        }
    }

    public virtual void OnUpdateDisplay(ButtonState oldState, ButtonState newState) {}
    private bool pressed = false;
	private bool hover = false;
    public void OnMouseEntered()
    {
        hover = true;
        pressed = false;
        UpdateDisplay();
    }
    public void OnMouseExited()
    {
        hover = false;
        pressed = false;
        UpdateDisplay();
    }
    public virtual void OnPressed()
    {

    }

    public sealed override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouse)
        {
            if (!IsVisibleInTree())
            {

                pressed = false;
                return;
            }
            InputEventMouseButton inputEventMouseButton;
            if ((inputEventMouseButton = @event as InputEventMouseButton) != null && inputEventMouseButton.ButtonIndex == MouseButton.Left)
            {
                if (!hover)
                {
                    pressed = false;
                } else if(inputEventMouseButton.Pressed && IsVisibleInTree())
                {
                    pressed = true;
                } else if(pressed && IsVisibleInTree())
                {
                    pressed = false;
                    EmitSignal(SignalName.ButtonPressed);
                    OnPressed();
                } else
                {
                    pressed = false;
                }
            }
            UpdateDisplay();
        }
    }
}
