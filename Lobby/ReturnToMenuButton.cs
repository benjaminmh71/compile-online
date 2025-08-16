using Godot;
using System;

public partial class ReturnToMenuButton : Button
{
    public override void _Ready()
    {
        Pressed += OnPressed;
    }

    void OnPressed()
    {
        Game.instance.QueueFree();
        Lobby lobby = GetTree().Root.GetNode<Lobby>("Lobby");
        lobby.Visible = true;
        lobby.ResetGame();
    }
}
