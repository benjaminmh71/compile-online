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
    PanelContainer passwordPanel;
    VBoxContainer gameSettingsVBox;
    List<CheckBox> draftCheckBoxes;
    TextEdit roomNameTextEdit;
    TextEdit roomPasswordTextEdit;

    public override void _Ready()
    {
        roomListContainer = GetNode<VBoxContainer>("RoomListContainer");
        gameSettingsPanel = GetNode<Panel>("GameSettingsPanel");
        waitRoomPanel = GetNode<Panel>("WaitRoomPanel");
        passwordPanel = GetNode<PanelContainer>("PasswordPanel");
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

    public void ResetGame()
    {
        game = GD.Load<PackedScene>("res://Game/Game.tscn").Instantiate<Game>();
    }

    private void PeerConnected(long id)
    {
        if (isServer)
        {
            GD.Print("New player");
            foreach (Room room in rooms)
            {
                RpcId(id, nameof(AddRoom), room.player1Id, room.name, (int)room.draftType, room.password);
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

        Rpc(nameof(AddRoom), Multiplayer.GetUniqueId(), roomNameTextEdit.Text, 
            (int)game.draftType, roomPasswordTextEdit.Text);
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
    private void AddRoom(int id, String name, int _draftType, String password)
    {
        Draft.DraftType draftType = (Draft.DraftType)_draftType;
        Room room = new Room(id, name, draftType, password);
        rooms.Add(room);
        PackedScene listRoomScene = GD.Load<PackedScene>("res://Lobby/list_room.tscn");
        ListRoom listRoom = listRoomScene.Instantiate<ListRoom>();
        roomListContainer.AddChild(listRoom);
        listRoom.creatorId = id;
        listRoom.nameLabel.Text = name + ": ";
        listRoom.draftTypeLabel.Text = draftType == Draft.DraftType.BanDraft ? "8 Ban Draft" :
            draftType == Draft.DraftType.Draft ? "7 Draft" : "Random Draft";
        listRoom.passwordLabel.Text = password.Length != 0 ? "Password" : "No password";
        listRoom.joinButton.Pressed += () => {
            void JoinGame()
            {
                game.draftType = draftType;
                room.player2Id = Multiplayer.GetUniqueId();
                Rpc(nameof(DeleteRoom), id);
                ClientJoinGame(id, Multiplayer.GetUniqueId());
            }
            void OnPasswordEnter() {
                String input = passwordPanel.GetNode("VBoxContainer").GetNode<TextEdit>("TextEdit").Text;
                passwordPanel.GetNode("VBoxContainer").GetNode<TextEdit>("TextEdit").Text = "";
                passwordPanel.Visible = false;
                passwordPanel.GetNode("VBoxContainer").GetNode<Button>("EnterButton").Pressed -= OnPasswordEnter;
                if (input == password) JoinGame();
            }
            if (password.Length == 0) JoinGame();
            else
            {
                passwordPanel.Visible = true;
                passwordPanel.GetNode("VBoxContainer").GetNode<Button>("EnterButton").Pressed += OnPasswordEnter;
            }
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
        Visible = false;
        await game.Init(p1Id, p2Id, p1Id == Multiplayer.GetUniqueId());
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private async void HostJoinGame(int p1Id, int p2Id)
    {
        GetTree().Root.AddChild(game);
        GD.Print((p1Id == Multiplayer.GetUniqueId()) ? "Player 1: " + p1Id.ToString() + " " + p2Id.ToString() :
            "Player 2: " + p1Id.ToString() + " " + p2Id.ToString());
        Visible = false;
        await game.Init(p1Id, p2Id, p1Id == Multiplayer.GetUniqueId());
        //RpcId(p2Id, nameof(ClientStartGame), p1Id);
    }
}
