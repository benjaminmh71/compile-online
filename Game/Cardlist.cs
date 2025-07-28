using System;
using System.Collections.Generic;
using Godot;
using CompileOnline.Game;
using System.Threading.Tasks;
using System.Linq;

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
            await Game.instance.localPlayer.Discard(1);
        };
        apathy.cards.Add(apathy5);


        ProtocolInfo darkness = new ProtocolInfo("Darkness");
        darkness.backgroundColor = new Color((float)55/256, (float)55/256, (float)70/256);
        protocols["Darkness"] = darkness;

        CardInfo darkness0 = new CardInfo("Darkness", 0);
        darkness0.middleText = "Draw 3 cards. Shift 1 of your opponent's covered cards.";
        darkness0.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Draw(3);
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
                PromptManager.PromptAction([PromptManager.Prompt.Play], Game.instance.localPlayer.hand, protocols,
                    (Card c, Protocol p) => true);
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
            await Game.instance.localPlayer.Discard(1);
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
            PromptManager.PromptAction([PromptManager.Prompt.EndAction, PromptManager.Prompt.CustomButtonA], "Draw 1 card");
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            if (response.type == PromptManager.Prompt.EndAction) return;

            await Game.instance.localPlayer.Draw(1);
            List<Card> deletableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered && c != card)
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
            await Game.instance.localPlayer.Discard(1);
        };
        death.cards.Add(death5);

        ProtocolInfo fire = new ProtocolInfo("Fire");
        fire.backgroundColor = new Color((float)200 / 256, (float)50 / 256, 0);
        protocols["Fire"] = fire;

        CardInfo fire0 = new CardInfo("Fire", 0);
        fire0.middleText = "Draw 2 cards. Flip 1 other card.";
        fire0.bottomText = "When this card would be covered: first, draw 1 card, then flip 1 other card.";
        fire0.OnPlay = async (Card card) => {
            await Game.instance.localPlayer.Draw(2);
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
            await Game.instance.localPlayer.Draw(1);
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
            if (card.covered) return;
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
                await Game.instance.localPlayer.Draw(1);
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
            await Game.instance.localPlayer.Draw(count + 1);
        };
        fire.cards.Add(fire4);

        CardInfo fire5 = new CardInfo("Fire", 5);
        fire5.middleText = "Discard a card.";
        fire5.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Discard(1);
        };
        fire.cards.Add(fire5);

        ProtocolInfo gravity = new ProtocolInfo("Gravity");
        gravity.backgroundColor = new Color((float)160 / 256, (float)80 / 256, (float)230 / 256);
        protocols["Gravity"] = gravity;

        CardInfo gravity0 = new CardInfo("Gravity", 0);
        gravity0.middleText = "For every 2 cards in this line, play the top card of your deck face-down under this card.";
        gravity0.OnPlay = async (Card card) =>
        {
            Protocol protocol = Game.instance.GetProtocolOfCard(card);
            int total = 0;
            foreach (Card c in protocol.cards) total++;
            foreach (Card c in Game.instance.GetOpposingProtocol(protocol).cards) total++;
            for (int i = 0; i < total / 2; i++) Game.instance.localPlayer.PlayTopUnderneath(protocol);
        };
        gravity.cards.Add(gravity0);

        CardInfo gravity1 = new CardInfo("Gravity", 1);
        gravity1.middleText = "Draw 2 cards. Shift 1 card either to or from this line.";
        gravity1.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Draw(2);
            Protocol protocol = Game.instance.GetProtocolOfCard(card);
            List<Card> shiftableToCards = new List<Card>();
            List<Card> shiftableFromCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                Protocol p = Game.instance.GetProtocolOfCard(c);
                if (!c.covered && Game.instance.Line(p) != Game.instance.Line(protocol))
                    shiftableToCards.Add(c);
                if (!c.covered && Game.instance.Line(p) == Game.instance.Line(protocol))
                    shiftableFromCards.Add(c);
            }

            async Task ShiftTo()
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Shift 1 card.";
                List<Protocol> protocols = [
                Game.instance.GetProtocolOfCard(card),
                Game.instance.GetOpposingProtocol(Game.instance.GetProtocolOfCard(card))];
                PromptManager.PromptAction([PromptManager.Prompt.Shift], shiftableToCards, protocols);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Shift(response.card, response.protocol);
            }

            async Task ShiftFrom()
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Shift 1 card.";
                List<Protocol> protocols = Game.instance.GetProtocols();
                protocols.Remove(Game.instance.GetProtocolOfCard(card));
                protocols.Remove(Game.instance.GetOpposingProtocol(Game.instance.GetProtocolOfCard(card)));
                PromptManager.PromptAction([PromptManager.Prompt.Shift], shiftableFromCards, protocols);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Shift(response.card, response.protocol);
            }

            if (shiftableToCards.Count > 0 && shiftableFromCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Shift to or from Gravity 1's line?";
                PromptManager.PromptAction([PromptManager.Prompt.CustomButtonA, PromptManager.Prompt.CustomButtonB],
                    "Shift to", "Shift from");
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                if (response.type == PromptManager.Prompt.CustomButtonA)
                    await ShiftTo();
                else await ShiftFrom();
            }
            else if (shiftableToCards.Count > 0)
                await ShiftTo();
            else if (shiftableFromCards.Count > 0)
                await ShiftFrom();
        };
        gravity.cards.Add(gravity1);

        CardInfo gravity2 = new CardInfo("Gravity", 2);
        gravity2.middleText = "Flip 1 card. Shift that card to this line.";
        gravity2.OnPlay = async (Card card) => {
            List<Card> flippableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered) flippableCards.Add(c);
            }
            if (flippableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Flip 1 card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], flippableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Flip(response.card);
                Protocol protocol = Game.instance.GetProtocolOfCard(card);
                if (Game.instance.IsLocal(response.card))
                    await Game.instance.localPlayer.Shift(response.card, protocol);
                else
                    await Game.instance.localPlayer.Shift(response.card, Game.instance.GetOpposingProtocol(protocol));
            }
        };
        gravity.cards.Add(gravity2);

        CardInfo gravity4 = new CardInfo("Gravity", 4);
        gravity4.middleText = "Shift 1 face-down card to this line.";
        gravity4.OnPlay = async (Card card) =>
        {
            List<Card> facedownCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered && c.flipped) facedownCards.Add(c);
            }
            if (facedownCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Shift 1 face-down card.";
                List<Protocol> protocols = [
                Game.instance.GetProtocolOfCard(card),
                Game.instance.GetOpposingProtocol(Game.instance.GetProtocolOfCard(card))];
                PromptManager.PromptAction([PromptManager.Prompt.Shift], facedownCards, protocols);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Shift(response.card, response.protocol);
            }
        };
        gravity.cards.Add(gravity4);

        CardInfo gravity5 = new CardInfo("Gravity", 5);
        gravity5.middleText = "Discard a card.";
        gravity5.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Discard(1);
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

        ProtocolInfo hate = new ProtocolInfo("Hate");
        hate.backgroundColor = new Color((float)100 / 256, (float)70 / 256, (float)70 / 256);
        protocols["Hate"] = hate;

        CardInfo hate0 = new CardInfo("Hate", 0);
        hate0.middleText = "Delete 1 card.";
        hate0.OnPlay = async (Card card) =>
        {
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
        hate.cards.Add(hate0);

        CardInfo hate1 = new CardInfo("Hate", 1);
        hate1.middleText = "Discard 3 cards. Delete 1 card. Delete 1 card.";
        hate1.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Discard(3);
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
            deleteableCards.Clear();
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
        hate.cards.Add(hate1);

        CardInfo hate2 = new CardInfo("Hate", 2);
        hate2.middleText = "Delete your highest value card. Delete your opponent's highest value card.";
        hate2.OnPlay = async (Card card) =>
        {
            List<Card> highests = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (c.covered || !Game.instance.IsLocal(c)) continue;
                if (highests.Count == 0 || c.GetValue() == highests[0].GetValue())
                    highests.Add(c);
                if (c.GetValue() > highests[0].GetValue())
                {
                    highests.Clear();
                    highests.Add(c);
                }
            }
            if (highests.Count == 1) await Game.instance.localPlayer.Delete(highests[0]);
            else
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Delete 1 of your cards among those with the highest value.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], highests);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Delete(response.card);
            }
            highests.Clear();
            foreach (Card c in Game.instance.GetCards())
            {
                if (c.covered || Game.instance.IsLocal(c)) continue;
                if (highests.Count == 0 || c.GetValue() == highests[0].GetValue())
                    highests.Add(c);
                if (c.GetValue() > highests[0].GetValue())
                {
                    highests.Clear();
                    highests.Add(c);
                }
            }
            if (highests.Count == 1) await Game.instance.localPlayer.Delete(highests[0]);
            else
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Delete 1 of your opponent's cards among those with the highest value.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], highests);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Delete(response.card);
            }
        };
        hate.cards.Add(hate2);

        CardInfo hate3 = new CardInfo("Hate", 3);
        hate3.topText = "After you delete cards: draw 1 card.";
        hate3.OnDelete = async (Card card) => { await Game.instance.localPlayer.Draw(1); };
        hate.cards.Add(hate3);

        CardInfo hate4 = new CardInfo("Hate", 4);
        hate4.bottomText = "When this card would be covered: first, delete the lowest value covered card in this line.";
        hate4.OnCover = async (Card card) =>
        {
            Protocol protocol = Game.instance.GetProtocolOfCard(card);
            List<Card> lowests = new List<Card>();
            foreach (Card c in protocol.cards)
            {
                if (!c.covered) continue;
                if (lowests.Count == 0 || c.GetValue() == lowests[0].GetValue())
                    lowests.Add(c);
                if (c.GetValue() < lowests[0].GetValue())
                {
                    lowests.Clear();
                    lowests.Add(c);
                }
            }
            foreach (Card c in Game.instance.GetOpposingProtocol(protocol).cards)
            {
                if (!c.covered) continue;
                if (lowests.Count == 0 || c.GetValue() == lowests[0].GetValue())
                    lowests.Add(c);
                if (c.GetValue() < lowests[0].GetValue())
                {
                    lowests.Clear();
                    lowests.Add(c);
                }
            }
            if (lowests.Count == 1)
                await Game.instance.localPlayer.Delete(lowests[0]);
            else if (lowests.Count > 1)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Delete 1 covered card of those with the lowest value.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], lowests);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Delete(response.card);
            }
        };
        hate.cards.Add(hate4);

        CardInfo hate5 = new CardInfo("Hate", 5);
        hate5.middleText = "Discard a card.";
        hate5.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Discard(1);
        };
        hate.cards.Add(hate5);

        ProtocolInfo life = new ProtocolInfo("Life");
        life.backgroundColor = new Color((float)50 / 256, (float)120 / 256, (float)50 / 256);
        protocols["Life"] = life;

        CardInfo life0 = new CardInfo("Life", 0);
        life0.topText = "End: if this card is covered, delete this card.";
        life0.middleText = "Play the top card of your deck face-down in each line where you have a card.";
        life0.OnPlay = async (Card card) =>
        {
            foreach (Protocol protocol in Game.instance.GetProtocols(true))
            {
                if (protocol.cards.Count > 0)
                    await Game.instance.localPlayer.PlayTop(protocol);
            }
        };
        life0.OnEnd = async (Card card) =>
        {
            if (card.covered)
                await Game.instance.localPlayer.Delete(card);
        };
        life.cards.Add(life0);

        CardInfo life1 = new CardInfo("Life", 1);
        life1.middleText = "Flip 1 card. Flip 1 card.";
        life1.OnPlay = async (Card card) =>
        {
            List<Card> flippableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered)
                    flippableCards.Add(c);
            }
            if (flippableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Flip 1 card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], flippableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Flip(response.card);
            }
            flippableCards.Clear();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered)
                    flippableCards.Add(c);
            }
            if (flippableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Flip 1 card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], flippableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Flip(response.card);
            }
        };
        life.cards.Add(life1);

        CardInfo life2 = new CardInfo("Life", 2);
        life2.middleText = "Draw 1 card. You may flip 1 face-down card.";
        life2.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Draw(1);
            List<Card> flippableCards = new List<Card>();
            foreach (Card c in Game.instance.GetCards())
            {
                if (!c.covered && c.flipped)
                    flippableCards.Add(c);
            }
            if (flippableCards.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "You may flip 1 face-down card.";
                PromptManager.PromptAction([PromptManager.Prompt.Select, PromptManager.Prompt.EndAction], flippableCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                if (response.type == PromptManager.Prompt.EndAction) return;
                await Game.instance.localPlayer.Flip(response.card);
            }
        };
        life.cards.Add(life2);

        CardInfo life3 = new CardInfo("Life", 3);
        life3.bottomText = "When this card would be covered: first, play the top card of your deck face-down in another line.";
        life3.OnCover = async (Card card) =>
        {
            List<Protocol> selectableProtocols = Game.instance.GetProtocols(true);
            for (int i = selectableProtocols.Count-1; i >= 0; i--)
            {
                if (selectableProtocols[i].cards.Contains(card)) selectableProtocols.Remove(selectableProtocols[i]);
            }
            if (selectableProtocols.Count > 0)
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Select a line.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], selectableProtocols);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.PlayTop(response.protocol);
            }
        };
        life.cards.Add(life3);

        CardInfo life4 = new CardInfo("Life", 4);
        life4.middleText = "If this card is covering a card, draw 1 card.";
        life4.OnPlay = async (Card card) =>
        {
            if (Game.instance.GetProtocolOfCard(card).cards.Count > 1)
            {
                await Game.instance.localPlayer.Draw(1);
            }
        };
        life.cards.Add(life4);

        CardInfo life5 = new CardInfo("Life", 5);
        life5.middleText = "Discard a card.";
        life5.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Discard(1);
        };
        life.cards.Add(life5);

        ProtocolInfo light = new ProtocolInfo("Light");
        light.backgroundColor = new Color((float)200 / 256, (float)140 / 256, (float)0 / 256);
        protocols["Light"] = light;

        CardInfo light0 = new CardInfo("Light", 0);
        light0.middleText = "Flip 1 card. Draw cards equal to that card's value.";
        light0.OnPlay = async (Card card) =>
        {
            List<Card> uncoveredCards = Game.instance.GetCards().FindAll(c => !c.covered);
            if (uncoveredCards.Count == 0) return;
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "Flip 1 card.";
            PromptManager.PromptAction([PromptManager.Prompt.Select], uncoveredCards);
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            await Game.instance.localPlayer.Flip(response.card);
            await Game.instance.localPlayer.Draw(response.card.GetValue());
        };
        light.cards.Add(light0);

        CardInfo light1 = new CardInfo("Light", 1);
        light1.bottomText = "End: Draw 1 card.";
        light1.OnEnd = async (Card card) =>
        {
            await Game.instance.localPlayer.Draw(1);
        };
        light.cards.Add(light1);

        CardInfo light2 = new CardInfo("Light", 2);
        light2.middleText = "Draw 2 cards. Reveal 1 face-down card. You may shift or flip that card.";
        light2.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Draw(2);
            List<Card> faceDownCards = Game.instance.GetCards().FindAll(c => c.flipped && !c.covered);
            if (faceDownCards.Count == 0) return;
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "Select a face-down card.";
            PromptManager.PromptAction([PromptManager.Prompt.Select], faceDownCards);
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            Card selectedCard = response.card;
            Game.instance.localPlayer.Reveal(new List<Card>{ selectedCard });
            prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "You may shift or flip that card.";
            PromptManager.PromptAction([PromptManager.Prompt.CustomButtonA, PromptManager.Prompt.CustomButtonB,
                PromptManager.Prompt.EndAction], "Shift", "Flip");
            response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            if (response.type == PromptManager.Prompt.CustomButtonA)
            {
                prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Shift the card.";
                List<Protocol> protocols = Game.instance.GetProtocols(Game.instance.IsLocal(selectedCard));
                PromptManager.PromptAction([PromptManager.Prompt.Shift], new List<Card> { selectedCard }, protocols);
                response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.Shift(selectedCard, response.protocol);
            } else if (response.type == PromptManager.Prompt.CustomButtonB)
            {
                await Game.instance.localPlayer.Flip(selectedCard);
            }
        };
        light.cards.Add(light2);

        CardInfo light3 = new CardInfo("Light", 3);
        light3.middleText = "Shift all face-down cards in this line to another line.";
        light3.OnPlay = async (Card card) =>
        {
            List<Card> faceDownCards = new List<Card>();
            foreach (Card c in Game.instance.GetProtocolOfCard(card).cards)
                if (c.flipped) faceDownCards.Add(c);
            foreach (Card c in Game.instance.GetOpposingProtocol(Game.instance.GetProtocolOfCard(card)).cards)
                if (c.flipped) faceDownCards.Add(c);
            if (faceDownCards.Count == 0) return;
            List<Protocol> selectableProtocols = Game.instance.GetProtocols(true);
            selectableProtocols.Remove(Game.instance.GetProtocolOfCard(card));
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "Select a line.";
            PromptManager.PromptAction([PromptManager.Prompt.Select], selectableProtocols);
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            await Game.instance.localPlayer.MultiShift(faceDownCards, response.protocol);
        };
        light.cards.Add(light3);

        CardInfo light4 = new CardInfo("Light", 4);
        light4.middleText = "Your opponent reveals their hand.";
        light4.OnPlay = async (Card card) =>
        {
            Game.instance.localPlayer.Reveal(Game.instance.localPlayer.oppHand);
        };
        light.cards.Add(light4);

        CardInfo light5 = new CardInfo("Light", 5);
        light5.middleText = "Discard a card.";
        light5.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Discard(1);
        };
        light.cards.Add(light5);

        ProtocolInfo love = new ProtocolInfo("Love");
        love.backgroundColor = new Color((float)225 / 256, (float)100 / 256, (float)100 / 256);
        protocols["Love"] = love;

        CardInfo love1 = new CardInfo("Love", 1);
        love1.middleText = "Draw the top card of your opponent's deck.";
        love1.bottomText = "End: you may give 1 card from your hand to your opponent. If you do, draw 2 cards.";
        love1.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.DrawFromOpp();
        };
        love1.OnEnd = async (Card card) =>
        {
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "You may give 1 card in your hand to your opponent.";
            PromptManager.PromptAction([PromptManager.Prompt.Select, PromptManager.Prompt.EndAction], Game.instance.localPlayer.hand);
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            if (response.type == PromptManager.Prompt.EndAction) return;
            await Game.instance.localPlayer.SendCommand(new Command(Player.CommandType.Give, new List<Card> { response.card }));
            await Game.instance.localPlayer.Draw(2);
        };
        love.cards.Add(love1);

        CardInfo love2 = new CardInfo("Love", 2);
        love2.middleText = "Your opponent draws 1 card. Refresh.";
        love2.OnPlay = async (Card card) =>
        {
            Command command = new Command(Player.CommandType.Draw, 1);
            await Game.instance.localPlayer.SendCommand(command);
            await Game.instance.localPlayer.Refresh();
        };
        love.cards.Add(love2);

        CardInfo love3 = new CardInfo("Love", 3);
        love3.middleText = "Take 1 random card from your opponent's hand. Give 1 card from your hand to your opponent.";
        love3.OnPlay = async (Card card) =>
        {
            List<Card> oppHand = Game.instance.localPlayer.oppHand;
            Card randomCard = oppHand[Utility.random.RandiRange(0, oppHand.Count-1)];
            Command command = new Command(Player.CommandType.Steal, new List<Card> { randomCard });
            await Game.instance.localPlayer.SendCommand(command);
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "Give 1 card in your hand to your opponent.";
            PromptManager.PromptAction([PromptManager.Prompt.Select], Game.instance.localPlayer.hand);
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            await Game.instance.localPlayer.SendCommand
                (new Command(Player.CommandType.Give, new List<Card> { response.card }));
        };
        love.cards.Add(love3);

        CardInfo love4 = new CardInfo("Love", 4);
        love4.middleText = "Reveal 1 card from your hand. Flip 1 card.";
        love4.OnPlay = async (Card card) =>
        {
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "Reveal 1 card in your hand.";
            PromptManager.PromptAction([PromptManager.Prompt.Select], Game.instance.localPlayer.hand);
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            await Game.instance.localPlayer.SendCommand
                (new Command(Player.CommandType.Reveal, new List<Card> { response.card }));
            prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "Flip 1 card.";
            PromptManager.PromptAction([PromptManager.Prompt.Select], Game.instance.GetCards().FindAll(c => !c.covered));
            response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            await Game.instance.localPlayer.Flip(response.card);
        };
        love.cards.Add(love4);

        CardInfo love5 = new CardInfo("Love", 5);
        love5.middleText = "Discard a card.";
        love5.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Discard(1);
        };
        love.cards.Add(love5);

        CardInfo love6 = new CardInfo("Love", 6);
        love6.middleText = "Your opponent draws 2 cards.";
        love6.OnPlay = async (Card card) =>
        {
            Command command = new Command(Player.CommandType.Draw, 2);
            await Game.instance.localPlayer.SendCommand(command);
        };
        love.cards.Add(love6);

        ProtocolInfo metal = new ProtocolInfo("Metal");
        metal.backgroundColor = new Color((float)140 / 256, (float)170 / 256, (float)170 / 256);
        protocols["Metal"] = metal;

        CardInfo metal0 = new CardInfo("Metal", 0);
        metal0.topText = "Your opponent's total value in this line is reduced by 2.";
        metal0.middleText = "Flip 1 card.";
        metal0.passives = [CardInfo.Passive.ReduceOppValueByTwo];
        metal0.OnPlay = async (Card card) =>
        {
            String prevText = Game.instance.promptLabel.Text;
            Game.instance.promptLabel.Text = "Flip 1 card.";
            PromptManager.PromptAction([PromptManager.Prompt.Select], Game.instance.GetCards().FindAll(c => !c.covered));
            Response response = await Game.instance.localPlayer.WaitForResponse();
            Game.instance.promptLabel.Text = prevText;
            await Game.instance.localPlayer.Flip(response.card);
        };
        metal.cards.Add(metal0);

        CardInfo metal1 = new CardInfo("Metal", 1);
        metal1.middleText = "Draw 2 cards. Your opponent can't compile next turn.";
        metal1.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Draw(2);
            Game.instance.localPlayer.ApplyTempEffect(CardInfo.TempEffect.NoCompile, 1);
        };
        metal.cards.Add(metal1);

        CardInfo metal2 = new CardInfo("Metal", 2);
        metal2.topText = "Your opponent cannot play cards face-down in this line.";
        metal2.passives = [ CardInfo.Passive.NoFaceDown ];
        metal.cards.Add(metal2);

        CardInfo metal3 = new CardInfo("Metal", 3);
        metal3.middleText = "Draw 1 card. Delete all cards in 1 other line with 8 or more cards.";
        metal3.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Draw(1);
            List<Protocol> protocols = new List<Protocol>();
            foreach (Protocol protocol in Game.instance.GetProtocols(true))
            {
                if (protocol.cards.Count + Game.instance.GetOpposingProtocol(protocol).cards.Count >= 8)
                {
                    protocols.Add(protocol);
                }
            }
            if (protocols.Count == 0) return;
            if (protocols.Count == 1)
            {
                await Game.instance.localPlayer.MultiDelete(
                    protocols[0].cards.Concat(Game.instance.GetOpposingProtocol(protocols[0]).cards).ToList());
            } else
            {
                String prevText = Game.instance.promptLabel.Text;
                Game.instance.promptLabel.Text = "Select a line to delete in.";
                PromptManager.PromptAction([PromptManager.Prompt.Select], protocols);
                Response response = await Game.instance.localPlayer.WaitForResponse();
                Game.instance.promptLabel.Text = prevText;
                await Game.instance.localPlayer.MultiDelete(
                    response.protocol.cards.Concat(Game.instance.GetOpposingProtocol(response.protocol).cards).ToList());
            }
        };
        metal.cards.Add(metal3);

        CardInfo metal5 = new CardInfo("Metal", 5);
        metal5.middleText = "Discard a card.";
        metal5.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Discard(1);
        };
        metal.cards.Add(metal5);

        CardInfo metal6 = new CardInfo("Metal", 6);
        metal6.topText = "When this card would be covered or flipped: first, delete this card.";
        metal6.OnCover = async (Card card) =>
        {
            await Game.instance.localPlayer.Delete(card);
        };
        metal6.OnFlip = async (Card card) =>
        {
            await Game.instance.localPlayer.Delete(card);
        };
        metal.cards.Add(metal6);

        ProtocolInfo plague = new ProtocolInfo("Plague");
        plague.backgroundColor = new Color((float)110 / 256, (float)150 / 256, (float)90 / 256);
        protocols["Plague"] = plague;

        CardInfo plague0 = new CardInfo("Plague", 0);
        plague0.middleText = "Your opponent discards 1 card.";
        plague0.bottomText = "Your opponent cannot play cards in this line.";
        plague.cards.Add(plague0);

        CardInfo plague1 = new CardInfo("Plague", 1);
        plague1.topText = "After your opponent discards cards: draw 1 card.";
        plague1.middleText = "Your opponent discards 1 card.";
        plague.cards.Add(plague1);

        CardInfo plague2 = new CardInfo("Plague", 2);
        plague2.middleText = "Discard 1 or more cards. Your opponent discards the amount of cards discarded plus 1.";
        plague.cards.Add(plague2);

        CardInfo plague3 = new CardInfo("Plague", 3);
        plague3.middleText = "Flip each other face-up card.";
        plague.cards.Add(plague3);

        CardInfo plague4 = new CardInfo("Plague", 4);
        plague4.bottomText = "End: your opponent deletes one of their face down cards. You may flip this card.";
        plague.cards.Add(plague4);

        CardInfo plague5 = new CardInfo("Plague", 5);
        plague5.middleText = "Discard a card.";
        plague5.OnPlay = async (Card card) =>
        {
            await Game.instance.localPlayer.Discard(1);
        };
        plague.cards.Add(plague5);
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
