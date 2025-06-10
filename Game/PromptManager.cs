using CompileOnline.Game;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class PromptManager
{
    public enum Prompt { Play, Refresh, Compile, Control, Select };
    public static Response response = null;

    static Prompt[] currPrompts = [];
    static List<Protocol> selectableProtocols = new List<Protocol>();

    public static void Init()
    {
        Game.instance.mousePosition.CardClicked += OnClick;
        Game.instance.mousePosition.CardPlaced += OnPlay;
        Game.instance.mousePosition.ProtocolSwapped += OnSwap;
        Game.instance.endActionButton.Pressed += OnEndAction;
        Game.instance.refreshButton.Pressed += OnRefresh;
        Game.instance.resetControlButton.Pressed += OnResetControl;
        foreach (Protocol p in Game.instance.GetProtocols(true))
        {
            p.OnClick += OnCompile;
        }
    }

    public static void PromptAction(Prompt[] prompts)
    {
        PromptAction(prompts, new List<Card>(), new List<Protocol>());
    }

    public static void PromptAction(Prompt[] prompts, List<Card> cards)
    {
        PromptAction(prompts, cards, new List<Protocol>());
    }

    public static void PromptAction(Prompt[] prompts, List<Protocol> protocols)
    {
        PromptAction(prompts, new List<Card>(), protocols);
    }

    public static void PromptAction(Prompt[] prompts, List<Card> cards, List<Protocol> protocols)
    {
        List<Button> buttons = new List<Button>();
        currPrompts = prompts;
        MousePosition.ResetSelections();
        if (prompts.Contains(Prompt.Play))
        {
            MousePosition.SetSelectedCards(cards);
        }

        if (prompts.Contains(Prompt.Refresh))
        {
            buttons.Add(Game.instance.refreshButton);
        }

        if (prompts.Contains(Prompt.Control))
        {
            MousePosition.SetSelectedProtocols(protocols);
        }

        if (prompts.Contains(Prompt.Select))
        {
            MousePosition.SetClickableCards(cards);
        }

        SetPrompt(buttons.ToArray());
        SetPrompt(protocols.ToArray());
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

    public static void SetPrompt(Protocol[] protocols)
    {
        foreach (Protocol p in Game.instance.GetProtocols(true))
        {
            p.GetNode<Control>("SelectionIndicator").Visible = false;
        }

        foreach (Protocol p in protocols)
        {
            p.GetNode<Control>("SelectionIndicator").Visible = true;
        }
    }

    public static void OnClick(Card card)
    {
        if (currPrompts.Contains(Prompt.Select))
        {
            response = new Response(card, Prompt.Select);
            PromptAction([]);
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

    public static void OnEndAction()
    {
        if (currPrompts.Contains(Prompt.Control))
        {
            response = new Response(Prompt.Control);
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

    public static void OnResetControl()
    {
        if (currPrompts.Contains(Prompt.Control))
        {
            GD.Print("Finish this later");
        }
    }

    public static void OnCompile(Protocol protocol)
    {
        if (currPrompts.Contains(Prompt.Compile) && selectableProtocols.Contains(protocol))
        {
            response = new Response(protocol, Prompt.Compile);
            PromptAction([]);
        }
    }

    public static void OnSwap(Protocol protocol)
    {
        if (currPrompts.Contains(Prompt.Control))
        {
            if (Game.instance.IsLocal(protocol))
            {
                MousePosition.SetSelectedProtocols(Game.instance.GetProtocols(true));
            } 
            else
            {
                MousePosition.SetSelectedProtocols(Game.instance.GetProtocols(false));
            }
        }
    }
}
