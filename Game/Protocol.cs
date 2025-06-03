using CompileOnline.Game;
using Godot;
using System;
using System.Collections;

public partial class Protocol : Control
{
    [Signal]
    public delegate void OnClickEventHandler(Protocol protocol);

    public ArrayList cards = new ArrayList();
    public bool compiled = false;

    public override void _Ready()
    {
        Render();
    }

    public override void _Process(double delta)
    {
        Vector2 GlobalMousePosition = GetGlobalMousePosition();

        if (Input.IsActionJustReleased("click") &&
            GlobalMousePosition.X > GlobalPosition.X &&
            GlobalMousePosition.X < GlobalPosition.X + Constants.PROTOCOL_WIDTH &&
            GlobalMousePosition.Y > GlobalPosition.Y &&
            GlobalMousePosition.Y < GlobalPosition.Y + Constants.PROTOCOL_HEIGHT)
        {
            EmitSignal("OnClick", this);
        }
    }

    public void Render()
    {
        GetNode("TextContainer").GetNode<Label>("Name").Text = "Apathy";
        if (compiled)
        {
            GetNode("TextContainer").GetNode<Label>("Compiled").Visible = true;
        } else
        {
            GetNode("TextContainer").GetNode<Label>("Compiled").Visible = false;
        }
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
