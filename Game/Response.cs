using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileOnline.Game
{
    public class Response
    {
        public Card card;
        public Protocol protocol;
        public bool flipped;

        public PromptManager.Prompt type;

        public Response(PromptManager.Prompt type)
        {
            this.type = type;
        }

        public Response(Protocol protocol, PromptManager.Prompt type)
        {
            this.protocol = protocol;
            this.type = type;
        }

        public Response(Card card, PromptManager.Prompt type)
        {
            this.card = card;
            this.type = type;
        }

        public Response(Card card, Protocol protocol, PromptManager.Prompt type)
        {
            this.card = card;
            this.protocol = protocol;
            this.type = type;
        }

        public Response(Card card, Protocol protocol, bool flipped, PromptManager.Prompt type)
        {
            this.card = card;
            this.protocol = protocol;
            this.flipped = flipped;
            this.type = type;
        }
    }
}