using CompileOnline.Game;
using Godot;
using System;
using System.Collections;

public partial class Protocol : Control
{
    public ArrayList cards = new ArrayList();

    public override void _Ready()
    {
        Render();
    }

    public void Render()
    {
        GetNode<Label>("Name").Text = "Apathy";
    }

    public void AddCard(Card card)
    {
        Control cardContainer = GetNode<Control>("Cards");
        card.GetParent().RemoveChild(card);
        cardContainer.AddChild(card);
        cards.Add(card);
        card.Position = new Vector2(-Constants.CARD_WIDTH/2, (cards.Count - 1) * Constants.CARD_STACK_SEPARATION);
    }

    public void AddOppCard(Card card)
    {
        Control cardContainer = GetNode<Control>("Cards");
        cardContainer.AddChild(card);
        cards.Add(card);
        card.Position = new Vector2(-Constants.CARD_WIDTH / 2, (cards.Count - 1) * Constants.CARD_STACK_SEPARATION);
    }
}
