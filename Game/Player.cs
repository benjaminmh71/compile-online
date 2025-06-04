using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Player : Node
{
    public int id;
    public List<Card> deck = new List<Card>();
    public List<Card> hand = new List<Card>();
    List<Card> empty = new List<Card>();
    int oppId;
    int nOppCards = 0;
    bool hasControl = false;

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
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void StartTurn()
    {
        Game.instance.promptLabel.Text = "It is your turn.";

        // Check control:
        int controlledLines = 0;
        foreach (Protocol p in Game.instance.GetProtocols(true))
        {
            int total = 0;
            foreach (Card c in p.cards)
            {
                total += c.info.value;
            }
            int oppTotal = 0;
            foreach (Card c in Game.instance.GetOpposingProtocol(p).cards)
            {
                oppTotal += c.info.value;
            }
            if (total > oppTotal)
            {
                controlledLines++;
            }
        }
        if (controlledLines > 1)
        {
            hasControl = true;
            Game.instance.control.OffsetTop = Constants.CONTROL_PLAYER_TOP;
            Game.instance.control.OffsetBottom = Constants.CONTROL_PLAYER_BOTTOM;
            RpcId(oppId, nameof(OppGainControl));
        }

        // TODO: Start of turn effects

        // Compiling:
        List<Protocol> compilableProtcols = new List<Protocol>();
        foreach (Protocol p in Game.instance.GetProtocols(true))
        {
            int total = 0;
            foreach (Card c in p.cards)
            {
                total += c.info.value;
            }
            if (total >= 10)
            {
                int oppTotal = 0;
                foreach (Card c in Game.instance.GetOpposingProtocol(p).cards)
                {
                    oppTotal += c.info.value;
                }
                if (oppTotal < total)
                {
                    compilableProtcols.Add(p);
                }
            }
        }
        if (compilableProtcols.Count > 0 && hasControl)
        {
            await UseControl();
        }
        if (compilableProtcols.Count == 1)
        {
            Compile(compilableProtcols[0]);
            EndTurn();
            return;
        } else if (compilableProtcols.Count > 1)
        {
            PromptManager.PromptAction([PromptManager.Prompt.Compile], compilableProtcols);

            Response compileResponse = await WaitForResponse();

            Compile(compileResponse.protocol);
            EndTurn();
            return;
        }

        // Refresh if hand is empty:
        if (hand.Count == 0)
        {
            await Refresh();
            EndTurn();
            return;
        }

        // Play/refresh:
        PromptManager.PromptAction([PromptManager.Prompt.Play, PromptManager.Prompt.Refresh], hand);

        Response response = await WaitForResponse();

        if (response.type == PromptManager.Prompt.Play)
        {
            Play(response.protocol, response.card);
        }

        if (response.type == PromptManager.Prompt.Refresh)
        {
            await Refresh();
        }

        EndTurn();
    }

    public void EndTurn()
    {
        // TODO: End of turn effects
        Game.instance.promptLabel.Text = "It is your opponent's turn.";
        MousePosition.SetSelectedCards(empty);
        RpcId(oppId, nameof(StartTurn));
    }

    public async Task UseControl()
    {
        hasControl = false;
        Game.instance.control.OffsetTop = Constants.CONTROL_TOP;
        Game.instance.control.OffsetBottom = Constants.CONTROL_BOTTOM;
        RpcId(oppId, nameof(OppUseControl));

        PromptManager.PromptAction([PromptManager.Prompt.Control]);

        Response response = await WaitForResponse();


    }

    public async Task Refresh()
    {
        if (hasControl)
        {
            GD.Print("Using control");
            await UseControl();
        }

        Draw(5 - hand.Count);
    }

    public void Compile(Protocol protocol)
    {
        // TODO: On compile effects (namely Speed 2)
        foreach (Card c in protocol.cards)
        {
            c.QueueFree();
        }
        protocol.cards.Clear();
        foreach (Card c in Game.instance.GetOpposingProtocol(protocol).cards)
        {
            c.QueueFree();
        }
        Game.instance.GetOpposingProtocol(protocol).cards.Clear();
        protocol.compiled = true;
        protocol.Render();
        RpcId(oppId, nameof(OppCompile), Game.instance.GetProtocols(true).FindIndex((Protocol p) => p == protocol));
    }

    public void Play(Protocol protocol, Card card)
    {
        hand.Remove(card);
        // TODO: On cover effects
        protocol.AddCard(card);
        // TODO: Change "Apathy 5" to card's name, find that card
        RpcId(oppId, nameof(OppPlay), "Apathy 5", Game.instance.GetProtocols(true).FindIndex(p => p == protocol));
        // TODO: On play effects
    }

    public void Draw(int n)
    {
        if (n <= 0) return;

        // TODO: on draw effects

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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppCompile(int protocolIndex)
    {
        Protocol protocol = Game.instance.GetProtocols(false)[protocolIndex];
        foreach (Card c in protocol.cards)
        {
            c.QueueFree();
        }
        protocol.cards.Clear();
        foreach (Card c in Game.instance.GetOpposingProtocol(protocol).cards)
        {
            c.QueueFree();
        }
        Game.instance.GetOpposingProtocol(protocol).cards.Clear();
        protocol.compiled = true;
        protocol.Render();
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppGainControl()
    {
        hasControl = false;
        Game.instance.control.OffsetTop = Constants.CONTROL_OPP_TOP;
        Game.instance.control.OffsetBottom = Constants.CONTROL_OPP_BOTTOM;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppUseControl()
    {
        Game.instance.control.OffsetTop = Constants.CONTROL_TOP;
        Game.instance.control.OffsetBottom = Constants.CONTROL_BOTTOM;
    }

    public async Task<Response> WaitForResponse()
    {
        while (PromptManager.response == null) // Test this
        {
            await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        }
        Response response = PromptManager.response;
        PromptManager.response = null;
        return response;
    }
}
