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
        protocols["Apathy"] = apathy;
        CardInfo apathy0 = new CardInfo("Apathy", 0);
        apathy0.topText = "Your total value in this line is increased by 1 for each" +
            "face-down card in this line.";
        apathy.cards.Add(apathy0);

        CardInfo apathy1 = new CardInfo("Apathy", 1);
        apathy1.middleText = "Flip all other face-up cards in this line.";
        apathy.cards.Add(apathy1);

        CardInfo apathy2 = new CardInfo("Apathy", 2);
        apathy2.topText = "Ignore all middle commands in this line.";
        apathy2.bottomText = "When this card would be covered: first, flip this card.";
        apathy.cards.Add(apathy2);

        CardInfo apathy3 = new CardInfo("Apathy", 3);
        apathy3.middleText = "Flip 1 of your opponent's face-up cards.";
        apathy3.OnPlay = async () =>
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
        apathy4.OnPlay = async () =>
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
        apathy5.OnPlay = async () =>
        {
            if (Game.instance.localPlayer.hand.Count > 0)
            {
                await Game.instance.localPlayer.Discard(1);
            }
        };
        apathy.cards.Add(apathy5);
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
