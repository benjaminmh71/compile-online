using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

public partial class Player : Node
{
    public struct PassiveLocation
    {
        public int line;
        public bool local;

        public PassiveLocation(int line, bool local)
        {
            this.line = line;
            this.local = local;
        }
    }
    public int id;
    public List<Card> deck = new List<Card>();
    public List<Card> hand = new List<Card>();
    public List<Card> discard = new List<Card>();
    public List<Card> oppDeck = new List<Card>();
    public List<Card> oppHand = new List<Card>();
    public Dictionary<CardInfo.Passive, PassiveLocation?> passives = 
        new Dictionary<CardInfo.Passive, PassiveLocation?>();
    List<Card> empty = new List<Card>();
    List<Protocol> emptyp = new List<Protocol>();
    int oppId;
    int nOppCards = 0;
    bool hasControl = false;
    bool oppResponse = false;

    public Player(int id, int oppId)
    {
        this.id = id;
        this.oppId = oppId;

        foreach (CardInfo.Passive passive in Enum.GetValues(typeof(CardInfo.Passive)))
        {
            passives[passive] = null;
        }

        foreach (Protocol protocol in Game.instance.GetProtocols(true))
        {
            foreach (CardInfo cardInfo in protocol.info.cards)
            {
                PackedScene cardScene = GD.Load("res://Game/Card.tscn") as PackedScene;
                Card card = cardScene.Instantiate<Card>();
                card.SetCardInfo(cardInfo);
                deck.Add(card);
            }
        }
        Utility.Shuffle(deck);
    }

