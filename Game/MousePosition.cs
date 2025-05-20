using CompileOnline.Game;
using Godot;
using System;

public partial class MousePosition : Control
{
    public bool dragging = false;
    public Card draggedCard = null;

    public override void _Process(double delta)
    {
        GlobalPosition = GetGlobalMousePosition();

        if (Input.IsActionPressed("click") && !dragging)
        {
            dragging = true;
            foreach (Card card in Game.instance.localPlayer.hand)
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
                    Game.instance.localPlayer.hand.Remove(draggedCard);
                    protocol.AddCard(draggedCard);
                }
                else
                {
                    draggedCard.GetParent().RemoveChild(draggedCard);
                    Game.instance.handCardsContainer.AddChild(draggedCard);
                }
            }
        }
    }
}
