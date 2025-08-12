using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
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
    Panel gameSettingsPanel;
    Panel waitRoomPanel;
    VBoxContainer gameSettingsVBox;
    List<CheckBox> draftCheckBoxes;
    TextEdit roomNameTextEdit;
    TextEdit roomPasswordTextEdit;

    public override void _Ready()
    {
        roomListContainer = GetNode<VBoxContainer>("RoomListContainer");
        gameSettingsPanel = GetNode<Panel>("GameSettingsPanel");
        waitRoomPanel = GetNode<Panel>("WaitRoomPanel");
        gameSettingsVBox = gameSettingsPanel.GetNode("MarginContainer").GetNode<VBoxContainer>("VBoxContainer");
        draftCheckBoxes = [ gameSettingsVBox.GetNode<CheckBox>("RandomCheckBox"),
        gameSettingsVBox.GetNode<CheckBox>("DraftCheckBox"),
        gameSettingsVBox.GetNode<CheckBox>("BanDraftCheckBox")];
        roomNameTextEdit = gameSettingsVBox.GetNode<TextEdit>("RoomNameTextEdit");
        roomPasswordTextEdit = gameSettingsVBox.GetNode<TextEdit>("RoomPasswordTextEdit");
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

        foreach (CheckBox checkbox in draftCheckBoxes)
        {
            checkbox.Pressed += () => OnDraftCheckBoxPressed(checkbox);
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

    private void OnDraftCheckBoxPressed(CheckBox checkbox)
    {
        foreach (CheckBox c in draftCheckBoxes)
        {
            if (c != checkbox) c.ButtonPressed = false;
        }
        checkbox.ButtonPressed = true;
    }

    private void OnFinishButtonPressed()
    {
        gameSettingsPanel.Visible = false;
        waitRoomPanel.Visible = true;
        if (draftCheckBoxes.Find(c => c.Name == "RandomCheckBox").IsPressed()) 
            game.draftType = Draft.DraftType.Random;
        if (draftCheckBoxes.Find(c => c.Name == "DraftCheckBox").IsPressed())
            game.draftType = Draft.DraftType.Draft;
        if (draftCheckBoxes.Find(c => c.Name == "BanDraftCheckBox").IsPressed())
            game.draftType = Draft.DraftType.BanDraft;

        Rpc(nameof(AddRoom), Multiplayer.GetUniqueId(), roomNameTextEdit.Text);
    }

    private void _on_create_game_pressed()
    {
        gameSettingsPanel.Visible = true;
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
    private void AddRoom(int id, String name)
    {
        Room room = new Room(id);
        rooms.Add(room);
        PackedScene listRoomScene = GD.Load<PackedScene>("res://Lobby/list_room.tscn");
        ListRoom listRoom = listRoomScene.Instantiate<ListRoom>();
        roomListContainer.AddChild(listRoom);
        listRoom.creatorId = id;
        listRoom.GetNode<Label>("GameInfo").Text = name + ": ";
        listRoom.GetNode<Button>("JoinButton").Pressed += () => {
            room.player2Id = Multiplayer.GetUniqueId();
            Rpc(nameof(DeleteRoom), id);
            ClientJoinGame(id, Multiplayer.GetUniqueId());
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
    private async void ClientJoinGame(int p1Id, int p2Id)
    {
        RpcId(p1Id, nameof(HostJoinGame), p1Id, p2Id);
        GetTree().Root.AddChild(game);
        GD.Print((p1Id == Multiplayer.GetUniqueId()) ? "Player 1: " + p1Id.ToString() + " " + p2Id.ToString() :
            "Player 2: " + p1Id.ToString() + " " + p2Id.ToString());
        QueueFree();
        await game.Init(p1Id, p2Id, p1Id == Multiplayer.GetUniqueId());
        
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private async void HostJoinGame(int p1Id, int p2Id)
    {
        GetTree().Root.AddChild(game);
        GD.Print((p1Id == Multiplayer.GetUniqueId()) ? "Player 1: " + p1Id.ToString() + " " + p2Id.ToString() :
            "Player 2: " + p1Id.ToString() + " " + p2Id.ToString());
        QueueFree();
        await game.Init(p1Id, p2Id, p1Id == Multiplayer.GetUniqueId());
        //RpcId(p2Id, nameof(ClientStartGame), p1Id);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void ClientStartGame(int hostId)
    {
        //game.Start();
        QueueFree();
        RpcId(hostId, nameof(HostStartGame));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void HostStartGame()
    {
        //game.Start();
        QueueFree();
    }
}