    public void Init()
    {
        RpcId(oppId, nameof(OppSetDeck), deck.Select<Card, String>((Card c) => c.info.GetCardName()).ToArray());
        Draw(5);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void StartTurn()
    {
        Game.instance.promptLabel.Text = "It is your turn.";

        // Check control:
        int controlledLines = 0;
        foreach (Protocol p in Game.instance.GetProtocols(true))
        {
            int total = Game.instance.SumStack(p);
            int oppTotal = Game.instance.SumStack(Game.instance.GetOpposingProtocol(p));
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
            int total = Game.instance.SumStack(p);
            if (total >= 10)
            {
                int oppTotal = Game.instance.SumStack(Game.instance.GetOpposingProtocol(p));
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
            await Compile(compilableProtcols[0]);
            EndTurn();
            return;
        } else if (compilableProtcols.Count > 1)
        {
            PromptManager.PromptAction([PromptManager.Prompt.Compile], compilableProtcols);

            Response compileResponse = await WaitForResponse();

            await Compile(compileResponse.protocol);
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
        PromptManager.PromptAction([PromptManager.Prompt.Play, PromptManager.Prompt.Refresh], hand, Game.instance.GetProtocols(true));

        Response response = await WaitForResponse();

        if (response.type == PromptManager.Prompt.Play)
        {
            await Play(response.protocol, response.card, response.flipped);
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
        MousePosition.SetSelectedCards(empty, emptyp);
        RpcId(oppId, nameof(StartTurn));
    }

    public async Task UseControl()
    {
        /*hasControl = false; FINISH LATER
        Game.instance.control.OffsetTop = Constants.CONTROL_TOP;
        Game.instance.control.OffsetBottom = Constants.CONTROL_BOTTOM;
        RpcId(oppId, nameof(OppUseControl));

        PromptManager.PromptAction([PromptManager.Prompt.Control], 
            Game.instance.GetProtocols(true).Concat(Game.instance.GetProtocols(false)).ToList());

        Response response = await WaitForResponse();*/

        
    }

    public async Task Refresh()
    {
        if (hasControl)
        {
            await UseControl();
        }

        Draw(5 - hand.Count);
    }

    public async Task Compile(Protocol protocol)
    {
        // TODO: On compile effects (namely Speed 2)
        for (int i = protocol.cards.Count - 1; i >= 0; i--)
        {
            SendToDiscard(protocol.cards[i]);
        }
        Protocol oppProtocol = Game.instance.GetOpposingProtocol(protocol);
        for (int i = oppProtocol.cards.Count - 1; i >= 0; i--)
        {
            SendToDiscard(oppProtocol.cards[i]);
        }
        protocol.compiled = true;
        protocol.Render();
        RpcId(oppId, nameof(OppCompile), Game.instance.GetProtocols(true).FindIndex((Protocol p) => p == protocol));

        int compiledProtocols = 0;
        foreach (Protocol p in Game.instance.GetProtocols(true))
        {
            if (p.compiled) compiledProtocols++;
        }
        if (compiledProtocols >= 3)
        {
            Game.instance.victoryPanel.Visible = true;
            RpcId(oppId, nameof(OppLose));
            await WaitForResponse();
        }
    }

    public async Task Play(Protocol protocol, Card card, bool flipped)
    {
        hand.Remove(card);
        if (protocol.cards.Count > 0)
        {
            if (!protocol.cards[protocol.cards.Count - 1].flipped)
                await protocol.cards[protocol.cards.Count - 1].info.OnCover(protocol.cards[protocol.cards.Count - 1]);
            protocol.cards[protocol.cards.Count - 1].covered = true;
        }
        card.flipped = flipped;
        protocol.AddCard(card);
        foreach (CardInfo.Passive passive in card.info.passives)
        {
            passives[passive] = new PassiveLocation(Game.instance.Line(protocol), true);
        }
        card.Render();
        RpcId(oppId, nameof(OppPlay),
            card.info.GetCardName(), Game.instance.GetProtocols(true).FindIndex(p => p == protocol), flipped);
        await Uncover(card, protocol);
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
        //GD.Print(deck.Count);
        if (deck.Count == 0) // Shuffle in discard
        {
            if (discard.Count == 0) return;
            while (discard.Count > 0)
            {
                Card c = discard[Utility.random.RandiRange(0, discard.Count - 1)];
                discard.Remove(c);
                deck.Add(c);
            }
            Game.instance.localDeckTop.placeholder = false;
            Game.instance.localDiscardTop.placeholder = true;
            Game.instance.localDeckTop.Render();
            Game.instance.localDiscardTop.Render();
            RpcId(oppId, nameof(OppSetDeck), deck.Select<Card, String>((Card c) => c.info.GetCardName()).ToArray());
        }
        if (deck.Count == 1) // Deck has no more cards
        {
            Game.instance.localDeckTop.placeholder = true;
            Game.instance.localDeckTop.Render();
        }
        Card card = deck[0];
        deck.Remove(card);
        hand.Add(card);
        Game.instance.handCardsContainer.AddChild(card);
        RpcId(oppId, nameof(OppDraw));
    }

    public async Task Discard(int n)
    {
        String prevText = Game.instance.promptLabel.Text;
        Game.instance.promptLabel.Text = "Discard " + n + (n > 1 ? " cards." : " card.");

        for (int i = 0; i < n; i++)
        {
            await Discard();
        }

        Game.instance.promptLabel.Text = prevText;

        // Todo: on discard
    }

    public async Task Discard()
    {
        PromptManager.PromptAction([PromptManager.Prompt.Select], hand);
        Response response = await WaitForResponse();
        SendToDiscard(response.card);
    }

    public async Task Flip(Card card)
    {
        card.flipped = !card.flipped;
        card.Render();
        var cardLocation = Game.instance.GetCardLocation(card);
        if (card.flipped)
        {
            foreach (CardInfo.Passive passive in card.info.passives)
            {
                passives[passive] = null;
            }
        }
        if (!card.flipped)
        {
            foreach (CardInfo.Passive passive in card.info.passives)
            {
                passives[passive] = new PassiveLocation
                    (Game.instance.Line(Game.instance.GetProtocols(cardLocation.local)[cardLocation.protocolIndex]), true);
            }
            await Uncover(card, Game.instance.GetProtocols(cardLocation.local)[cardLocation.protocolIndex]);
        }
        RpcId(oppId, nameof(OppFlip), cardLocation.local, cardLocation.protocolIndex, cardLocation.cardIndex);
        // TODO: Wait for opponent response (for flipped up actions)
    }

    public async Task Shift(Card card, Protocol protocol)
    {
        var cardLocation = Game.instance.GetCardLocation(card);
        Protocol sourceProtocol = Game.instance.GetProtocols(cardLocation.local)[cardLocation.protocolIndex];
        sourceProtocol.cards.Remove(card);
        protocol.AddCard(card);
        if (card.covered)
        {
            card.covered = false;
        }
        else if (sourceProtocol.cards.Count > 0)
        {
            sourceProtocol.cards[sourceProtocol.cards.Count - 1].covered = false;
        }
        RpcId(oppId, nameof(OppShift), cardLocation.local,
            Game.instance.GetProtocols(cardLocation.local).FindIndex((Protocol p) => p == protocol),
            cardLocation.cardIndex, cardLocation.protocolIndex);
        await WaitForOppResponse();
        if (card.covered)
        {
            await Uncover(card, protocol);
        }
        else if (sourceProtocol.cards.Count > 0)
        {
            await Uncover(sourceProtocol.cards[sourceProtocol.cards.Count - 1], sourceProtocol);
        }
    }

    public void SendToDiscard(Card card)
    {
        if (hand.Contains(card))
        {
            hand.Remove(card);
            card.GetParent().RemoveChild(card);
            discard.Add(card);
            Game.instance.localDiscardTop.SetCardInfo(card.info);
            Game.instance.localDiscardTop.placeholder = false;
            Game.instance.localDiscardTop.Render();
            RpcId(oppId, nameof(OppSendToDiscard), card.info.GetCardName());
        }
        else
        {
            var cardLocation = Game.instance.GetCardLocation(card);
            card.GetParent().RemoveChild(card);
            Game.instance.GetProtocols(cardLocation.local)[cardLocation.protocolIndex].cards.Remove(card);
            card.Reset();
            foreach (CardInfo.Passive passive in card.info.passives)
            {
                passives[passive] = null;
            }
            if (cardLocation.local)
            {
                discard.Add(card);
                Game.instance.localDiscardTop.SetCardInfo(card.info);
                Game.instance.localDiscardTop.placeholder = false;
                Game.instance.localDiscardTop.Render();
            }
            else
            {
                Game.instance.oppDiscardTop.SetCardInfo(card.info);
                Game.instance.oppDiscardTop.placeholder = false;
                Game.instance.oppDiscardTop.Render();
            }
            RpcId(oppId, nameof(OppSendToDiscard),
                cardLocation.local, cardLocation.protocolIndex, cardLocation.cardIndex);
        }
    }

    public async Task Uncover(Card card, Protocol protocol)
    {
        if (Game.instance.IsLocal(card) && !card.flipped && !LineContainsPassive(protocol, CardInfo.Passive.NoMiddleCommands))
            await card.info.OnPlay(card);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppCompile(int protocolIndex)
    {
        Protocol protocol = Game.instance.GetProtocols(false)[protocolIndex];
        protocol.compiled = true;
        protocol.Render();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppPlay(String cardName, int protocolIndex, bool flipped)
    {
        Card card = oppHand.Find(handCard => handCard.info.GetCardName() == cardName);
        oppHand.Remove(card);
        card.flipped = flipped;
        List<Protocol> protocols = Game.instance.GetProtocols(false);
        if (protocols[protocolIndex].cards.Count > 0)
        {
            protocols[protocolIndex].cards[protocols[protocolIndex].cards.Count - 1].covered = true;
        }
        foreach (CardInfo.Passive passive in card.info.passives)
        {
            passives[passive] = new PassiveLocation(Game.instance.Line(protocols[protocolIndex]), false);
        }
        protocols[protocolIndex].AddCard(card);
        card.Render();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppSetDeck(String[] cards)
    {
        foreach (String cardName in cards)
        {
            PackedScene cardScene = GD.Load("res://Game/Card.tscn") as PackedScene;
            Card card = cardScene.Instantiate<Card>();
            card.SetCardInfo(Cardlist.GetCard(cardName));
            oppDeck.Add(card);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppDraw()
    {
        if (oppDeck.Count == 1) // Deck has no more cards
        {            
            Game.instance.oppDeckTop.placeholder = true;
            Game.instance.oppDiscardTop.placeholder = true;
            Game.instance.oppDeckTop.Render();
            Game.instance.oppDiscardTop.Render();
        }
        else
        {
            Game.instance.oppDeckTop.placeholder = false;
            Game.instance.oppDeckTop.Render();
        }
        Card card = oppDeck[0];
        oppDeck.Remove(card);
        oppHand.Add(card);
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppLose()
    {
        Game.instance.losePanel.Visible = true;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void OppFlip(bool local, int protocolIndex, int cardIndex)
    {
        Card card = Game.instance.FindCard(!local, protocolIndex, cardIndex);
        card.flipped = !card.flipped;
        card.Render();
        if (card.flipped)
        {
            foreach (CardInfo.Passive passive in card.info.passives)
            {
                passives[passive] = null;
            }
        }
        if (!card.flipped)
        {
            foreach (CardInfo.Passive passive in card.info.passives)
            {
                passives[passive] = new PassiveLocation(Game.instance.Line(Game.instance.GetProtocols(local)[protocolIndex]), false);
            }
            await Uncover(card, Game.instance.GetProtocols(local)[protocolIndex]);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void OppShift(bool local, int protocolIndex, int cardIndex, int sourceProtocolIndex)
    {
        Protocol protocol = Game.instance.GetProtocols(!local)[protocolIndex];
        Protocol sourceProtocol = Game.instance.GetProtocols(!local)[sourceProtocolIndex];
        Card card = sourceProtocol.cards[cardIndex];
        sourceProtocol.cards.Remove(card);
        protocol.AddCard(card);
        if (card.covered)
        {
            card.covered = false;
            await Uncover(card, protocol);
        }
        else if (sourceProtocol.cards.Count > 0)
        {
            sourceProtocol.cards[sourceProtocol.cards.Count - 1].covered = false;
            await Uncover(sourceProtocol.cards[sourceProtocol.cards.Count - 1], sourceProtocol);
        }

        RpcId(oppId, nameof(OppResponse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppSendToDiscard(String cardName)
    {
        Card card = oppHand.Find((Card c) => c.info.GetCardName() == cardName);
        card.QueueFree();
        Game.instance.oppDiscardTop.SetCardInfo(card.info);
        Game.instance.oppDiscardTop.placeholder = false;
        Game.instance.oppDiscardTop.Render();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppSendToDiscard(bool local, int protocolIndex, int cardIndex)
    {
        Protocol protocol = Game.instance.GetProtocols(!local)[protocolIndex];
        Card card = Game.instance.FindCard(!local, protocolIndex, cardIndex);
        protocol.cards.Remove(card);
        card.QueueFree();
        foreach (CardInfo.Passive passive in card.info.passives)
        {
            passives[passive] = null;
        }
        if (local)
        {
            Game.instance.oppDiscardTop.SetCardInfo(card.info);
            Game.instance.oppDiscardTop.placeholder = false;
            Game.instance.oppDiscardTop.Render();
        } else
        {
            Game.instance.localDiscardTop.SetCardInfo(card.info);
            Game.instance.localDiscardTop.placeholder = false;
            Game.instance.localDiscardTop.Render();
        }
    }

    public bool LineContainsPassive(Protocol p, CardInfo.Passive passive)
    {
        return passives[passive] != null && passives[passive].Value.line == Game.instance.Line(p);
    }

    public bool StackContainsPassive(bool local, Protocol p, CardInfo.Passive passive)
    {
        return passives[passive] != null && passives[passive].Value.line == Game.instance.Line(p)
            && passives[passive].Value.local == local;
    }

    public async Task<Response> WaitForResponse()
    {
        while (PromptManager.response == null) 
        {
            await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        }
        Response response = PromptManager.response;
        PromptManager.response = null;
        return response;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppResponse()
    {
        oppResponse = true;
    }

    public async Task WaitForOppResponse()
    {
        while (!oppResponse)
        {
            await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        }
        oppResponse = false;
    }
}