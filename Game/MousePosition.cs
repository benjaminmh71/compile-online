using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class MousePosition : Control
{
    [Signal]
    public delegate void CardClickedEventHandler(Card card);
    [Signal]
    public delegate void CardPlacedEventHandler(Protocol protocol, Card card);
    [Signal]
    public delegate void ProtocolSwappedEventHandler(Protocol protocolA, Protocol protocolB);

    bool dragging = false;
    Card draggedCard = null;
    Node draggedCardParent = null;
    int draggedCardIndex = -1;
    Control draggedProtocol = null;
    Protocol referencedProtocol = null;
    static List<Card> clickableCards = new List<Card>();
    // Click and drag cards:
    static List<Card> selectedCards = new List<Card>();
    // Protocol click/drag cards can be placed:
    static List<Protocol> destinationProtocols = new List<Protocol>();
    // Click and drag protocols:
    static List<Protocol> selectedProtocols = new List<Protocol>();

    public static Func<Card, Protocol, bool, bool> CanBePlaced = (Card c, Protocol p, bool facedown) => true;

    public override void _Process(double delta)
    {
        GlobalPosition = GetGlobalMousePosition();

        // TESTING
        if (Input.IsActionJustPressed("right_click") && Game.instance.GetHoveredProtocol() != null)
        {
            foreach (Card card in Game.instance.GetHoveredProtocol().cards)
            {
                GD.Print(card.info.GetCardName());
            }
            GD.Print(Game.instance.SumStack(Game.instance.GetHoveredProtocol()));
        }

        // Deck count:
        Card deckTop = Game.instance.localDeckTop;
        Game.instance.deckLabel.Visible = false;
        if (deckTop != null && Geometry2D.IsPointInPolygon(GlobalPosition,
                    [new Vector2(deckTop.GlobalPosition.X, deckTop.GlobalPosition.Y),
                    new Vector2(deckTop.GlobalPosition.X, deckTop.GlobalPosition.Y + Constants.CARD_HEIGHT),
                    new Vector2(deckTop.GlobalPosition.X + Constants.CARD_WIDTH,
                    deckTop.GlobalPosition.Y + Constants.CARD_HEIGHT),
                    new Vector2(deckTop.GlobalPosition.X + Constants.CARD_WIDTH, deckTop.GlobalPosition.Y)]))
        {
            Game.instance.deckLabel.Visible = true;
            Game.instance.deckLabel.Text = Game.instance.localPlayer.deck.Count.ToString();
        }
        Card oppDeckTop = Game.instance.oppDeckTop;
        Game.instance.oppDeckLabel.Visible = false;
        if (oppDeckTop != null && Geometry2D.IsPointInPolygon(GlobalPosition,
                    [new Vector2(oppDeckTop.GlobalPosition.X, oppDeckTop.GlobalPosition.Y),
                    new Vector2(oppDeckTop.GlobalPosition.X, oppDeckTop.GlobalPosition.Y + Constants.CARD_HEIGHT),
                    new Vector2(oppDeckTop.GlobalPosition.X + Constants.CARD_WIDTH,
                    oppDeckTop.GlobalPosition.Y + Constants.CARD_HEIGHT),
                    new Vector2(oppDeckTop.GlobalPosition.X + Constants.CARD_WIDTH, oppDeckTop.GlobalPosition.Y)]))
        {
            Game.instance.oppDeckLabel.Visible = true;
            Game.instance.oppDeckLabel.Text = Game.instance.localPlayer.oppDeck.Count.ToString();
        }

        // Focused card:
        Game.instance.focusedCard.Visible = false;
        if (!dragging)
        {
            foreach (Card card in Game.instance.GetCards())
            {
                if (Geometry2D.IsPointInPolygon(GlobalPosition,
                    [new Vector2(card.GlobalPosition.X, card.GlobalPosition.Y),
                    new Vector2(card.GlobalPosition.X, card.GlobalPosition.Y + (Game.instance.IsLocal(card) ? 1 : -1) *
                    (card.covered ? Constants.CARD_STACK_SEPARATION : Constants.CARD_HEIGHT)),
                    new Vector2(card.GlobalPosition.X + (Game.instance.IsLocal(card) ? 1 : -1) * Constants.CARD_WIDTH,
                    card.GlobalPosition.Y + (Game.instance.IsLocal(card) ? 1 : -1) *
                    (card.covered ? Constants.CARD_STACK_SEPARATION : Constants.CARD_HEIGHT)),
                    new Vector2(card.GlobalPosition.X + (Game.instance.IsLocal(card) ? 1 : -1) * Constants.CARD_WIDTH, card.GlobalPosition.Y)]))
                {
                    Game.instance.focusedCard.Visible = true;
                    Game.instance.focusedCard.info = card.info;
                    Game.instance.focusedCard.flipped = !Game.instance.IsLocal(card) && card.flipped;
                    Game.instance.focusedCard.Render();
                    Vector2 sizeDifference = Game.instance.focusedCard.Size - card.Size;
                    if (Game.instance.IsLocal(card))
                        Game.instance.focusedCard.GlobalPosition = new Vector2(
                            card.GlobalPosition.X - sizeDifference.X / 2, card.GlobalPosition.Y - sizeDifference.Y / 2);
                    else
                        Game.instance.focusedCard.GlobalPosition = new Vector2(
                            card.GlobalPosition.X - sizeDifference.X / 2 - card.Size.X, 
                            card.GlobalPosition.Y - sizeDifference.Y / 2 - card.Size.Y);

                }
            }
        }

        if (Input.IsActionJustPressed("click"))
        {
            foreach (Card card in clickableCards)
            {
                if (Geometry2D.IsPointInPolygon(GlobalPosition,
                    [new Vector2(card.GlobalPosition.X, card.GlobalPosition.Y),
                    new Vector2(card.GlobalPosition.X, card.GlobalPosition.Y + (Game.instance.IsLocal(card) ? 1 : -1) * 
                    (card.covered ? Constants.CARD_STACK_SEPARATION : Constants.CARD_HEIGHT)),
                    new Vector2(card.GlobalPosition.X + (Game.instance.IsLocal(card) ? 1 : -1) * Constants.CARD_WIDTH,
                    card.GlobalPosition.Y + (Game.instance.IsLocal(card) ? 1 : -1) * 
                    (card.covered ? Constants.CARD_STACK_SEPARATION : Constants.CARD_HEIGHT)),
                    new Vector2(card.GlobalPosition.X + (Game.instance.IsLocal(card) ? 1 : -1) * Constants.CARD_WIDTH, card.GlobalPosition.Y)]))
                {
                    EmitSignal("CardClicked", card);
                }
            }
        }

        if (Input.IsActionPressed("click") && !dragging)
        {
            dragging = true;

            foreach (Card card in selectedCards)
            {
                if (Geometry2D.IsPointInPolygon(GlobalPosition,
                    [new Vector2(card.GlobalPosition.X, card.GlobalPosition.Y),
                    new Vector2(card.GlobalPosition.X, card.GlobalPosition.Y + (Game.instance.IsLocal(card) ? 1 : -1) * 
                    (card.covered ? Constants.CARD_STACK_SEPARATION : Constants.CARD_HEIGHT)),
                    new Vector2(card.GlobalPosition.X + (Game.instance.IsLocal(card) ? 1 : -1) * Constants.CARD_WIDTH,
                    card.GlobalPosition.Y + (Game.instance.IsLocal(card) ? 1 : -1) * 
                    (card.covered ? Constants.CARD_STACK_SEPARATION : Constants.CARD_HEIGHT)),
                    new Vector2(card.GlobalPosition.X + (Game.instance.IsLocal(card) ? 1 : -1) * Constants.CARD_WIDTH, card.GlobalPosition.Y)]))
                {
                    draggedCard = card;
                    draggedCardParent = card.GetParent();
                    draggedCardIndex = draggedCardParent.GetChildren().IndexOf(card);
                    card.GetParent().RemoveChild(card);
                    AddChild(card);
                    card.Position = Vector2.Zero;
                }
            }

            foreach (Protocol protocol in selectedProtocols)
            {
                if (Geometry2D.IsPointInPolygon(GlobalPosition,
                    [new Vector2(protocol.GlobalPosition.X, protocol.GlobalPosition.Y),
                    new Vector2(protocol.GlobalPosition.X, protocol.GlobalPosition.Y + (Game.instance.IsLocal(protocol) ? 1 : -1) * Constants.CARD_HEIGHT),
                    new Vector2(protocol.GlobalPosition.X + (Game.instance.IsLocal(protocol) ? 1 : -1) * Constants.CARD_WIDTH,
                    protocol.GlobalPosition.Y + (Game.instance.IsLocal(protocol) ? 1 : -1) * Constants.CARD_HEIGHT),
                    new Vector2(protocol.GlobalPosition.X + (Game.instance.IsLocal(protocol) ? 1 : -1) * Constants.CARD_WIDTH, protocol.GlobalPosition.Y)]))
                {
                    Control copiedProtocol = protocol.Duplicate(0) as Control;
                    draggedProtocol = copiedProtocol;
                    referencedProtocol = protocol;
                    protocol.HideProtocol();
                    copiedProtocol.GetNode("Cards").QueueFree();
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
                if (protocol != null && destinationProtocols.Contains(protocol) && !protocol.cards.Contains(draggedCard)
                    && Game.instance.IsLocal(draggedCard) == Game.instance.IsLocal(protocol)
                    && CanBePlaced(draggedCard, protocol, Game.instance.flippedCheckbox.GetNode<CheckBox>("CheckBox").ButtonPressed))
                {
                    EmitSignal("CardPlaced", protocol, draggedCard);
                }
                else
                {
                    if (draggedCardParent.GetParent() is Protocol)
                    {
                        (draggedCardParent.GetParent() as Protocol).ReparentCard(draggedCardIndex, draggedCard);
                    }
                    else
                    {
                        draggedCard.GetParent().RemoveChild(draggedCard);
                        draggedCardParent.AddChild(draggedCard);
                        draggedCardParent.MoveChild(draggedCard, draggedCardIndex);
                    }
                }
                draggedCard = null;
            }

            if (draggedProtocol != null)
            {
                Protocol otherProtocol = Game.instance.GetHoveredProtocol();
                if (otherProtocol != null && 
                    (Game.instance.IsLocal(referencedProtocol) == Game.instance.IsLocal(otherProtocol))
                    && referencedProtocol != otherProtocol && selectedProtocols.Contains(otherProtocol))
                {
                    Swap(referencedProtocol, otherProtocol);
                    draggedProtocol.QueueFree();
                    referencedProtocol.UnHideProtocol();
                    EmitSignal("ProtocolSwapped", referencedProtocol, otherProtocol);
                    draggedProtocol = null;
                    referencedProtocol = null;
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

    public static void Swap(Protocol A, Protocol B)
    {
        int oldPosition = A.GetIndex();
        int newPosition = B.GetIndex();
        Control oldCards = A.GetNode<Control>("Cards");
        Control newCards = B.GetNode<Control>("Cards");
        A.RemoveChild(oldCards);
        B.RemoveChild(newCards);
        A.AddChild(newCards);
        B.AddChild(oldCards);
        List<Card> tempCards = A.cards;
        A.cards = B.cards;
        B.cards = tempCards;
        if (Game.instance.IsLocal(A))
        {
            Game.instance.localProtocolsContainer.MoveChild(A, newPosition);
            Game.instance.localProtocolsContainer.MoveChild(B, oldPosition);
        }
        else
        {
            Game.instance.oppProtocolsContainer.MoveChild(A, newPosition);
            Game.instance.oppProtocolsContainer.MoveChild(B, oldPosition);
        }
    }

    public static void SetClickableCards(List<Card> cards)
    {
        clickableCards = cards;
    }

    public static void SetSelectedCards(List<Card> cards, List<Protocol> protocols)
    {
        selectedCards = cards;
        destinationProtocols = protocols;
    }

    public static void SetSelectedProtocols(List<Protocol> protocols)
    {
        selectedProtocols = protocols;
    }

    public static void ResetSelections()
    {
        clickableCards = new List<Card>();
        selectedCards = new List<Card>();
        selectedProtocols = new List<Protocol>();
    }
}
