using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

public partial class Game : Control
{
    public static Game instance;
    public Player localPlayer;
    public Draft.DraftType draftType = Draft.DraftType.BanDraft;
    public Draft draft;
    public HBoxContainer handCardsContainer;
    public HBoxContainer oppCardsContainer;
    public HBoxContainer localProtocolsContainer;
    public HBoxContainer oppProtocolsContainer;
    public Label deckLabel;
    public Label oppDeckLabel;
    public Card localDeckTop;
    public Card localDiscardTop;
    public Card oppDeckTop;
    public Card oppDiscardTop;
    public Panel control;
    public Card focusedCard;
    public VBoxContainer leftUI;
    public Label promptLabel;
    public Button endActionButton;
    public Button refreshButton;
    public Button resetControlButton;
    public Button customButtonA;
    public Button customButtonB;
    public HBoxContainer flippedCheckbox;
    public PanelContainer revealPanel;
    public PanelContainer victoryPanel;
    public PanelContainer losePanel;
    public PanelContainer disconnectPanel;
    public MousePosition mousePosition;
    bool host;
    bool first;
    bool oppResponse;
    int oppId;

    public async Task Init(int player1Id, int player2Id, bool isHost)
    {
        GD.Randomize();
        instance = this;
        Multiplayer.PeerDisconnected += PeerDisconnected;
        host = isHost;
        oppId = Multiplayer.GetUniqueId() == player1Id ? player2Id : player1Id;
        draft = GetNode<Draft>("DraftPanel");
        handCardsContainer = GetNode<HBoxContainer>("LocalHandCardsContainer");
        oppCardsContainer = GetNode<HBoxContainer>("OppHandCardsContainer");
        localProtocolsContainer = GetNode<HBoxContainer>("LocalProtocolsContainer");
        oppProtocolsContainer = GetNode<HBoxContainer>("OppProtocolsContainer");
        deckLabel = GetNode<Label>("DeckLabel");
        oppDeckLabel = GetNode<Label>("OppDeckLabel");
        control = GetNode<Panel>("Control");
        focusedCard = GetNode<Card>("FocusedCard");
        focusedCard.Resize(1.2);
        leftUI = GetNode<VBoxContainer>("LeftUI");
        promptLabel = leftUI.GetNode<Label>("PromptLabel");
        endActionButton = leftUI.GetNode<Button>("EndActionButton");
        refreshButton = leftUI.GetNode<Button>("RefreshButton");
        resetControlButton = leftUI.GetNode<Button>("ResetControlButton");
        customButtonA = leftUI.GetNode<Button>("CustomButtonA");
        customButtonB = leftUI.GetNode<Button>("CustomButtonB");
        flippedCheckbox = leftUI.GetNode<HBoxContainer>("FlippedCheckbox");
        revealPanel = GetNode<PanelContainer>("RevealPanel");
        victoryPanel = GetNode<PanelContainer>("VictoryPanel");
        losePanel = GetNode<PanelContainer>("LosePanel");
        disconnectPanel = GetNode<PanelContainer>("DisconnectPanel");
        mousePosition = GetNode<MousePosition>("MousePosition");

        RpcId(oppId, nameof(OppResponse));
        await WaitForOppResponse();

        if (host)
        {
            first = Utility.random.Randi() % 2 == 0;
            RpcId(oppId, nameof(SetFirst), !first);
            await WaitForOppResponse();
            RpcId(oppId, nameof(OppResponse));
        } else
        {
            await WaitForOppResponse();
        }

        if (first) await draft.Init(draftType, oppId);
        await draft.WaitForDraft();
        draft.Visible = false;

        foreach (String name in draft.localProtocols)
        {
            PackedScene protocolScene = GD.Load("res://Game/Protocol.tscn") as PackedScene;
            Protocol protocol = protocolScene.Instantiate<Protocol>();
            protocol.info = Cardlist.protocols[name];
            localProtocolsContainer.AddChild(protocol);
        }
        foreach (String name in draft.oppProtocols)
        {
            PackedScene protocolScene = GD.Load("res://Game/Protocol.tscn") as PackedScene;
            Protocol protocol = protocolScene.Instantiate<Protocol>();
            protocol.info = Cardlist.protocols[name];
            protocol.Rotation = (float)Math.PI;
            oppProtocolsContainer.AddChild(protocol);
        }

        // Initialize deck/discard:
        localDeckTop = GD.Load<PackedScene>("res://Game/Card.tscn").Instantiate<Card>();
        localDeckTop.flipped = true;
        localDeckTop.SetCardInfo(new CardInfo("Apathy", 5));
        handCardsContainer.AddChild(localDeckTop);
        localDiscardTop = GD.Load<PackedScene>("res://Game/Card.tscn").Instantiate<Card>();
        localDiscardTop.placeholder = true;
        localDiscardTop.SetCardInfo(new CardInfo("Apathy", 5));
        handCardsContainer.AddChild(localDiscardTop);
        Control localSeperator = new Control();
        handCardsContainer.AddChild(localSeperator);

        oppDeckTop = GD.Load<PackedScene>("res://Game/Card.tscn").Instantiate<Card>();
        oppDeckTop.flipped = true;
        oppDeckTop.SetCardInfo(new CardInfo("Apathy", 5));
        oppCardsContainer.AddChild(oppDeckTop);
        oppDiscardTop = GD.Load<PackedScene>("res://Game/Card.tscn").Instantiate<Card>();
        oppDiscardTop.placeholder = true;
        oppDiscardTop.SetCardInfo(new CardInfo("Apathy", 5));
        oppCardsContainer.AddChild(oppDiscardTop);
        Control oppSeperator = new Control();
        oppCardsContainer.AddChild(oppSeperator);

        PromptManager.Init();

        RpcId(oppId, nameof(OppResponse));
        await WaitForOppResponse();

        localPlayer = new Player(Multiplayer.GetUniqueId(), oppId);
        localPlayer.Name = "Player";

        AddChild(localPlayer);

        RpcId(oppId, nameof(OppResponse));
        await WaitForOppResponse();

        await localPlayer.Init();

        RpcId(oppId, nameof(OppResponse));
        await WaitForOppResponse();

        if (first)
            localPlayer.StartTurn();
        else
            await localPlayer.EndTurn();
    }

