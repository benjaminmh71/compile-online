using Godot;
using System;

public partial class Game : Control
{
    public Player player1;
    public Player player2;
    Player localPlayer;

    public void Init(int player1Id, int player2Id)
    {
        player1 = new Player(player1Id);
        AddChild(player1);
        player2 = new Player(player2Id);
        AddChild(player2);
        if (Multiplayer.GetUniqueId() == player1Id)
        {
            localPlayer = player1;
        } else
        {
            localPlayer = player2;
        }

        localPlayer.Draw(5);
    }
}
