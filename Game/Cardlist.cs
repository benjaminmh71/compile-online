using System;
using System.Collections.Generic;
using Godot;
using CompileOnline.Game;

public static class Cardlist
{
    public static Dictionary<String, ProtocolInfo> protocols = new Dictionary<String, ProtocolInfo>();

    static Cardlist()
    {
        // Vile evil data management:

        ProtocolInfo apathy = new ProtocolInfo("Apathy");
        apathy.backgroundColor = new Color((float)153/256, (float)153/256, (float)153/256);
        protocols["Apathy"] = apathy;
        CardInfo apathy0 = new CardInfo("Apathy", 0);
        apathy0.topText = "Your total value in this line is increased by 1 for each " +
            "face-down card in this line.";
        apathy0.passives = [CardInfo.Passive.PlusOneForFaceDown];
        apathy.cards.Add(apathy0);

        CardInfo apathy1 = new CardInfo("Apathy", 1);
        apathy1.middleText = "Flip all other face-up cards in this line.";
        apathy1.OnPlay = async (Card card) =>
        {
            Protocol protocol = Game.instance.GetProtocolOfCard(card);
            foreach (Card c in protocol.cards)
            {
                if (!c.flipped && c != card) await Game.instance.localPlayer.Flip(c);
            }
            foreach (Card c in Game.instance.GetOpposingProtocol(protocol).cards)
            {
                if (!c.flipped) await Game.instance.localPlayer.Flip(c);
            }
        };
        apathy.cards.Add(apathy1);

        CardInfo apathy2 = new CardInfo("Apathy", 2);
        apathy2.topText = "Ignore all middle commands in this line.";
        apathy2.bottomText = "When this card would be covered: first, flip this card.";
        apathy2.passives = [CardInfo.Passive.NoMiddleCommands];
        apathy2.OnCover = async (Card card) => { await Game.instance.localPlayer.Flip(card); };
        apathy.cards.Add(apathy2);

        CardInfo apathy3 = new CardInfo("Apathy", 3);
        apathy3.middleText = "Flip 1 of your opponent's face-up cards.";
        apathy3.OnPlay = async (Card card) =>
        {
            List<Card> oppFaceUpCards = new List<Card>();
            foreach (Protocol p in Game.instance.GetProtocols(false))
            {
                foreach (Card c in p.cards)
                {
                    if (!c.covered && !c.flipped) oppFaceUpCards.Add(c);
                }
            }
            if (oppFaceUpCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Flip a card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], oppFaceUpCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Flip(response.card);
            }
        };
        apathy.cards.Add(apathy3);

