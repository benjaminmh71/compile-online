using Godot;
using System;

public partial class CloseRevealPanelButton : Button
{
    public override void _Ready()
    {
        Pressed += OnPressed;
    }

    void OnPressed()
    {
        Game.instance.revealPanel.Visible = false;
    }
}
