using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using static CardInfo;

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
    public enum CommandType { PlayTop, Draw, Discard, Delete, Reveal, Give, Steal };
    public int id;
    public List<Card> deck = new List<Card>();
    public List<Card> hand = new List<Card>();
    public List<Card> discard = new List<Card>();
    public List<Card> oppDeck = new List<Card>();
    public List<Card> oppHand = new List<Card>();
    public Dictionary<CardInfo.Passive, PassiveLocation?> passives = new Dictionary<CardInfo.Passive, PassiveLocation?>();
    public Dictionary<CardInfo.TempEffect, int> tempEffects = new Dictionary<CardInfo.TempEffect, int>();
    public Func<Card, Protocol, bool, bool> CanBePlaced;
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

        foreach (CardInfo.TempEffect tempEffect in Enum.GetValues(typeof(CardInfo.TempEffect)))
        {
            tempEffects[tempEffect] = 0;
        }

        CanBePlaced = (Card c, Protocol p, bool facedown) =>
            (facedown || passives[Passive.OnlyFaceDown] == null) &&
            !(facedown && StackContainsPassive(false, p, CardInfo.Passive.NoFaceDown)) && 
            !StackContainsPassive(false, p, CardInfo.Passive.NoPlay) &&
            (facedown || p.info.name == c.info.protocol || Game.instance.GetOpposingProtocol(p).info.name == c.info.protocol);

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

        foreach(CardInfo.TempEffect tempEffect in Enum.GetValues(typeof(CardInfo.TempEffect)))
        {
            if (tempEffects[tempEffect] > 0) tempEffects[tempEffect] -= 1;
        }

        foreach (Card card in Game.instance.GetCards())
        {
            if (Game.instance.IsLocal(card) && !card.flipped)
                await card.info.OnStart(card);
        }

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

        // Compiling:
        List<int> compilableProtcols = new List<int>();
        foreach (Protocol p in Game.instance.GetProtocols(true))
        {
            int total = Game.instance.SumStack(p);
            if (total >= 10)
            {
                int oppTotal = Game.instance.SumStack(Game.instance.GetOpposingProtocol(p));
                if (oppTotal < total)
                {
                    compilableProtcols.Add(Game.instance.IndexOfProtocol(p));
                }
            }
        }
        if (compilableProtcols.Count > 0 && hasControl)
        {
            await UseControl();
        }
        if (compilableProtcols.Count == 1 && tempEffects[CardInfo.TempEffect.NoCompile] == 0)
        {
            await Compile(Game.instance.GetProtocols(true)[compilableProtcols[0]]);
            await EndTurn();
            return;
        } else if (compilableProtcols.Count > 1 && tempEffects[CardInfo.TempEffect.NoCompile] == 0)
        {
            PromptManager.PromptAction([PromptManager.Prompt.Compile],
                compilableProtcols.Select((int val) => Game.instance.GetProtocols(true)[val]).ToList());

            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "Select a line to compile.";
            Response compileResponse = await WaitForResponse();
            Game.instance.promptLabel.Text = prevText;

            await Compile(compileResponse.protocol);
            await EndTurn();
            return;
        }

        // Refresh if hand is empty:
        if (hand.Count == 0)
        {
            await Refresh();
            await EndTurn();
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

        await EndTurn();
    }

    public async Task EndTurn()
    {
        // Check cache:
        if (hand.Count > 5)
        {
            await Discard(hand.Count - 5);
        }

        foreach (Card card in Game.instance.GetCards())
        {
            if (Game.instance.IsLocal(card) && !card.flipped)
                await card.info.OnEnd(card);
        }

        Game.instance.promptLabel.Text = "It is your opponent's turn.";
        MousePosition.SetSelectedCards(empty, emptyp);
        RpcId(oppId, nameof(StartTurn));
    }

    public async Task UseControl()
    {
        hasControl = false;
        Game.instance.control.OffsetTop = Constants.CONTROL_TOP;
        Game.instance.control.OffsetBottom = Constants.CONTROL_BOTTOM;

        PromptManager.PromptAction([PromptManager.Prompt.Control], 
            Game.instance.GetProtocols());

        Response response = await WaitForResponse();

        List<String> protocolALocations = new List<String>();
        foreach (int p in response.swapA)
        {
            protocolALocations.Add(response.swapLocal + "," + p);
        }
        List<String> protocolBLocations = new List<String>();
        foreach (int p in response.swapB)
        {
            protocolBLocations.Add(response.swapLocal + "," + p);
        }

        RpcId(oppId, nameof(OppUseControl),
            Json.Stringify(new Godot.Collections.Array<String>(protocolALocations)),
            Json.Stringify(new Godot.Collections.Array<String>(protocolBLocations)));
    }

    public async Task Rearrange(Response response)
    {
        List<String> protocolALocations = new List<String>();
        foreach (int p in response.swapA)
        {
            protocolALocations.Add(response.swapLocal + "," + p);
        }
        List<String> protocolBLocations = new List<String>();
        foreach (int p in response.swapB)
        {
            protocolBLocations.Add(response.swapLocal + "," + p);
        }

        RpcId(oppId, nameof(OppRearrange),
            Json.Stringify(new Godot.Collections.Array<String>(protocolALocations)),
            Json.Stringify(new Godot.Collections.Array<String>(protocolBLocations)));
        await WaitForOppResponse();
    }

    public async Task Refresh()
    {
        if (hasControl)
        {
            await UseControl();
        }

        await Draw(5 - hand.Count);
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
        RpcId(oppId, nameof(OppCompile), Game.instance.IndexOfProtocol(protocol));

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

        foreach (Card c in Game.instance.GetCards())
        {
            if (Game.instance.IsLocal(c) && !c.flipped) await c.info.OnDelete(c);
        }
    }

    public async Task Play(Protocol protocol, Card card, bool flipped, bool top = false)
    {
        if (hand.Contains(card))
            hand.Remove(card);
        if (protocol.cards.Count > 0)
        {
            await Cover(protocol.cards[protocol.cards.Count - 1], protocol);
        }
        card.flipped = flipped;
        protocol.AddCard(card);
        foreach (CardInfo.Passive passive in card.info.passives)
        {
            if (!flipped)
                passives[passive] = new PassiveLocation(Game.instance.Line(protocol), true);
        }
        card.Render();
        RpcId(oppId, nameof(OppPlay),
            card.info.GetCardName(), Game.instance.IndexOfProtocol(protocol), flipped, top);
        await Uncover(card, protocol);
    }

    public async Task PlayTop(Protocol protocol)
    {
        if (deck.Count == 0 || !CanBePlaced(new Card(), protocol, true)) return;
        Card card = deck[0];
        deck.Remove(card);
        await Play(protocol, card, true, true);
    }

    public async Task PlayTopUnderneath(Protocol protocol)
    {
        if (deck.Count == 0 || !CanBePlaced(new Card(), protocol, true)) return;
        if (protocol.cards.Count == 0) return;
        Card card = deck[0];
        deck.Remove(card);
        card.flipped = true;
        protocol.InsertCard(protocol.cards.Count - 1, card);
        card.Render();
        protocol.OrderCards();
        RpcId(oppId, nameof(OppPlayTopUnderneath),
            Game.instance.IndexOfProtocol(protocol));
        await WaitForOppResponse();
    }

    public async Task Draw(int n)
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
            RpcId(oppId, nameof(OppSetDeck), deck.Select((Card c) => c.info.GetCardName()).ToArray());
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

    public async Task DrawFromOpp()
    {
        RpcId(oppId, nameof(OppDrawFromOpp));
        await WaitForOppResponse();
        if (oppDeck.Count == 0) return;
        if (oppDeck.Count == 1) // Deck has no more cards
        {
            Game.instance.oppDeckTop.placeholder = true;
            Game.instance.oppDeckTop.Render();
        }
        else
        {
            Game.instance.oppDeckTop.placeholder = false;
            Game.instance.oppDeckTop.Render();
        }
        Card card = oppDeck[0];
        oppDeck.Remove(card);
        hand.Add(card);
        Game.instance.handCardsContainer.AddChild(card);
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

        RpcId(oppId, nameof(OppDiscardTriggers));
        await WaitForOppResponse();
    }

    public async Task Discard()
    {
        if (hand.Count == 0) return;
        PromptManager.PromptAction([PromptManager.Prompt.Select], hand);
        Response response = await WaitForResponse();
        SendToDiscard(response.card);
    }

    public async Task Flip(Card card)
    {
        await card.info.OnFlip(card);
        if (Game.instance.GetProtocolOfCard(card) == null) return; // May leave field
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
        bool wasFlipped = card.flipped;
        bool wasCovered = card.covered;
        if (!wasFlipped)
        {
            foreach (CardInfo.Passive passive in card.info.passives)
            {
                passives[passive] = new PassiveLocation
                    (Game.instance.Line(Game.instance.GetProtocols(cardLocation.local)[cardLocation.protocolIndex]), true);
            }
        }
        RpcId(oppId, nameof(OppFlip), cardLocation.local, cardLocation.protocolIndex, cardLocation.cardIndex);
        await WaitForOppResponse();
        if (!wasFlipped && !wasCovered)
            await Uncover(card, Game.instance.GetProtocols(cardLocation.local)[cardLocation.protocolIndex]);
    }

    public async Task Delete(Card card) {
        bool wasCovered = card.covered;
        var cardLocation = Game.instance.GetCardLocation(card);
        Protocol protocol = Game.instance.GetProtocols(cardLocation.local)[cardLocation.protocolIndex];
        card.GetParent().RemoveChild(card);
        protocol.cards.Remove(card);
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
        if (!wasCovered && protocol.cards.Count > 0)
        {
            protocol.cards[protocol.cards.Count - 1].covered = false;
        }
        protocol.OrderCards();
        RpcId(oppId, nameof(OppDelete),
            cardLocation.local, cardLocation.protocolIndex, cardLocation.cardIndex);
        await WaitForOppResponse();
        if (!wasCovered && protocol.cards.Count > 0)
        {
            await Uncover(protocol.cards[protocol.cards.Count - 1], protocol);
        }
        foreach (Card c in Game.instance.GetCards())
        {
            if (Game.instance.IsLocal(c) && !c.flipped) await c.info.OnDelete(c);
        }
    }

    public async Task Return(Card card)
    {
        bool wasCovered = card.covered;
        var cardLocation = Game.instance.GetCardLocation(card);
        Protocol protocol = Game.instance.GetProtocols(cardLocation.local)[cardLocation.protocolIndex];
        card.GetParent().RemoveChild(card);
        protocol.cards.Remove(card);
        card.Reset();
        foreach (CardInfo.Passive passive in card.info.passives)
        {
            passives[passive] = null;
        }
        if (cardLocation.local)
        {
            hand.Add(card);
            Game.instance.handCardsContainer.AddChild(card);
        }
        else
        {
            oppHand.Add(card);
            card.flipped = true;
            Game.instance.oppCardsContainer.AddChild(card);
        }
        if (!wasCovered && protocol.cards.Count > 0)
        {
            protocol.cards[protocol.cards.Count - 1].covered = false;
        }
        protocol.OrderCards();
        RpcId(oppId, nameof(OppReturn),
            cardLocation.local, cardLocation.protocolIndex, cardLocation.cardIndex);
        await WaitForOppResponse();
        if (!wasCovered && protocol.cards.Count > 0)
        {
            await Uncover(protocol.cards[protocol.cards.Count - 1], protocol);
        }
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
        if (protocol.cards.Count > 1)
        {
            protocol.cards[protocol.cards.Count - 2].covered = true;
        }
        sourceProtocol.OrderCards();
        RpcId(oppId, nameof(OppShift), cardLocation.local,
            Game.instance.IndexOfProtocol(protocol),
            cardLocation.cardIndex, cardLocation.protocolIndex);
        await WaitForOppResponse();
        if (protocol.cards.Count > 1 && !protocol.cards[protocol.cards.Count - 2].flipped)
        {
            await Cover(protocol.cards[protocol.cards.Count - 2], protocol);
        }
        if (card.covered)
        {
            await Uncover(card, protocol);
        }
        else if (sourceProtocol.cards.Count > 0)
        {
            await Uncover(sourceProtocol.cards[sourceProtocol.cards.Count - 1], sourceProtocol);
        }
    }

    public void Reveal(List<Card> cards)
    {
        Control revealPanel = Game.instance.revealPanel;
        revealPanel.Visible = true;
        float size = Constants.CARD_WIDTH * cards.Count + Constants.REVEAL_PANEL_MARGINS;
        revealPanel.OffsetLeft = size * (float)-0.5;
        revealPanel.OffsetRight = size * (float)0.5;
        foreach (Node card in revealPanel.GetNode("MarginContainer").GetNode("Cards").GetChildren())
        {
            card.QueueFree();
        }
        foreach (Card card in cards)
        {
            Card tempCard = (Card)card.Duplicate();
            tempCard.info = card.info;
            revealPanel.GetNode("MarginContainer").GetNode("Cards").AddChild(tempCard);
        }
    }

    public void ApplyTempEffect(CardInfo.TempEffect effect, int time)
    {
        tempEffects[effect] = time;
        RpcId(oppId, nameof(OppApplyTempEffect), (int)effect, time);
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
            card.Render();
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

    public async Task MultiDelete(List<Card> cards)
    {
        List<Card> uncoveredCards = new List<Card>();
        foreach (Card card in cards)
        {
            Protocol protocol = Game.instance.GetProtocolOfCard(card);
            if (protocol.cards.IndexOf(card) > 0 && !cards.Contains(protocol.cards[protocol.cards.IndexOf(card) - 1]))
                uncoveredCards.Add(protocol.cards[protocol.cards.IndexOf(card) - 1]);
            SendToDiscard(card);
            if (!card.covered && protocol.cards.Count > 0)
                protocol.cards[protocol.cards.Count - 1].covered = false;
        }

        List<String> locations = new List<String>();
        foreach (Card c in uncoveredCards)
        {
            var location = Game.instance.GetCardLocation(c);
            locations.Add(location.local + "," + location.protocolIndex + "," + location.cardIndex);
        }

        RpcId(oppId, nameof(OppMultiDelete), Json.Stringify(new Godot.Collections.Array<String>(locations)));
        await WaitForOppResponse();

        foreach (Card c in Game.instance.GetCards())
        {
            if (Game.instance.IsLocal(c) && !c.flipped) await c.info.OnDelete(c);
        }

        foreach (Card card in uncoveredCards)
        {
            if (!card.covered)
                await Uncover(card, Game.instance.GetProtocolOfCard(card));
        }
    }

    public async Task MultiShift(List<Card> cards, Protocol protocol)
    {
        List<Card> localCards = cards.FindAll(Game.instance.IsLocal).ToList();
        List<String> locations = new List<String>();
        foreach (Card c in cards)
        {
            var location = Game.instance.GetCardLocation(c);
            locations.Add(location.local + "," + location.protocolIndex + "," + location.cardIndex);
        }

        List<Card> uncoveredCards = new List<Card>();
        foreach (Card card in cards)
        {
            var cardLocation = Game.instance.GetCardLocation(card);
            Protocol sourceProtocol = Game.instance.GetProtocols(cardLocation.local)[cardLocation.protocolIndex];
            sourceProtocol.cards.Remove(card);
            card.GetParent().RemoveChild(card);
            sourceProtocol.OrderCards();
            if (card.covered)
            {
                card.covered = false;
            }
            else if (sourceProtocol.cards.Count > 0)
            {
                sourceProtocol.cards[sourceProtocol.cards.Count - 1].covered = false;
                uncoveredCards.Add(sourceProtocol.cards[sourceProtocol.cards.Count - 1]);
            }
        }

        RpcId(oppId, nameof(OppMultiShift), Json.Stringify(new Godot.Collections.Array<String>(locations)),
            Game.instance.IndexOfProtocol(protocol));
        await WaitForOppResponse();

        await Cover(protocol.cards[protocol.cards.Count - 1], protocol);

        List<Card> uncoveredShiftedCards = new List<Card>();
        foreach (Card card in cards)
        {
            Protocol currProtocol = localCards.Contains(card) ? protocol : Game.instance.GetOpposingProtocol(protocol);
            currProtocol.AddCard(card);
            if (currProtocol.cards.Count > 1)
            {
                currProtocol.cards[currProtocol.cards.Count - 2].covered = true;
            }
            if (currProtocol.cards.Count > 1 && !currProtocol.cards[currProtocol.cards.Count - 2].flipped &&
                !cards.Contains(currProtocol.cards[currProtocol.cards.Count - 2]))
            {
                uncoveredShiftedCards.Add(currProtocol.cards[currProtocol.cards.Count - 2]);
            }
        }

        RpcId(oppId, nameof(OppResponse));
        await WaitForOppResponse();

        foreach (Card card in uncoveredCards)
        {
            await Uncover(card, Game.instance.GetProtocolOfCard(card));
        }
        foreach (Card card in uncoveredShiftedCards)
        {
            if (!card.covered)
                await Uncover(card, Game.instance.GetProtocolOfCard(card));
        }
    }

    public async Task SendCommand(Command command, String text = "")
    {
        List<String> handCards = new List<String>();
        List<String> oppHandCards = new List<String>();
        List<String> locations = new List<String>();
        foreach (Card c in command.cards)
        {
            if (hand.Contains(c))
            {
                handCards.Add(c.info.GetCardName());
            }
            else if (oppHand.Contains(c))
            {
                oppHandCards.Add(c.info.GetCardName());
            }
            else
            {
                var location = Game.instance.GetCardLocation(c);
                locations.Add(location.local + "," + location.protocolIndex + "," + location.cardIndex);
            }
        }

        List<String> protocolLocations = new List<String>();
        foreach (Protocol p in command.protocols)
        {
            protocolLocations.Add(Game.instance.IsLocal(p) + "," + Game.instance.IndexOfProtocol(p));
        }

        if (command.type == CommandType.Give)
        {
            foreach (Card card in command.cards)
            {
                hand.Remove(card);
                card.GetParent().RemoveChild(card);
                oppHand.Add(card);
                Game.instance.oppCardsContainer.AddChild(card);
            }
        }

        if (command.type == CommandType.Steal)
        {
            foreach (Card card in command.cards)
            {
                oppHand.Remove(card);
                card.GetParent().RemoveChild(card);
                hand.Add(card);
                Game.instance.handCardsContainer.AddChild(card);
            }
        }

        RpcId(oppId, nameof(OppHandleCommand), (int)command.type, command.num, 
            Json.Stringify(new Godot.Collections.Array<String>(protocolLocations)),
            Json.Stringify(new Godot.Collections.Array<String>(locations)),
            Json.Stringify(new Godot.Collections.Array<String>(handCards)),
            Json.Stringify(new Godot.Collections.Array<String>(oppHandCards)), text);
        await WaitForOppResponse();
    }

    public async Task Cover(Card card, Protocol protocol)
    {
        if (!card.flipped) await card.info.OnCover(card);
        card.covered = true;
        foreach (Passive passive in card.info.bottomPassives) passives[passive] = null;
    }

    public async Task Uncover(Card card, Protocol protocol)
    {
        if (!card.flipped)
        {
            foreach (Passive passive in card.info.bottomPassives)
                passives[passive] = new PassiveLocation(Game.instance.Line(protocol), Game.instance.IsLocal(card));
            if (Game.instance.IsLocal(card) && !LineContainsPassive(protocol, CardInfo.Passive.NoMiddleCommands))
                await card.info.OnPlay(card);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppCompile(int protocolIndex)
    {
        Protocol protocol = Game.instance.GetProtocols(false)[protocolIndex];
        protocol.compiled = true;
        protocol.Render();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppPlay(String cardName, int protocolIndex, bool flipped, bool top)
    {
        Card card = null;
        if (top)
        {
            card = oppDeck[0];
            oppDeck.Remove(card);
        }
        else
        {
            card = oppHand.Find(handCard => handCard.info.GetCardName() == cardName);
            oppHand.Remove(card);
        }
        card.flipped = flipped;
        List<Protocol> protocols = Game.instance.GetProtocols(false);
        if (protocols[protocolIndex].cards.Count > 0)
        {
            protocols[protocolIndex].cards[protocols[protocolIndex].cards.Count - 1].covered = true;
        }
        foreach (CardInfo.Passive passive in card.info.passives)
        {
            if (!flipped)
                passives[passive] = new PassiveLocation(Game.instance.Line(protocols[protocolIndex]), false);
        }
        protocols[protocolIndex].AddCard(card);
        card.Render();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppPlayTopUnderneath(int protocolIndex)
    {
        Protocol protocol = Game.instance.GetProtocols(false)[protocolIndex];
        Card card = oppDeck[0];
        oppDeck.Remove(card);
        card.flipped = true;
        protocol.InsertCard(protocol.cards.Count - 1, card);
        card.Render();
        protocol.OrderCards();
        RpcId(oppId, nameof(OppResponse));
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
        //card.flipped = true; UNCOMMENT THIS
        card.Render();
        Game.instance.oppCardsContainer.AddChild(card);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void OppDiscardTriggers()
    {
        foreach (Card c in Game.instance.GetCards())
        {
            if (Game.instance.IsLocal(c) && !c.flipped) await c.info.OnDiscard(c);
        }
        RpcId(oppId, nameof(OppResponse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppDrawFromOpp()
    {
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
            RpcId(oppId, nameof(OppSetDeck), deck.Select((Card c) => c.info.GetCardName()).ToArray());
        }
        if (deck.Count == 1) // Deck has no more cards
        {
            Game.instance.localDeckTop.placeholder = true;
            Game.instance.localDeckTop.Render();
        }
        Card card = deck[0];
        deck.Remove(card);
        oppHand.Add(card);
        Game.instance.oppCardsContainer.AddChild(card);
        // card.flipped = true; UNCOMMENT THIS
        card.Render();
        RpcId(oppId, nameof(OppResponse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppGainControl()
    {
        hasControl = false;
        Game.instance.control.OffsetTop = Constants.CONTROL_OPP_TOP;
        Game.instance.control.OffsetBottom = Constants.CONTROL_OPP_BOTTOM;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppUseControl(String protocolAJson, String protocolBJson)
    {
        Game.instance.control.OffsetTop = Constants.CONTROL_TOP;
        Game.instance.control.OffsetBottom = Constants.CONTROL_BOTTOM;

        List<String> protocolALocations = new Godot.Collections.Array<String>(Json.ParseString(protocolAJson).AsGodotArray()).ToList();
        List<String> protocolBLocations = new Godot.Collections.Array<String>(Json.ParseString(protocolBJson).AsGodotArray()).ToList();
        for (int i = 0; i < protocolALocations.Count; i++)
        {
            String[] splitA = protocolALocations[i].Split(',');
            Protocol protocolA = Game.instance.GetProtocols(!Boolean.Parse(splitA[0]))[Int32.Parse(splitA[1])];
            String[] splitB = protocolBLocations[i].Split(',');
            Protocol protocolB = Game.instance.GetProtocols(!Boolean.Parse(splitB[0]))[Int32.Parse(splitB[1])];
            MousePosition.Swap(protocolA, protocolB);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppRearrange(String protocolAJson, String protocolBJson)
    {
        List<String> protocolALocations = new Godot.Collections.Array<String>(Json.ParseString(protocolAJson).AsGodotArray()).ToList();
        List<String> protocolBLocations = new Godot.Collections.Array<String>(Json.ParseString(protocolBJson).AsGodotArray()).ToList();
        for (int i = 0; i < protocolALocations.Count; i++)
        {
            String[] splitA = protocolALocations[i].Split(',');
            Protocol protocolA = Game.instance.GetProtocols(!Boolean.Parse(splitA[0]))[Int32.Parse(splitA[1])];
            String[] splitB = protocolBLocations[i].Split(',');
            Protocol protocolB = Game.instance.GetProtocols(!Boolean.Parse(splitB[0]))[Int32.Parse(splitB[1])];
            MousePosition.Swap(protocolA, protocolB);
        }

        RpcId(oppId, nameof(OppResponse));
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
        if (Game.instance.IsLocal(card))
        {
            await card.info.OnFlip(card);
            if (Game.instance.GetProtocolOfCard(card) == null) return; // May leave field
        }
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
            if (!card.covered)
                await Uncover(card, Game.instance.GetProtocols(local)[protocolIndex]);
        }
        RpcId(oppId, nameof(OppResponse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void OppDelete(bool local, int protocolIndex, int cardIndex)
    {
        Card card = Game.instance.FindCard(!local, protocolIndex, cardIndex);
        bool wasCovered = card.covered;
        Protocol protocol = Game.instance.GetProtocols(!local)[protocolIndex];
        card.GetParent().RemoveChild(card);
        protocol.cards.Remove(card);
        card.Reset();
        foreach (CardInfo.Passive passive in card.info.passives)
        {
            passives[passive] = null;
        }
        if (!local)
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
        if (!wasCovered && protocol.cards.Count > 0)
        {
            protocol.cards[protocol.cards.Count - 1].covered = false;
        }
        protocol.OrderCards();
        if (!wasCovered && protocol.cards.Count > 0)
        {
            await Uncover(protocol.cards[protocol.cards.Count - 1], protocol);
        }

        RpcId(oppId, nameof(OppResponse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void OppReturn(bool local, int protocolIndex, int cardIndex)
    {
        Card card = Game.instance.FindCard(!local, protocolIndex, cardIndex);
        bool wasCovered = card.covered;
        Protocol protocol = Game.instance.GetProtocols(!local)[protocolIndex];
        card.GetParent().RemoveChild(card);
        protocol.cards.Remove(card);
        card.Reset();
        foreach (CardInfo.Passive passive in card.info.passives)
        {
            passives[passive] = null;
        }
        if (!local)
        {
            hand.Add(card);
            Game.instance.handCardsContainer.AddChild(card);
        }
        else
        {
            oppHand.Add(card);
            card.flipped = true;
            card.Render();
            Game.instance.oppCardsContainer.AddChild(card);
        }
        if (!wasCovered && protocol.cards.Count > 0)
        {
            protocol.cards[protocol.cards.Count - 1].covered = false;
        }
        protocol.OrderCards();
        if (!wasCovered && protocol.cards.Count > 0)
        {
            await Uncover(protocol.cards[protocol.cards.Count - 1], protocol);
        }

        RpcId(oppId, nameof(OppResponse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void OppShift(bool local, int protocolIndex, int cardIndex, int sourceProtocolIndex)
    {
        Protocol protocol = Game.instance.GetProtocols(!local)[protocolIndex];
        Protocol sourceProtocol = Game.instance.GetProtocols(!local)[sourceProtocolIndex];
        Card card = sourceProtocol.cards[cardIndex];
        sourceProtocol.cards.Remove(card);
        protocol.AddCard(card);
        if (protocol.cards.Count > 1)
        {
            protocol.cards[protocol.cards.Count - 2].covered = true;
        }
        sourceProtocol.OrderCards();
        bool wasCovered = card.covered;
        if (wasCovered)
        {
            card.covered = false;
        }
        else if (sourceProtocol.cards.Count > 0)
        {
            sourceProtocol.cards[sourceProtocol.cards.Count - 1].covered = false;
        }

        if (protocol.cards.Count > 1)
        {
            await Cover(protocol.cards[protocol.cards.Count - 2], protocol);
        }
        if (wasCovered)
        {
            await Uncover(card, protocol);
        }
        else if (sourceProtocol.cards.Count > 0)
        {
            await Uncover(sourceProtocol.cards[sourceProtocol.cards.Count - 1], sourceProtocol);
        }

        RpcId(oppId, nameof(OppResponse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppApplyTempEffect(int effect, int time)
    {
        tempEffects[(CardInfo.TempEffect)effect] = time + 1;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppSendToDiscard(String cardName)
    {
        Card card = oppHand.Find((Card c) => c.info.GetCardName() == cardName);
        oppHand.Remove(card);
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void OppMultiDelete(string json)
    {
        List<String> uncoveredCardLocations = new Godot.Collections.Array<String>(Json.ParseString(json).AsGodotArray()).ToList();
        List<Card> uncoveredCards = new List<Card>();
        foreach (String location in uncoveredCardLocations)
        {
            String[] split = location.Split(',');
            Card card = Game.instance.FindCard(
                !Boolean.Parse(split[0]), Int32.Parse(split[1]), Int32.Parse(split[2]));
            uncoveredCards.Add(card);
        }

        foreach (Card card in uncoveredCards)
        {
            Protocol protocol = Game.instance.GetProtocolOfCard(card);
            if (protocol.cards.IndexOf(card) == protocol.cards.Count - 1)
            {
                card.covered = false;
                await Uncover(card, Game.instance.GetProtocolOfCard(card));
            }
        }

        RpcId(oppId, nameof(OppResponse));
    }

    // Remove from protocol and trigger OnCover
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void OppMultiShift(string json, int protocolIndex)
    {
        List<String> locations = new Godot.Collections.Array<String>(Json.ParseString(json).AsGodotArray()).ToList();
        List<Card> cards = new List<Card>();
        foreach (String location in locations)
        {
            String[] split = location.Split(',');
            Card card = Game.instance.FindCard(
                !Boolean.Parse(split[0]), Int32.Parse(split[1]), Int32.Parse(split[2]));
            cards.Add(card);
        }
        List<Card> localCards = cards.FindAll(Game.instance.IsLocal).ToList();

        List<Card> uncoveredCards = new List<Card>();
        foreach (Card card in cards)
        {
            var cardLocation = Game.instance.GetCardLocation(card);
            Protocol sourceProtocol = Game.instance.GetProtocols(cardLocation.local)[cardLocation.protocolIndex];
            sourceProtocol.cards.Remove(card);
            card.GetParent().RemoveChild(card);
            sourceProtocol.OrderCards();
            if (card.covered)
            {
                card.covered = false;
            }
            else if (sourceProtocol.cards.Count > 0)
            {
                sourceProtocol.cards[sourceProtocol.cards.Count - 1].covered = false;
            }
            if (sourceProtocol.cards.Count > 0)
            {
                uncoveredCards.Add(sourceProtocol.cards[sourceProtocol.cards.Count - 1]);
            }
        }

        Protocol oppProtocol = Game.instance.GetProtocols(false)[protocolIndex];
        Protocol localProtocol = Game.instance.GetOpposingProtocol(oppProtocol);
        if (localProtocol.cards.Count > 0 && localCards.Count > 0)
            await Cover(localProtocol.cards[localProtocol.cards.Count - 1], localProtocol);

        RpcId(oppId, nameof(OppResponse));
        await WaitForOppResponse();

        List<Card> uncoveredShiftedCards = new List<Card>();
        foreach (Card card in cards)
        {
            Protocol protocol = localCards.Contains(card) ? localProtocol : oppProtocol;
            protocol.AddCard(card);
            if (protocol.cards.Count > 1)
            {
                protocol.cards[protocol.cards.Count - 2].covered = true;
            }
            if (protocol.cards.Count > 1 && !protocol.cards[protocol.cards.Count - 2].flipped &&
                !cards.Contains(protocol.cards[protocol.cards.Count - 2]))
            {
                uncoveredShiftedCards.Add(protocol.cards[protocol.cards.Count - 2]);
            }
        }

        foreach (Card card in uncoveredCards)
        {
            await Uncover(card, Game.instance.GetProtocolOfCard(card));
        }
        foreach (Card card in uncoveredShiftedCards)
        {
            if (!card.covered)
                await Uncover(card, Game.instance.GetProtocolOfCard(card));
        }

        RpcId(oppId, nameof(OppResponse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void OppHandleCommand(int typeInt, int num, String protocolJson, String cardJson, 
        String handCardJson, String oppHandCardJson, String text)
    {
        CommandType type = (CommandType)typeInt;
        List<String> cardLocations = new Godot.Collections.Array<String>(Json.ParseString(cardJson).AsGodotArray()).ToList();
        List<Card> cards = new List<Card>();
        foreach (String location in cardLocations)
        {
            String[] split = location.Split(',');
            Card card = Game.instance.FindCard(
                !Boolean.Parse(split[0]), Int32.Parse(split[1]), Int32.Parse(split[2]));
            cards.Add(card);
        }

        List<String> handCardLocations = new Godot.Collections.Array<String>(Json.ParseString(handCardJson).AsGodotArray()).ToList();
        List<Card> handCards = new List<Card>();
        foreach (String location in handCardLocations)
        {
            Card card = oppHand.Find(c => c.info.GetCardName() == location);
            handCards.Add(card);
        }

        List<String> oppHandCardLocations = 
            new Godot.Collections.Array<String>(Json.ParseString(oppHandCardJson).AsGodotArray()).ToList();
        List<Card> oppHandCards = new List<Card>();
        foreach (String location in oppHandCardLocations)
        {
            Card card = hand.Find(c => c.info.GetCardName() == location);
            oppHandCards.Add(card);
        }

        List<String> protocolLocations = new Godot.Collections.Array<String>(Json.ParseString(protocolJson).AsGodotArray()).ToList();
        List<Protocol> protocols = new List<Protocol>();
        foreach (String location in protocolLocations)
        {
            String[] split = location.Split(',');
            Protocol protocol = Game.instance.GetProtocols(!Boolean.Parse(split[0]))[Int32.Parse(split[1])];
            protocols.Add(protocol);
        }

        if (type == CommandType.PlayTop)
        {
            foreach (Protocol protocol in protocols)
            {
                await PlayTop(protocol);
            }
        }

        if (type == CommandType.Draw)
        {
            await Draw(num);
        }

        if (type == CommandType.Discard)
        {
            await Discard(num);
        }

        if (type == CommandType.Delete)
        {
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = text;
            PromptManager.PromptAction([PromptManager.Prompt.Select], cards);
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            await Delete(response.card);
        }

        if (type == CommandType.Reveal)
        {
            Reveal(handCards);
        }

        if (type == CommandType.Give)
        {
            foreach (Card c in handCards)
            {
                oppHand.Remove(c);
                c.GetParent().RemoveChild(c);
                hand.Add(c);
                Game.instance.handCardsContainer.AddChild(c);
            }
        }

        if (type == CommandType.Steal)
        {
            foreach (Card c in oppHandCards)
            {
                hand.Remove(c);
                c.GetParent().RemoveChild(c);
                oppHand.Add(c);
                Game.instance.oppCardsContainer.AddChild(c);
            }
        }
        RpcId(oppId, nameof(OppResponse));
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