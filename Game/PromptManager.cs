using CompileOnline.Game;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class PromptManager
{
    public enum Prompt { Play, Refresh, Compile };
    public static Response response = null;

    static Prompt[] currPrompts = [];

    public static void Init()
    {
        Game.instance.mousePosition.CardPlaced += OnPlay;
        Game.instance.refreshButton.Pressed += OnRefresh;
    }

    public static void PromptAction(Prompt[] prompts)
    {
        PromptAction(prompts, null);
    }

    public static void PromptAction(Prompt[] prompts, List<Card> cards)
    {
        List<Button> buttons = new List<Button>();
        currPrompts = prompts;
        if (prompts.Contains(Prompt.Play))
        {
            MousePosition.SetSelectedCards(cards);
        }

        if (prompts.Contains(Prompt.Refresh))
        {
            buttons.Add(Game.instance.refreshButton);
        }

        SetPrompt(buttons.ToArray());
    }

    public static void SetPrompt(Button[] buttons)
    {
        foreach (Node n in Game.instance.leftUI.GetChildren())
        {
            if (n is Button) (n as Button).Visible = false;
        }

        foreach (Button b in buttons)
        {
            b.Visible = true;
        }
    }

    public static void OnPlay(Protocol protocol, Card card)
    {
        if (currPrompts.Contains(Prompt.Play))
        {
            response = new Response(card, protocol, Prompt.Play);
            PromptAction([]);
        }
    }

    public static void OnRefresh()
    {
        if (currPrompts.Contains(Prompt.Refresh))
        {
            response = new Response(Prompt.Refresh);
            PromptAction([]);
        }
    }
}
