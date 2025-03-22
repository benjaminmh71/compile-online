using Godot;
using System;

public partial class Card : Node
{
    public CardInfo info;

    public override void _Ready()
    {
        GetNode<Label>("Name").Text = info.protocol + " " + info.value.ToString();
        GetNode<Label>("TopText").Text = info.topText;
        GetNode<Label>("MiddleText").Text = info.middleText;
        GetNode<Label>("BottomText").Text = info.bottomText;
    }
}