        CardInfo apathy4 = new CardInfo("Apathy", 4);
        apathy4.middleText = "You may flip one of your face-up covered cards.";
        apathy4.OnPlay = async (Card card) =>
        {
            List<Card> coveredFaceUpCards = new List<Card>();
            foreach (Protocol p in Game.instance.GetProtocols(true))
            {
                foreach (Card c in p.cards)
                {
                    if (c.covered && !c.flipped) coveredFaceUpCards.Add(c);
                }
            }
            if (coveredFaceUpCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Flip a face-up covered card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], coveredFaceUpCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Flip(response.card);
            }
        };
        apathy.cards.Add(apathy4);

        CardInfo apathy5 = new CardInfo("Apathy", 5);
        apathy5.middleText = "Discard a card.";
        apathy5.OnPlay = async (Card card) =>
        {
            if (Game.instance.localPlayer.hand.Count > 0)
            {
                await Game.instance.localPlayer.Discard(1);
            }
        };
        apathy.cards.Add(apathy5);


        ProtocolInfo darkness = new ProtocolInfo("Darkness");
        darkness.backgroundColor = new Color((float)55/256, (float)55/256, (float)70/256);
        protocols["Darkness"] = darkness;

        CardInfo darkness0 = new CardInfo("Darkness", 0);
        darkness0.middleText = "Draw 3 cards. Shift 1 of your opponent's covered cards.";
        darkness0.OnPlay = async (Card card) =>
        {
            Game.instance.localPlayer.Draw(3);
            List<Card> shiftableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (c.covered && !Game.instance.IsLocal(c)) shiftableCards.Add(c);
            }
            if (shiftableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Shift 1 of your opponent's covered cards.";
                PromptManager.PromptAction([PromptManager.Prompt.Shift], shiftableCards, Game.instance.GetProtocols());
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Shift(response.card, response.protocol);
            }
        };
        darkness.cards.Add(darkness0);

        CardInfo darkness1 = new CardInfo("Darkness", 1);
        darkness1.middleText = "Flip 1 of your opponent's cards. You may shift that card.";
        darkness1.OnPlay = async (Card card) =>
        {
            List<Card> flippableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered && !Game.instance.IsLocal(c)) flippableCards.Add(c);
            }
            if (flippableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Flip 1 of your opponent's cards.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], flippableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Flip(response.card);
                Game.instance.promptLabel.Text = "You may shift that card.";
                PromptManager.PromptAction([PromptManager.Prompt.Shift, PromptManager.Prompt.EndAction], 
                    [response.card], Game.instance.GetProtocols());
                Response shiftResponse = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                if (shiftResponse.type == PromptManager.Prompt.EndAction) return;
                await Game.instance.localPlayer.Shift(shiftResponse.card, shiftResponse.protocol);
            }
        };
        darkness.cards.Add(darkness1);

        CardInfo darkness2 = new CardInfo("Darkness", 2);
        darkness2.topText = "Face-down cards in this stack have a value of 4.";
        darkness2.middleText = "You may flip 1 covered card in this line.";
        darkness2.passives = [CardInfo.Passive.FaceDownFours];
        darkness2.OnPlay = async (Card card) =>
        {
            List<Card> flippableCards = new List<Card>();
            Protocol protocol = Game.instance.GetProtocolOfCard(card);
            foreach (Card c in Game.instance.GetCards())
            {
                Protocol p = Game.instance.GetProtocolOfCard(c);
                if (c.covered && Game.instance.Line(p) == Game.instance.Line(protocol)) flippableCards.Add(c);
            }
            if (flippableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Flip a covered card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], flippableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Flip(response.card);
            }
        };
        darkness.cards.Add(darkness2);

        CardInfo darkness3 = new CardInfo("Darkness", 3);
        darkness3.middleText = "Play 1 card face-down in another line.";
        darkness3.OnPlay = async (Card card) =>
        {
            List<Protocol> protocols = new List<Protocol>();
            foreach (Protocol p in Game.instance.GetProtocols(true))
            {
                if (p != Game.instance.GetProtocolOfCard(card)) protocols.Add(p);
            }
            if (Game.instance.localPlayer.hand.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Play a card.";
                PromptManager.PromptAction([PromptManager.Prompt.Play], Game.instance.localPlayer.hand, protocols);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Play(response.protocol, response.card, true);
            }
        };
        darkness.cards.Add(darkness3);

        CardInfo darkness4 = new CardInfo("Darkness", 4);
        darkness4.middleText = "Shift 1 face-down card.";
        darkness4.OnPlay = async (Card card) =>
        {
            List<Card> shiftableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (c.flipped && !c.covered) shiftableCards.Add(c);
            }
            if (shiftableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Shift a face-down card.";
                PromptManager.PromptAction([PromptManager.Prompt.Shift], shiftableCards, Game.instance.GetProtocols());
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Shift(response.card, response.protocol);
            }
        };
        darkness.cards.Add(darkness4);

        CardInfo darkness5 = new CardInfo("Darkness", 5);
        darkness5.middleText = "Discard a card.";
        darkness5.OnPlay = async (Card card) =>
        {
            if (Game.instance.localPlayer.hand.Count > 0)
            {
                await Game.instance.localPlayer.Discard(1);
            }
        };
        darkness.cards.Add(darkness5);

        ProtocolInfo death = new ProtocolInfo("Death");
        death.backgroundColor = new Color((float)55 / 256, (float)40 / 256, (float)60 / 256);
        protocols["Death"] = death;

        CardInfo death0 = new CardInfo("Death", 0);
        death0.middleText = "Delete 1 card from each other line.";
        death0.OnPlay = async (Card card) =>
        {
            Protocol protocol = Game.instance.GetProtocolOfCard(card);
            List<Card> deletableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered && Game.instance.Line(Game.instance.GetProtocolOfCard(c)) != Game.instance.Line(protocol))
                    deletableCards.Add(c);
            }
            if (deletableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Delete 1 card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], deletableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                Protocol protocol2 = Game.instance.GetProtocolOfCard(response.card);
                await Game.instance.localPlayer.Delete(response.card);

                deletableCards.Clear();
                foreach (Card c in Game.instance.GetCards())
                {
                    if (!c.covered && Game.instance.Line(Game.instance.GetProtocolOfCard(c)) != Game.instance.Line(protocol)
                    && Game.instance.Line(Game.instance.GetProtocolOfCard(c)) != Game.instance.Line(protocol2))
                        deletableCards.Add(c);
                }
                if (deletableCards.Count > 0)
                {
                    Game.instance.promptLabel.Text = "Delete 1 card.";
                    PromptManager.PromptAction([PromptManager.Prompt.Select], deletableCards);
                    response = await Game.instance.localPlayer.WaitForResponse();
                    Game.instance.promptLabel.Text = prevText;
                    await Game.instance.localPlayer.Delete(response.card);
                }
            }
        };
        death.cards.Add(death0);

        CardInfo death1 = new CardInfo("Death", 1);
        death1.topText = "Start: you may draw 1 card. If you do, delete 1 other card, then delete this card.";
        death1.OnStart = async (Card card) =>
        {
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "You may draw 1 card to delete 1 card.";
            PromptManager.PromptAction([PromptManager.Prompt.EndAction, PromptManager.Prompt.CustomButton], "Draw 1 card");
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            if (response.type == PromptManager.Prompt.EndAction) return;

            List<Card> deletableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered && c != card)
                    deletableCards.Add(c);
            }
            Game.instance.promptLabel.Text = "Delete 1 card.";
            PromptManager.PromptAction([PromptManager.Prompt.Select], deletableCards);
            response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            await Game.instance.localPlayer.Delete(response.card);
            await Game.instance.localPlayer.Delete(card);
        };
        death.cards.Add(death1);

        CardInfo death2 = new CardInfo("Death", 2);
        death2.middleText = "Delete all cards in 1 line with values of 1 or 2.";
        death2.OnPlay = async (Card card) =>
        {
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "Choose a protocol.";
            PromptManager.PromptAction([PromptManager.Prompt.Select], Game.instance.GetProtocols());
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            List<Card> cards = new List<Card>();
            foreach (Card c in response.protocol.cards)
            {
                if (c.GetValue() == 1 || c.GetValue() == 2)
                    cards.Add(c);
            }
            foreach (Card c in Game.instance.GetOpposingProtocol(response.protocol).cards)
            {
                if (c.GetValue() == 1 || c.GetValue() == 2)
                    cards.Add(c);
            }
            await Game.instance.localPlayer.MultiDelete(cards);
        };
        death.cards.Add(death2);

        CardInfo death3 = new CardInfo("Death", 3);
        death3.middleText = "Delete 1 face-down card.";
        death3.OnPlay = async (Card card) =>
        {
            List<Card> deletableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered && c.flipped) deletableCards.Add(c);
            }
            if (deletableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Delete 1 face-down card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], deletableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Delete(response.card);
            }
        };
        death.cards.Add(death3);

        CardInfo death4 = new CardInfo("Death", 4);
        death4.middleText = "Delete 1 card with a value of 0 or 1.";
        death4.OnPlay = async (Card card) =>
        {
            List<Card> deletableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered && c.GetValue() == 0 || c.GetValue() == 1) deletableCards.Add(c);
            }
            if (deletableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Delete 1 card with value 0 or 1.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], deletableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Delete(response.card);
            }
        };
        death.cards.Add(death4);

        CardInfo death5 = new CardInfo("Death", 5);
        death5.middleText = "Discard a card.";
        death5.OnPlay = async (Card card) =>
        {
            if (Game.instance.localPlayer.hand.Count > 0)
            {
                await Game.instance.localPlayer.Discard(1);
            }
        };
        death.cards.Add(death5);

        ProtocolInfo fire = new ProtocolInfo("Fire");
        fire.backgroundColor = new Color((float)200 / 256, (float)50 / 256, 0);
        protocols["Fire"] = fire;

        CardInfo fire0 = new CardInfo("Fire", 0);
        fire0.middleText = "Draw 2 cards. Flip 1 other card.";
        fire0.bottomText = "When this card would be covered: first, draw 1 card, then flip 1 other card.";
        fire0.OnPlay = async (Card card) => {
            Game.instance.localPlayer.Draw(2);
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "Flip 1 card.";
            List<Card> flippableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered && c != card) flippableCards.Add(c);
            }
            if (flippableCards.Count > 0)
            {
                PromptManager.PromptAction([PromptManager.Prompt.Select], flippableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Flip(response.card);
            }
        };
        fire0.OnCover = async (Card card) =>
        {
            Game.instance.localPlayer.Draw(1);
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "Flip 1 card.";
            List<Card> flippableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered && c != card) flippableCards.Add(c);
            }
            if (flippableCards.Count > 0)
            {
                PromptManager.PromptAction([PromptManager.Prompt.Select], flippableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Flip(response.card);
            }
        };
        fire.cards.Add(fire0);

        CardInfo fire1 = new CardInfo("Fire", 1);
        fire1.middleText = "Discard 1 card. If you do, delete 1 card.";
        fire1.OnPlay = async (Card card) =>
        {
            if (Game.instance.localPlayer.hand.Count == 0) return;
            await Game.instance.localPlayer.Discard(1);
            List<Card> deleteableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered) deleteableCards.Add(c);
            }
            if (deleteableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Delete 1 card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], deleteableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Delete(response.card);
            }
        };
        fire.cards.Add(fire1);

        CardInfo fire2 = new CardInfo("Fire", 2);
        fire2.middleText = "Discard 1 card. If you do, return 1 card.";
        fire2.OnPlay = async (Card card) =>
        {
            if (Game.instance.localPlayer.hand.Count == 0) return;
            await Game.instance.localPlayer.Discard(1);
            List<Card> returnableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered) returnableCards.Add(c);
            }
            if (returnableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Return 1 card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], returnableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Return(response.card);
            }
        };
        fire.cards.Add(fire2);

        CardInfo fire3 = new CardInfo("Fire", 3);
        fire3.bottomText = "End: You may discard 1 card. If you do, flip 1 card.";
        fire3.OnEnd = async (Card card) =>
        {
            if (Game.instance.localPlayer.hand.Count == 0) return;
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "You may discard 1 card.";
            PromptManager.PromptAction([PromptManager.Prompt.EndAction, PromptManager.Prompt.Select],
                Game.instance.localPlayer.hand);
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            if (response.type == PromptManager.Prompt.EndAction) return;
            Game.instance.localPlayer.SendToDiscard(response.card);

            List<Card> flippableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered) flippableCards.Add(c);
            }
            if (flippableCards.Count > 0)
            {
                Game.instance.promptLabel.Text = "Flip a card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], flippableCards);
                response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Flip(response.card);
            }

        };
        fire.cards.Add(fire3);

        CardInfo fire4 = new CardInfo("Fire", 4);
        fire4.middleText = "Discard 1 or more cards. Draw the amount discarded plus 1.";
        fire4.OnPlay = async (Card card) => {
            if (Game.instance.localPlayer.hand.Count == 0)
            {
                Game.instance.localPlayer.Draw(1);
                return;
            }
            await Game.instance.localPlayer.Discard(1);
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "You may discard 1 card.";
            PromptManager.PromptAction([PromptManager.Prompt.EndAction, PromptManager.Prompt.Select], 
                Game.instance.localPlayer.hand);
            int count = 1;
            Response response;
            while ((response = await Game.instance.localPlayer.WaitForResponse()).type != PromptManager.Prompt.EndAction
            && Game.instance.localPlayer.hand.Count > 0)
            {
                Game.instance.localPlayer.SendToDiscard(response.card);
                count++;
                PromptManager.PromptAction([PromptManager.Prompt.EndAction, PromptManager.Prompt.Select],
                    Game.instance.localPlayer.hand);
            }
            Game.instance.promptLabel.Text = prevText;
            Game.instance.localPlayer.Draw(count + 1);
        };
        fire.cards.Add(fire4);

        CardInfo fire5 = new CardInfo("Fire", 5);
        fire5.middleText = "Discard a card.";
        fire5.OnPlay = async (Card card) =>
        {
            if (Game.instance.localPlayer.hand.Count > 0)
            {
                await Game.instance.localPlayer.Discard(1);
            }
        };
        fire.cards.Add(fire5);

        ProtocolInfo gravity = new ProtocolInfo("Gravity");
        gravity.backgroundColor = new Color((float)160 / 256, (float)80 / 256, (float)230 / 256);
        protocols["Gravity"] = gravity;

        CardInfo gravity0 = new CardInfo("Gravity", 0);
        gravity0.middleText = "For every 2 cards in this line, play the top card of your deck face-down under this card.";
        gravity.cards.Add(gravity0);

        CardInfo gravity1 = new CardInfo("Gravity", 1);
        gravity1.middleText = "Draw 2 cards. Shift 1 card either to or from this line.";
        gravity.cards.Add(gravity1);

        CardInfo gravity2 = new CardInfo("Gravity", 2);
        gravity2.middleText = "Flip 1 card. Shift that card to this line.";
        gravity.cards.Add(gravity2);

        CardInfo gravity4 = new CardInfo("Gravity", 4);
        gravity4.middleText = "Shift 1 face-down card to this line.";
        gravity.cards.Add(gravity4);

        CardInfo gravity5 = new CardInfo("Gravity", 5);
        gravity5.middleText = "Discard a card.";
        gravity5.OnPlay = async (Card card) =>
        {
            if (Game.instance.localPlayer.hand.Count > 0)
            {
                await Game.instance.localPlayer.Discard(1);
            }
        };
        gravity.cards.Add(gravity5);

        CardInfo gravity6 = new CardInfo("Gravity", 6);
        gravity6.middleText = "Your opponent plays the top card of their deck face down in this line.";
        gravity6.OnPlay = async (Card card) =>
        {
            List<Protocol> protocols = new List<Protocol>();
            protocols.Add(Game.instance.GetOpposingProtocol(Game.instance.GetProtocolOfCard(card)));
            Command command = new Command(Player.CommandType.PlayTop, protocols);
            await Game.instance.localPlayer.SendCommand(command);
        };
        gravity.cards.Add(gravity6);
    }

    public static CardInfo GetCard(String name)
    {
        foreach (ProtocolInfo protocolinfo in protocols.Values)
        {
            foreach (CardInfo cardinfo in protocolinfo.cards)
            {
                if (cardinfo.GetCardName() == name) return cardinfo;
            }
        }
        throw new Exception("Card not found");
    }
}
