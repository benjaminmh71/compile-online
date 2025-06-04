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
        GetTree().Root.AddChild(GD.Load<PackedScene>("res://Lobby/lobby.tscn").Instantiate<Lobby>());
    }
}
