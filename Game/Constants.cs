using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileOnline.Game
{
    public static class Constants
    {
        public const int PROTOCOL_WIDTH = 150;
        public const int PROTOCOL_HEIGHT = 75;
        public const int CARD_WIDTH = 125;
        public const int CARD_HEIGHT = 200;
        public const int CARD_STACK_SEPARATION = 75;
        public const int CONTROL_OPP_TOP = -200;
        public const int CONTROL_OPP_BOTTOM = 0;
        public const int CONTROL_PLAYER_TOP = 0;
        public const int CONTROL_PLAYER_BOTTOM = 200;
        public const int CONTROL_TOP = -100;
        public const int CONTROL_BOTTOM = 100;
        public const int REVEAL_PANEL_MARGINS = 175;

        public readonly static Color DEFAULT_COLOR = 
            new Color((float)153 / 256, (float)153 / 256, (float)153 / 256);
    }
}
