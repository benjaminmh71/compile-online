using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class Game : Control
{
    public static Game instance;
    public Player localPlayer;
    public HBoxContainer handCardsContainer;
    public HBoxContainer oppCardsContainer;
    public HBoxContainer localProtocolsContainer;
    public HBoxContainer oppProtocolsContainer;
    public Label turnLabel;
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
        turnLabel = GetNode("LeftUI").GetNode<Label>("TurnLabel");
        mousePosition = GetNode<MousePosition>("MousePosition");
        for (int i = 0; i < 3; i++)
        {
            PackedScene protocolScene = GD.Load("res://Game/Protocol.tscn") as PackedScene;
            Protocol protocol = protocolScene.Instantiate<Protocol>();
            localProtocolsContainer.AddChild(protocol);
        }
        for (int i = 0; i < 3; i++)
        {
            PackedScene protocolScene = GD.Load("res://Game/Protocol.tscn") as PackedScene;
            Protocol protocol = protocolScene.Instantiate<Protocol>();
            protocol.Rotation = (float)Math.PI;
            oppProtocolsContainer.AddChild(protocol);
        }

        if (Multiplayer.GetUniqueId() == player1Id)
        {
            localPlayer = new Player(player1Id, player2Id);
        } else
        {
            localPlayer = new Player(player2Id, player1Id);
        }

        AddChild(localPlayer);
    }

    public void Start()
    {
        localPlayer.Draw(5);
        if (host)
        {
            if (GD.Randi() % 2 == 0)
            {
                localPlayer.StartTurn();
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
}
