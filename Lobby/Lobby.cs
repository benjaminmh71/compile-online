using Godot;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

public partial class Lobby : Control
{
    private int port = 8910;
    private string address = "157.245.123.33";
    ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
    bool isServer = false;
    ArrayList rooms = new ArrayList();
    Game game;
    VBoxContainer roomListContainer;

    public override void _Ready()
    {
        double x = 3.0;
        double y = 2.0;
        double total = x;
        for (int i = 0; i < 20; i++)
        {
            double temp = y;
            y = (x + y) / 2.0;
            x = temp;
            total += x;
        }
        GD.Print(x);
        GD.Print(total - (7.0/3.0 * 21));
        roomListContainer = GetNode<VBoxContainer>("RoomListContainer");
        game = GD.Load<PackedScene>("res://Game/Game.tscn").Instantiate<Game>();

        Multiplayer.PeerConnected += PeerConnected;
        Multiplayer.ConnectedToServer += ConnectedToServer;
        Multiplayer.PeerDisconnected += PeerDisconnected;

        if (OS.GetCmdlineArgs().Contains("--server"))
        {
            Error error = peer.CreateServer(port);
            if (error != Error.Ok)
            {
                GD.Print(error);
            }
            isServer = true;

            GD.Print("Started server");

            Multiplayer.MultiplayerPeer = peer;
        } else 
        {
            Error error = peer.CreateClient(address, port);
            if (error != Error.Ok)
            {
                GD.Print(error);
            }

            GD.Print("Started Client");

            Multiplayer.MultiplayerPeer = peer;
        }
    }

    private void PeerConnected(long id)
    {
        if (isServer)
        {
            GD.Print("New player");
            foreach (Room room in rooms)
            {
                RpcId(id, nameof(AddRoom), room.player1Id);
            }
        }
    }

    private void PeerDisconnected(long id)
    {
        DeleteRoom((int)id);
    }

    private void ConnectedToServer()
    {
        GD.Print("Connected");
    }

    private void _on_create_game_pressed()
    {
        Rpc(nameof(AddRoom), Multiplayer.GetUniqueId());
    }

    private Room GetRoom(int id)
    {
        foreach (Room room in rooms)
        {
            if (room.player1Id == id)
            {
                return room;
            }
        }
        return null;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void AddRoom(int id)
    {
        Room room = new Room(id);
        rooms.Add(room);
        PackedScene listRoomScene = GD.Load<PackedScene>("res://Lobby/list_room.tscn");
        ListRoom listRoom = listRoomScene.Instantiate<ListRoom>();
        roomListContainer.AddChild(listRoom);
        listRoom.creatorId = id;
        listRoom.GetNode<Button>("JoinButton").Pressed += () => {
            room.player2Id = Multiplayer.GetUniqueId();
            JoinGame(id, Multiplayer.GetUniqueId());
            RpcId(id, nameof(JoinGame), id, Multiplayer.GetUniqueId());
            Rpc(nameof(DeleteRoom), id);
            StartGame();
            RpcId(id, nameof(StartGame));
        };
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void DeleteRoom(int id)
    {
        for (int i = rooms.Count - 1; i >= 0; i--)
        {
            Room room = rooms[i] as Room;
            if (room.player1Id == id)
            {
                rooms.Remove(room);
                foreach (ListRoom listRoom in roomListContainer.GetChildren())
                {
                    if (listRoom.creatorId == id)
                    {
                        listRoom.QueueFree();
                    }
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void JoinGame(int p1Id, int p2Id)
    {
        GetTree().Root.AddChild(game);
        game.Init(p1Id, p2Id);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void StartGame()
    {
        game.Start();
        QueueFree();
    }
}
