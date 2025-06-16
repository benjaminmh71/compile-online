using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileOnline.Game
{
    public class ProtocolInfo
    {
        public String name;
        public List<CardInfo> cards = new List<CardInfo>();
        public Color backgroundColor = new Color((float)153/256, (float)153/256, (float)153/256);
        public Color textColor = new Color(1, 1, 1);

        public ProtocolInfo(String _name)
        {
            name = _name;
        }
    }
}
