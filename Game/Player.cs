using Godot;
using System;
using System.Collections;

public partial class Player : Node
{
    public int id;
    ArrayList deck = new ArrayList();
    ArrayList hand = new ArrayList();
    HBoxContainer handCardsContainer;

    public Player(int id)
    {
        this.id = id;
        for (int i = 0; i < 18; i++)
        {
            PackedScene cardScene = GD.Load("res://Game/Card.tscn") as PackedScene;
            Card card = cardScene.Instantiate<Card>();
            card.info = new CardInfo();
            deck.Add(card);
        }
    }

    public override void _Ready()
    {
        handCardsContainer = GetParent().GetNode<HBoxContainer>("HandCardsContainer");
    }

    public void Draw(int n)
    {
        for (int i = 0; i < n; i++)
        {
            Draw();
        }
    }

    public void Draw()
    {
        Card card = deck[0] as Card;
        deck.Remove(card);
        hand.Add(card);
        GD.Print(card);
        GD.Print(handCardsContainer);
        handCardsContainer.AddChild(card);
    }
}
