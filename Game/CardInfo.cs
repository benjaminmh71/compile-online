using Godot;
using System;
using System.Threading.Tasks;

public partial class CardInfo : Node
{
    public enum Passive { NoMiddleCommands, PlusOneForFaceDown, FaceDownFours, NoFaceDown };

    public String protocol = "Apathy";
    public int value = 5;
    public String topText = "";
    public String middleText = "";
    public String bottomText = "";
    public Passive[] passives = new Passive[0];

# pragma warning disable CS1998 // Warning for async without await
    public Func<Card, Task> OnPlay = async (Card card) => { };
    public Func<Card, Task> OnCover = async (Card card) => { };
    public Func<Card, Task> OnFlip = async (Card card) => { };
    public Func<Card, Task> OnStart = async (Card card) => { };
    public Func<Card, Task> OnEnd = async (Card card) => { };
    public Func<Card, Task> OnDelete = async (Card card) => { };
# pragma warning restore CS1998

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
