using Godot;
using System;

public partial class Protocol : Control
{
    public override void _Ready()
    {
        Render();
    }

    public void Render()
    {
        GetNode<Label>("Name").Text = "Apathy";
    }
}
