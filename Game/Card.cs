using CompileOnline.Game;
using Godot;
using System;

public partial class Card : Control
{
    public CardInfo info;
    public bool flipped = false;
    public bool placeholder = false;
    public bool covered = false;

    public override void _Ready()
    {
        Render();
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
    }

    public void Render()
    {
        ProtocolInfo protocolInfo = Cardlist.protocols[info.protocol];
        GetNode<Panel>("Background").SelfModulate = protocolInfo.backgroundColor;

        if (placeholder)
        {
            GetNode<Label>("Name").Visible = false;
            GetNode<Label>("TopText").Visible = false;
            GetNode<Label>("MiddleText").Visible = false;
            GetNode<Label>("BottomText").Visible = false;
            GetNode<Panel>("Background").Visible = false;
        } 
        else
        {
            GetNode<Label>("Name").Visible = true;
            GetNode<Label>("TopText").Visible = true;
            GetNode<Label>("MiddleText").Visible = true;
            GetNode<Label>("BottomText").Visible = true;
            GetNode<Panel>("Background").Visible = true;
        }

        if (flipped)
        {
            GetNode<Label>("Name").Text = "2";
            GetNode<Label>("TopText").Text = "";
            GetNode<Label>("MiddleText").Text = "";
            GetNode<Label>("BottomText").Text = "";
        }
        else
        {
            GetNode<Label>("Name").Text = info.GetCardName();
            GetNode<Label>("TopText").Text = info.topText;
            GetNode<Label>("MiddleText").Text = info.middleText;
            GetNode<Label>("BottomText").Text = info.bottomText;
        }
    }

    public void SetCardInfo(CardInfo _info)
    {
        info = _info;
    }

    public int GetValue()
    {
        if (flipped)
        {
            Protocol protocol = Game.instance.GetProtocolOfCard(this);
            if (protocol != null && Game.instance.localPlayer.StackContainsPassive
                (Game.instance.IsLocal(this), protocol, CardInfo.Passive.FaceDownFours)) return 4;
            return 2;
        }
        return info.value;
    }

    public void Reset()
    {
        flipped = false;
        placeholder = false;
        covered = false;
    }
}
