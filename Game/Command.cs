using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileOnline.Game
{
    public class Command
    {
        public int num;

        public PromptManager.Prompt type;

        public Command(PromptManager.Prompt type, int num)
        {
            this.type = type;
            this.num = num;
        }
    }
}