using Godot;
using System;

public partial class Game : Control
{
    public Player player1;
    public Player player2;
    Player localPlayer;
    public HBoxContainer handCardsContainer;
    public HBoxContainer oppCardsContainer;
    public HBoxContainer localProtocolsContainer;
    public HBoxContainer oppProtocolsContainer;

    public void Init(int player1Id, int player2Id)
    {
        handCardsContainer = GetNode<HBoxContainer>("LocalHandCardsContainer");
        oppCardsContainer = GetNode<HBoxContainer>("OppHandCardsContainer");
        localProtocolsContainer = GetNode<HBoxContainer>("LocalProtocolsContainer");
        oppProtocolsContainer = GetNode<HBoxContainer>("OppProtocolsContainer");
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

        player1 = new Player(player1Id, player2Id, this);
        player2 = new Player(player2Id, player1Id, this);
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
}
