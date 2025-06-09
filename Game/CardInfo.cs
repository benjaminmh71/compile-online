using Godot;
using System;

public partial class CardInfo : Node
{
    public String protocol = "Apathy";
    public int value = 5;
    public String topText = "";
    public String middleText = "";
    public String bottomText = "";

    public Card card = null;

    public CardInfo(String protocolName, int _value)
    {
        protocol = protocolName;
        value = _value;
    }
}
