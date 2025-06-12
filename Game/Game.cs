using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

public partial class Game : Control
{
    public static Game instance;
    public Player localPlayer;
    public HBoxContainer handCardsContainer;
    public HBoxContainer oppCardsContainer;
    public HBoxContainer localProtocolsContainer;
    public HBoxContainer oppProtocolsContainer;
    public Card localDeckTop;
    public Card localDiscardTop;
    public Card oppDeckTop;
    public Card oppDiscardTop;
    public Panel control;
    public VBoxContainer leftUI;
    public Label promptLabel;
    public Button endActionButton;
    public Button refreshButton;
    public Button resetControlButton;
    public PanelContainer victoryPanel;
    public PanelContainer losePanel;
    public MousePosition mousePosition;
    bool host;

    public void Init(int player1Id, int player2Id, bool isHost)
    {
        GD.Randomize();
        instance = this;
        host = isHost;
        handCardsContainer = GetNode<HBoxContainer>("LocalHandCardsContainer");
        oppCardsContainer = GetNode<HBoxContainer>("OppHandCardsContainer");
        localProtocolsContainer = GetNode<HBoxContainer>("LocalProtocolsContainer");
        oppProtocolsContainer = GetNode<HBoxContainer>("OppProtocolsContainer");
        control = GetNode<Panel>("Control");
        leftUI = GetNode<VBoxContainer>("LeftUI");
        promptLabel = leftUI.GetNode<Label>("PromptLabel");
        endActionButton = leftUI.GetNode<Button>("EndActionButton");
        refreshButton = leftUI.GetNode<Button>("RefreshButton");
        resetControlButton = leftUI.GetNode<Button>("ResetControlButton");
        victoryPanel = GetNode<PanelContainer>("VictoryPanel");
        losePanel = GetNode<PanelContainer>("LosePanel");
        mousePosition = GetNode<MousePosition>("MousePosition");

        String[] protocolNames = new String[3]{ "Apathy", "Apathy", "Apathy" };
        foreach (String name in protocolNames)
        {
            PackedScene protocolScene = GD.Load("res://Game/Protocol.tscn") as PackedScene;
            Protocol protocol = protocolScene.Instantiate<Protocol>();
            protocol.info = Cardlist.protocols[name];
            localProtocolsContainer.AddChild(protocol);
        }
        foreach (String name in protocolNames)
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

        if (Multiplayer.GetUniqueId() == player1Id)
        {
            localPlayer = new Player(player1Id, player2Id);
        } else
        {
            localPlayer = new Player(player2Id, player1Id);
        }
        localPlayer.Name = "Player";

        AddChild(localPlayer);
    }

    public void Start()
    {
        localPlayer.Init();
        if (host)
        {
            if (GD.Randi() % 2 == 0)
            {
#pragma warning disable CS4014 // Async not awaited warning
                localPlayer.StartTurn();
#pragma warning restore CS4014
            } else
            {
                localPlayer.EndTurn();
            }
        }
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

    public Protocol GetOpposingProtocol(Protocol p) 
    {
        List<Protocol> locals = GetProtocols(true);
        List<Protocol> opps = GetProtocols(false);

        if (locals.Contains(p)) return opps[opps.Count - 1 - locals.FindIndex((Protocol _p) => _p == p)];
        else return locals[locals.Count - 1 - opps.FindIndex((Protocol _p) => _p == p)];
    }

    public Protocol GetHoveredProtocol()
    {
        foreach (Protocol p in localProtocolsContainer.GetChildren())
        {
            if (GetGlobalMousePosition().X > p.GlobalPosition.X &&
                GetGlobalMousePosition().X < p.GlobalPosition.X + Constants.PROTOCOL_WIDTH &&
                GetGlobalMousePosition().Y > p.GlobalPosition.Y &&
                GetGlobalMousePosition().Y < p.GlobalPosition.Y + Constants.PROTOCOL_HEIGHT)
            {
                return p;
            }
        }
        foreach (Protocol p in oppProtocolsContainer.GetChildren())
        {
            if (GetGlobalMousePosition().X > p.GlobalPosition.X &&
                GetGlobalMousePosition().X < p.GlobalPosition.X + Constants.PROTOCOL_WIDTH &&
                GetGlobalMousePosition().Y > p.GlobalPosition.Y &&
                GetGlobalMousePosition().Y < p.GlobalPosition.Y + Constants.PROTOCOL_HEIGHT)
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
        List<Protocol> protocols = GetProtocols(!local);
        return protocols[protocolIndex].cards[cardIndex];
    }

    public int Line(Protocol protocol)
    {
        if (IsLocal(protocol)) return GetProtocols(true).FindIndex((Protocol p) => p == protocol);
        else return GetProtocols(false).Count - 1 - GetProtocols(false).FindIndex((Protocol p) => p == protocol);
    }
}
