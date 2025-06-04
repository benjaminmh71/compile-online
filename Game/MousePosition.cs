using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class MousePosition : Control
{
    [Signal]
    public delegate void CardPlacedEventHandler(Protocol protocol, Card card);
    [Signal]
    public delegate void ProtocolSwappedEventHandler(Protocol protocol);

    bool dragging = false;
    Card draggedCard = null;
    Protocol draggedProtocol = null;
    Protocol referencedProtocol = null;
    static List<Card> selectedCards = new List<Card>();
    static List<Protocol> selectedProtocols = new List<Protocol>();

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
                    AddChild(card);
                    card.Position = Vector2.Zero;
                }
            }

            foreach (Protocol protocol in selectedProtocols)
            {
                if (GlobalPosition.X > protocol.GlobalPosition.X &&
                GlobalPosition.X < protocol.GlobalPosition.X + Constants.PROTOCOL_WIDTH &&
                GlobalPosition.Y > protocol.GlobalPosition.Y &&
                GlobalPosition.Y < protocol.GlobalPosition.Y + Constants.PROTOCOL_HEIGHT)
                {
                    Protocol copiedProtocol = protocol.Duplicate() as Protocol;
                    draggedProtocol = copiedProtocol;
                    referencedProtocol = protocol;
                    protocol.HideProtocol();
                    copiedProtocol.GetParent().RemoveChild(copiedProtocol);
                    AddChild(copiedProtocol);
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

            if (draggedProtocol != null)
            {
                Protocol otherProtocol = Game.instance.GetHoveredProtocol();
                if (otherProtocol != null && 
                    (Game.instance.IsLocal(referencedProtocol) == Game.instance.IsLocal(otherProtocol)))
                {
                    int oldPosition = referencedProtocol.GetIndex();
                    int newPosition = otherProtocol.GetIndex();
                    Control oldCards = referencedProtocol.GetNode<Control>("Cards");
                    Control newCards = otherProtocol.GetNode<Control>("Cards");
                    referencedProtocol.RemoveChild(oldCards);
                    otherProtocol.RemoveChild(newCards);
                    referencedProtocol.AddChild(newCards);
                    otherProtocol.AddChild(oldCards);
                    if (Game.instance.IsLocal(referencedProtocol))
                    {
                        Game.instance.localProtocolsContainer.MoveChild(referencedProtocol, newPosition);
                        Game.instance.localProtocolsContainer.MoveChild(otherProtocol, oldPosition);
                    }
                    else
                    {
                        Game.instance.oppProtocolsContainer.MoveChild(referencedProtocol, newPosition);
                        Game.instance.oppProtocolsContainer.MoveChild(otherProtocol, oldPosition);
                    }
                    draggedProtocol.QueueFree();
                    referencedProtocol.UnHideProtocol();
                    draggedProtocol = null;
                    referencedProtocol = null;
                    EmitSignal("ProtocolSwapped", referencedProtocol);
                } 
                else
                {
                    draggedProtocol.QueueFree();
                    referencedProtocol.UnHideProtocol();
                    draggedProtocol = null;
                    referencedProtocol = null;
                }
            }
        }
    }

    public static void SetSelectedCards(List<Card> cards)
    {
        selectedCards = cards;
    }

    public static void SetSelectedProtocols(List<Protocol> protocols)
    {
        selectedProtocols = protocols;
    }
}
