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

        public ProtocolInfo(String _name)
        {
            name = _name;
        }
    }
}
