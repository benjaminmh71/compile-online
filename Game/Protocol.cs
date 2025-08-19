using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class Protocol : Control
{
    [Signal]
    public delegate void OnClickEventHandler(Protocol protocol);

    public ProtocolInfo info = null;

    public List<Card> cards = new List<Card>();
    public bool compiled = false;

    public override void _Ready()
    {
        Render();
    }

    public override void _Process(double delta)
    {
        Vector2 GlobalMousePosition = GetGlobalMousePosition();

        if (Input.IsActionJustReleased("click") &&
            Geometry2D.IsPointInPolygon(GlobalMousePosition,
                    [new Vector2(GlobalPosition.X, GlobalPosition.Y),
                    new Vector2(GlobalPosition.X, GlobalPosition.Y + (Game.instance.IsLocal(this) ? 1 : -1) * 
                    (Constants.PROTOCOL_HEIGHT + Constants.CARD_HEIGHT + cards.Count * Constants.CARD_STACK_SEPARATION)),
                    new Vector2(GlobalPosition.X + (Game.instance.IsLocal(this) ? 1 : -1) * Constants.PROTOCOL_WIDTH,
                    GlobalPosition.Y + (Game.instance.IsLocal(this) ? 1 : -1) * (Constants.PROTOCOL_HEIGHT + Constants.CARD_HEIGHT +
                    cards.Count * Constants.CARD_STACK_SEPARATION)),
                    new Vector2(GlobalPosition.X + (Game.instance.IsLocal(this) ? 1 : -1) * Constants.PROTOCOL_WIDTH, GlobalPosition.Y)]))
        {
            EmitSignal("OnClick", this);
        }
    }

    public void Render()
    {
        GetNode<Panel>("Panel").SelfModulate = info.backgroundColor;
        GetNode("TextContainer").GetNode<Label>("Name").Text = info.name;
        if (compiled)
        {
            GetNode("TextContainer").GetNode<Label>("Compiled").Visible = true;
        } else
        {
            GetNode("TextContainer").GetNode<Label>("Compiled").Visible = false;
        }
    }

    public void HideProtocol()
    {
        GetNode<Control>("Panel").Visible = false;
        GetNode<Control>("TextContainer").Visible = false;
        GetNode<Control>("SelectionIndicator").Visible = false;
    }

    public void UnHideProtocol()
    {
        GetNode<Control>("Panel").Visible = true;
        GetNode<Control>("TextContainer").Visible = true;
        GetNode<Control>("SelectionIndicator").Visible = true;
    }

    public void AddCard(Card card)
    {
        Control cardContainer = GetNode<Control>("Cards");
        if (card.GetParent() != null)
            card.GetParent().RemoveChild(card);
        cardContainer.AddChild(card);
        cards.Add(card);
        card.Position = new Vector2(-Constants.CARD_WIDTH / 2, (cards.Count - 1) * Constants.CARD_STACK_SEPARATION);
    }

    public void InsertCard(int index, Card card)
    {
        Control cardContainer = GetNode<Control>("Cards");
        if (card.GetParent() != null)
            card.GetParent().RemoveChild(card);
        cardContainer.AddChild(card);
        cardContainer.MoveChild(card, index);
        cards.Insert(index, card);
        card.Position = new Vector2(-Constants.CARD_WIDTH / 2, index * Constants.CARD_STACK_SEPARATION);
    }

    public void ReparentCard(int index, Card card)
    {
        Control cardContainer = GetNode<Control>("Cards");
        card.GetParent().RemoveChild(card);
        cardContainer.AddChild(card);
        cardContainer.MoveChild(card, index);
        card.Position = new Vector2(-Constants.CARD_WIDTH / 2, index * Constants.CARD_STACK_SEPARATION);
    }

    public void OrderCards()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].Position = new Vector2(-Constants.CARD_WIDTH / 2, i * Constants.CARD_STACK_SEPARATION);
        }
    }
}
