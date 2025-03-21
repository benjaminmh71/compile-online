using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


partial class Room : Node
{
    public int player1Id = -1;
    public int player2Id = -1;

    public Room(int _player1ID)
    {
        player1Id = _player1ID;
    }
}
