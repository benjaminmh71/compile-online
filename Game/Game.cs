using CompileOnline.Game;
using Godot;
using System;

public partial class Game : Control
{
    public static Game instance;
    public Player player1;
    public Player player2;
    public Player localPlayer;
    public HBoxContainer handCardsContainer;
    public HBoxContainer oppCardsContainer;
    public HBoxContainer localProtocolsContainer;
    public HBoxContainer oppProtocolsContainer;
    public Control mousePosition;

    public void Init(int player1Id, int player2Id)
    {
        instance = this;
        handCardsContainer = GetNode<HBoxContainer>("LocalHandCardsContainer");
        oppCardsContainer = GetNode<HBoxContainer>("OppHandCardsContainer");
        localProtocolsContainer = GetNode<HBoxContainer>("LocalProtocolsContainer");
        oppProtocolsContainer = GetNode<HBoxContainer>("OppProtocolsContainer");
        mousePosition = GetNode<Control>("MousePosition");
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
            oppProtocolsContainer.AddChild(protocol);
        }

        player1 = new Player(player1Id, player2Id);
        player2 = new Player(player2Id, player1Id);
        AddChild(player1);
        AddChild(player2);
        if (Multiplayer.GetUniqueId() == player1Id)
        {
            localPlayer = player1;
        } else
        {
            localPlayer = player2;
        }
    }

    public void Start()
    {
        localPlayer.Draw(5);
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
