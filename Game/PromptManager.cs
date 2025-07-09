using CompileOnline.Game;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class PromptManager
{
    public enum Prompt { EndAction, CustomButtonA, CustomButtonB, Play, Refresh, Compile, Control, Shift, Select };
    public static Response response = null;

    static Prompt[] currPrompts = [];
    static List<Protocol> selectableProtocols = new List<Protocol>();

    static List<int> swapA = new List<int>();
    static List<int> swapB = new List<int>();
    static bool swapLocal = false;

    public static void Init()
    {
        Game.instance.mousePosition.CardClicked += OnClick;
        Game.instance.mousePosition.CardPlaced += OnPlaced;
        Game.instance.mousePosition.ProtocolSwapped += OnSwap;
        Game.instance.endActionButton.Pressed += OnEndAction;
        Game.instance.refreshButton.Pressed += OnRefresh;
        Game.instance.resetControlButton.Pressed += OnResetControl;
        Game.instance.customButtonA.Pressed += OnCustomButtonA;
        Game.instance.customButtonB.Pressed += OnCustomButtonB;
        foreach (Protocol p in Game.instance.GetProtocols(true))
        {
            p.OnClick += OnProtocolClicked;
        }
    }

    public static void PromptAction(Prompt[] prompts)
    {
        PromptAction(prompts, new List<Card>(), new List<Protocol>());
    }

    public static void PromptAction(Prompt[] prompts, String customNameA)
    {
        Game.instance.customButtonA.Text = customNameA;
        PromptAction(prompts);
    }

    public static void PromptAction(Prompt[] prompts, String customNameA, String customNameB)
    {
        Game.instance.customButtonA.Text = customNameA;
        Game.instance.customButtonB.Text = customNameB;
        PromptAction(prompts);
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
        List<Control> leftUIElements = new List<Control>();
        currPrompts = prompts;
        MousePosition.ResetSelections();
        if (prompts.Contains(Prompt.EndAction))
        {
            leftUIElements.Add(Game.instance.endActionButton);
        }

        if (prompts.Contains(Prompt.CustomButtonA))
        {
            leftUIElements.Add(Game.instance.customButtonA);
        }

        if (prompts.Contains(Prompt.CustomButtonB))
        {
            leftUIElements.Add(Game.instance.customButtonB);
        }

        if (prompts.Contains(Prompt.Play))
        {
            MousePosition.SetSelectedCards(cards, protocols);
            leftUIElements.Add(Game.instance.flippedCheckbox);
            Game.instance.flippedCheckbox.GetNode<CheckBox>("CheckBox").ButtonPressed = false;
        }

        if (prompts.Contains(Prompt.Refresh))
        {
            leftUIElements.Add(Game.instance.refreshButton);
        }

        if (prompts.Contains(Prompt.Control))
        {
            MousePosition.SetSelectedProtocols(protocols);
            SetProtocolPrompt(protocols.ToArray());
            leftUIElements.Add(Game.instance.resetControlButton);
            leftUIElements.Add(Game.instance.endActionButton);
        }

        if (prompts.Contains(Prompt.Shift))
        {
            MousePosition.SetSelectedCards(cards, protocols);
        }

        if (prompts.Contains(Prompt.Select))
        {
            MousePosition.SetClickableCards(cards);
            selectableProtocols = protocols;
        }

        SetPrompt(leftUIElements.ToArray());
    }

    public static void SetPrompt(Control[] controls)
    {
        foreach (Node n in Game.instance.leftUI.GetChildren())
        {
            if (n.Name != "PromptLabel") (n as Control).Visible = false;
        }

        foreach (Control e in controls)
        {
            e.Visible = true;
        }
    }

    public static void SetProtocolPrompt(Protocol[] protocols)
    {
        foreach (Protocol p in Game.instance.GetProtocols())
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

    public static void OnPlaced(Protocol protocol, Card card)
    {
        if (currPrompts.Contains(Prompt.Play))
        {
            response = new Response(card, protocol, 
                Game.instance.flippedCheckbox.GetNode<CheckBox>("CheckBox").ButtonPressed, Prompt.Play);
            PromptAction([]);
        }
        else if (currPrompts.Contains(Prompt.Shift))
        {
            response = new Response(card, protocol, Prompt.Shift);
            PromptAction([]);
        }
    }

    public static void OnEndAction()
    {
        if (currPrompts.Contains(Prompt.EndAction)) {
            response = new Response(Prompt.EndAction);
            PromptAction([]);
        }
        else if (currPrompts.Contains(Prompt.Control))
        {
            SetProtocolPrompt([]);
            response = new Response(swapLocal, new List<int>(swapA), new List<int>(swapB), Prompt.Control);
            PromptAction([]);
            swapA.Clear();
            swapB.Clear();
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
            for (int i = swapA.Count-1; i >= 0; i--)
            {
                MousePosition.Swap(Game.instance.GetProtocols(swapLocal)[swapB[i]],
                   Game.instance.GetProtocols(swapLocal)[swapA[i]]);
            }
            swapA.Clear();
            swapB.Clear();
            MousePosition.SetSelectedProtocols(Game.instance.GetProtocols());
            SetProtocolPrompt(Game.instance.GetProtocols().ToArray());
        }
    }

    public static void OnCustomButtonA()
    {
        if (currPrompts.Contains(Prompt.CustomButtonA))
        {
            response = new Response(Prompt.CustomButtonA);
            PromptAction([]);
        }
    }

    public static void OnCustomButtonB()
    {
        if (currPrompts.Contains(Prompt.CustomButtonB))
        {
            response = new Response(Prompt.CustomButtonB);
            PromptAction([]);
        }
    }

    public static void OnProtocolClicked(Protocol protocol)
    {
        if (currPrompts.Contains(Prompt.Compile) && selectableProtocols.Contains(protocol))
        {
            response = new Response(protocol, Prompt.Compile);
            PromptAction([]);
        }

        if (currPrompts.Contains(Prompt.Select) && selectableProtocols.Contains(protocol))
        {
            response = new Response(protocol, Prompt.Select);
            PromptAction([]);
        }
    }

    public static void OnSwap(Protocol protocolA, Protocol protocolB)
    {
        if (currPrompts.Contains(Prompt.Control))
        {
            List<Protocol> protocols = Game.instance.GetProtocols(Game.instance.IsLocal(protocolA));
            MousePosition.SetSelectedProtocols(protocols);
            SetProtocolPrompt(protocols.ToArray());
            swapA.Add(Game.instance.IndexOfProtocol(protocolB));
            swapB.Add(Game.instance.IndexOfProtocol(protocolA));
            swapLocal = Game.instance.IsLocal(protocolA);
        }
    }
}
