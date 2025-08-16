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
    public String name;
    public Draft.DraftType draftType;
    public String password;

    public Room(int _player1ID, String _name, Draft.DraftType _draftType, String _password)
    {
        player1Id = _player1ID;
        name = _name;
        draftType = _draftType;
        password = _password;
    }
}
