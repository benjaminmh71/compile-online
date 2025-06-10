using Godot;
using System;
using System.Threading.Tasks;

public partial class CardInfo : Node
{
    public String protocol = "Apathy";
    public int value = 5;
    public String topText = "";
    public String middleText = "";
    public String bottomText = "";

    public Card card = null;

    public Func<Task> OnPlay = async () => { GD.Print("Here"); };

    public CardInfo(String protocolName, int _value)
    {
        protocol = protocolName;
        value = _value;
    }
    
    public String GetCardName()
    {
        return protocol + " " + value;
    }
}
