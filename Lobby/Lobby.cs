using Godot;
using System;
using System.Linq;

public partial class Lobby : Control
{
    private int port = 8910;
    private string address = "157.245.123.33";
    [Export]
    public int text = 0;

    public override void _Ready()
    {
        Label l = GetNode("VBoxContainer").GetNode("Label") as Label;
        l.Text = text.ToString();
        Multiplayer.PeerConnected += PeerConnected;
        Multiplayer.ConnectedToServer += ConnectedToServer;

        if (OS.GetCmdlineArgs().Contains("--server"))
        {
            ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
            var error = peer.CreateServer(port);
            if (error != Error.Ok)
            {
                GD.Print("Error: " + error.ToString());
            }

            Multiplayer.MultiplayerPeer = peer;
            GD.Print("Start");
        } else
        {
            GD.Print("Start");
            ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
            peer.CreateClient(address, port);

            Multiplayer.MultiplayerPeer = peer;
        }
    }

    private void PeerConnected(long id)
    {

    }

    private void ConnectedToServer()
    {
        GD.Print("Connected");
    }

    private void _on_button_pressed()
    {
        Rpc(nameof(UpdateText));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void UpdateText()
    {
        text += 1;
        Label l = GetNode("VBoxContainer").GetNode("Label") as Label;
        l.Text = text.ToString();
    }
}
