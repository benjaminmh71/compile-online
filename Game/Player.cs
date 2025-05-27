using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class Player : Node
{
    public int id;
    public List<Card> deck = new List<Card>();
    public List<Card> hand = new List<Card>();
    List<Card> empty = new List<Card>();
    int oppId;
    int nOppCards = 0;

    public Player(int id, int oppId)
    {
        this.id = id;
        this.oppId = oppId;
        for (int i = 0; i < 18; i++)
        {
            PackedScene cardScene = GD.Load("res://Game/Card.tscn") as PackedScene;
            Card card = cardScene.Instantiate<Card>();
            card.info = new CardInfo();
            deck.Add(card);
        }

        Game.instance.mousePosition.CardPlaced += OnPlay;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void StartTurn()
    {
        Game.instance.turnLabel.Text = "It is your turn.";
        // TODO: Start of turn effects
        // TODO: Check compile
        if (hand.Count == 0)
        {
            // TODO: Refresh
        }
        MousePosition.SetSelectedCards(hand);
    }

    public void EndTurn()
    {
        // TODO: End of turn effects
        Game.instance.turnLabel.Text = "It is your opponent's turn.";
        MousePosition.SetSelectedCards(empty);
        RpcId(oppId, nameof(StartTurn));
    }

    public void Refresh()
    {
        while (hand.Count < 5)
        {
            Draw();
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
        Game.instance.handCardsContainer.AddChild(card);
        RpcId(oppId, nameof(OppDraw));
    }

    public void OnPlay(Protocol protocol, Card card)
    {
        hand.Remove(card);
        // TODO: On cover effects
        protocol.AddCard(card);
        // TODO: Change "Apathy 5" to card's name, find that card
        RpcId(oppId, nameof(OppPlay), "Apathy 5", Game.instance.GetProtocols(true).FindIndex(p => p == protocol));
        // TODO: On play effects
        EndTurn();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppPlay(String cardName, int protocolIndex)
    {
        PackedScene cardScene = GD.Load("res://Game/Card.tscn") as PackedScene;
        Card card = cardScene.Instantiate<Card>();
        card.info = new CardInfo();
        List<Protocol> protocols = Game.instance.GetProtocols(false);
        protocols[protocolIndex].AddOppCard(card);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppDraw()
    {
        PackedScene cardScene = GD.Load("res://Game/Card.tscn") as PackedScene;
        Card card = cardScene.Instantiate<Card>();
        card.info = new CardInfo();
        card.flipped = true;
        Game.instance.oppCardsContainer.AddChild(card);
    }
}
