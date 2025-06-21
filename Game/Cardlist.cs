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
                PromptManager.PromptAction([PromptManager.Prompt.Select], coveredFaceUpCards);
                Response response = await Game.instance.localPlayer.WaitForResponse();
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
        darkness0.middleText = "Draw 3 cards. Shift one of your opponent's covered cards.";
        darkness.cards.Add(darkness0);

        CardInfo darkness1 = new CardInfo("Darkness", 1);
        darkness1.middleText = "Flip 1 of your opponent's cards. You may shift that card.";
        darkness.cards.Add(darkness1);

        CardInfo darkness2 = new CardInfo("Darkness", 2);
        darkness2.topText = "Face-down cards in this stack have a value of 4.";
        darkness2.middleText = "You may flip 1 covered card in this line.";
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
            PromptManager.PromptAction([PromptManager.Prompt.Play], Game.instance.localPlayer.hand, protocols);
            Response response = await Game.instance.localPlayer.WaitForResponse();
            await Game.instance.localPlayer.Play(response.protocol, response.card, true);
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
