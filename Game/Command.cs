using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileOnline.Game
{
    public class Command
    {
        public int num = 0;
        public List<Card> cards = new List<Card>();
        public List<Protocol> protocols = new List<Protocol>();

        public Player.CommandType type;

        public Command(Player.CommandType type, int num)
        {
            this.type = type;
            this.num = num;
        }

        public Command(Player.CommandType type, List<Card> cards)
        {
            this.type = type;
            this.cards = cards;
        }

        public Command(Player.CommandType type, List<Protocol> protocols)
        {
            this.type = type;
            this.protocols = protocols;
        }
    }
}