    public int SumStack(Protocol p)
    {
        int total = 0;
        foreach (Card card in p.cards)
        {
            total += card.GetValue();
            if (localPlayer.LineContainsPassive(p, CardInfo.Passive.PlusOneForFaceDown))
            {
                if (card.flipped) total++;
            }
        }
        foreach (Card card in GetOpposingProtocol(p).cards)
        {
            if (localPlayer.LineContainsPassive(p, CardInfo.Passive.PlusOneForFaceDown))
            {
                if (card.flipped) total++;
            }
        }
        if (localPlayer.StackContainsPassive(!IsLocal(p), p, CardInfo.Passive.ReduceOppValueByTwo)) total -= 2;
        return total;
    }

    public List<Protocol> GetProtocols()
    {
        return GetProtocols(true).Concat(GetProtocols(false)).ToList();
    }

    public List<Protocol> GetProtocols(bool local)
    {
        List<Protocol> protocols = new List<Protocol>();
        if (local)
        {
            foreach (Protocol p in localProtocolsContainer.GetChildren()) protocols.Add(p);
        } else
        {
            foreach (Protocol p in oppProtocolsContainer.GetChildren()) protocols.Add(p);
        }
        return protocols;
    }

    public int IndexOfProtocol(Protocol p)
    {
        return GetProtocols(IsLocal(p)).FindIndex((Protocol protocol)  => protocol == p);
    }

    public List<Card> GetCards()
    {
        List<Card> cards = new List<Card>();
        foreach (Protocol p in GetProtocols(true))
        {
            cards.AddRange(p.cards);
        }
        foreach (Protocol p in GetProtocols(false))
        {
            cards.AddRange(p.cards);
        }
        return cards;
    }

