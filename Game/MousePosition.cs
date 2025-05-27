using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class MousePosition : Control
{
    [Signal]
    public delegate void CardPlacedEventHandler(Protocol protocol, Card card);

    public bool dragging = false;
    public Card draggedCard = null;
    static List<Card> selectedCards = new List<Card>();

    public override void _Process(double delta)
    {
        GlobalPosition = GetGlobalMousePosition();

        if (Input.IsActionPressed("click") && !dragging)
        {
            dragging = true;
            foreach (Card card in selectedCards)
            {
                if (GlobalPosition.X > card.GlobalPosition.X &&
                GlobalPosition.X < card.GlobalPosition.X + Constants.CARD_WIDTH &&
                GlobalPosition.Y > card.GlobalPosition.Y &&
                GlobalPosition.Y < card.GlobalPosition.Y + Constants.CARD_HEIGHT)
                {
                    draggedCard = card;
                    card.GetParent().RemoveChild(card);
                    Game.instance.mousePosition.AddChild(card);
                    card.Position = Vector2.Zero;
                }
            }
        }
        if (Input.IsActionJustReleased("click") && dragging)
        {
            dragging = false;
            if (draggedCard != null)
            {
                Protocol protocol = Game.instance.GetHoveredProtocol();
                if (protocol != null)
                {
                    EmitSignal("CardPlaced", protocol, draggedCard);
                }
                else
                {
                    draggedCard.GetParent().RemoveChild(draggedCard);
                    Game.instance.handCardsContainer.AddChild(draggedCard); // Change this
                }
                draggedCard = null;
            }
        }
    }

    public static void SetSelectedCards(List<Card> cards)
    {
        selectedCards = cards;
    }
}
