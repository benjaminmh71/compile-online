using Godot;
using System;

public partial class Card : Control
{
    public CardInfo info;
    public bool flipped = false;

    public override void _Ready()
    {
        Render();
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
    }

    public void Render()
    {
        if (flipped)
        {
            GetNode<Label>("Name").Text = "";
            GetNode<Label>("TopText").Text = "";
            GetNode<Label>("MiddleText").Text = "2";
            GetNode<Label>("BottomText").Text = "";
        } else
        {
            GetNode<Label>("Name").Text = info.protocol + " " + info.value.ToString();
            GetNode<Label>("TopText").Text = info.topText;
            GetNode<Label>("MiddleText").Text = info.middleText;
            GetNode<Label>("BottomText").Text = info.bottomText;
        }
    }
}