    public Protocol GetOpposingProtocol(Protocol p) 
    {
        List<Protocol> locals = GetProtocols(true);
        List<Protocol> opps = GetProtocols(false);

        if (locals.Contains(p)) return opps[opps.Count - 1 - locals.FindIndex((Protocol _p) => _p == p)];
        else return locals[locals.Count - 1 - opps.FindIndex((Protocol _p) => _p == p)];
    }

    public Protocol GetHoveredProtocol()
    {
        Vector2 mousePos = GetGlobalMousePosition();
        foreach (Protocol p in localProtocolsContainer.GetChildren())
        {
            if (Geometry2D.IsPointInPolygon(mousePos,
                    [new Vector2(p.GlobalPosition.X, p.GlobalPosition.Y),
                    new Vector2(p.GlobalPosition.X, p.GlobalPosition.Y + (IsLocal(p) ? 1 : -1) * Constants.PROTOCOL_HEIGHT),
                    new Vector2(p.GlobalPosition.X + (IsLocal(p) ? 1 : -1) * Constants.PROTOCOL_WIDTH,
                    p.GlobalPosition.Y + (IsLocal(p) ? 1 : -1) * Constants.PROTOCOL_HEIGHT),
                    new Vector2(p.GlobalPosition.X + (IsLocal(p) ? 1 : -1) * Constants.PROTOCOL_WIDTH, p.GlobalPosition.Y)]))
            {
                return p;
            }
        }
        foreach (Protocol p in oppProtocolsContainer.GetChildren())
        {
            if (Geometry2D.IsPointInPolygon(mousePos,
                    [new Vector2(p.GlobalPosition.X, p.GlobalPosition.Y),
                    new Vector2(p.GlobalPosition.X, p.GlobalPosition.Y + (IsLocal(p) ? 1 : -1) * Constants.PROTOCOL_HEIGHT),
                    new Vector2(p.GlobalPosition.X + (IsLocal(p) ? 1 : -1) * Constants.PROTOCOL_WIDTH,
                    p.GlobalPosition.Y + (IsLocal(p) ? 1 : -1) * Constants.PROTOCOL_HEIGHT),
                    new Vector2(p.GlobalPosition.X + (IsLocal(p) ? 1 : -1) * Constants.PROTOCOL_WIDTH, p.GlobalPosition.Y)]))
            {
                return p;
            }
        }
        return null;
    }

    public bool IsLocal(Protocol p)
    {
        return localProtocolsContainer.GetChildren().Contains(p);
    }

    public bool IsLocal(Card c)
    {
        if (localPlayer.hand.Contains(c)) return true;
        foreach (Protocol p in GetProtocols(true))
        {
            if (p.cards.Contains(c)) return true;
        }
        return false;
    }

    public Protocol GetProtocolOfCard(Card card)
    {
        foreach (Protocol p in GetProtocols(true))
        {
            if (p.cards.Contains(card)) return p;
        }
        foreach (Protocol p in GetProtocols(false))
        {
            if (p.cards.Contains(card)) return p;
        }
        return null;
    }

    public (bool local, int protocolIndex, int cardIndex) GetCardLocation(Card c)
    {
        for (int i = 0; i < GetProtocols(true).Count; i++)
        {
            Protocol p = GetProtocols(true)[i];
            if (p.cards.Contains(c)) return (true, i, p.cards.IndexOf(c));
        }
        for (int i = 0; i < GetProtocols(false).Count; i++)
        {
            Protocol p = GetProtocols(false)[i];
            if (p.cards.Contains(c)) return (false, i, p.cards.IndexOf(c));
        }
        throw new Exception("Card not found");
    }

    public Card FindCard(bool local, int protocolIndex, int cardIndex)
    {
        List<Protocol> protocols = GetProtocols(local);
        return protocols[protocolIndex].cards[cardIndex];
    }

    public int Line(Protocol protocol)
    {
        if (IsLocal(protocol)) return IndexOfProtocol(protocol);
        else return GetProtocols(false).Count - 1 - IndexOfProtocol(protocol);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetFirst(bool _first)
    {
        first = _first;
        RpcId(oppId, nameof(OppResponse));
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

    void PeerDisconnected(long id)
    {
        disconnectPanel.Visible = true;
    }
}
