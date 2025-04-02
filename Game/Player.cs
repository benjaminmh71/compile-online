using Godot;
using System;
using System.Collections;

public partial class Player : Node
{
    public int id;
    Game game;
    ArrayList deck = new ArrayList();
    ArrayList hand = new ArrayList();
    int oppId;
    int nOppCards = 0;

    public Player(int id, int oppId, Game game)
    {
        this.id = id;
        this.oppId = oppId;
        this.game = game;
        for (int i = 0; i < 18; i++)
        {
            PackedScene cardScene = GD.Load("res://Game/Card.tscn") as PackedScene;
            Card card = cardScene.Instantiate<Card>();
            card.info = new CardInfo();
            deck.Add(card);
        }
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
        game.handCardsContainer.AddChild(card);
        RpcId(oppId, nameof(OppDraw));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppDraw()
    {
        PackedScene cardScene = GD.Load("res://Game/Card.tscn") as PackedScene;
        Card card = cardScene.Instantiate<Card>();
        card.info = new CardInfo();
        card.flipped = true;
        game.oppCardsContainer.AddChild(card);
    }
}
