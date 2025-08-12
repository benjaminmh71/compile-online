using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

public partial class Draft : Control
{
    public enum DraftType { Random, Draft, BanDraft };
    public List<String> localProtocols = new List<String>();
    public List<String> oppProtocols = new List<String>();

    bool oppResponse = false;

    public async Task Init(DraftType draftType, long oppId)
    {
        if (draftType == DraftType.Random)
        {
            List<String> protocols = Cardlist.protocols.Keys.ToList();
            for (int i = 0; i < 3; i++)
            {
                String protocol = protocols[Utility.random.RandiRange(0, protocols.Count - 1)];
                protocols.Remove(protocol);
                localProtocols.Add(protocol);
            }
            for (int i = 0; i < 3; i++)
            {
                String protocol = protocols[Utility.random.RandiRange(0, protocols.Count - 1)];
                protocols.Remove(protocol);
                oppProtocols.Add(protocol);
            }
            RpcId(oppId, nameof(SetProtocols), localProtocols.Aggregate((a, s) => a + "," + s),
                oppProtocols.Aggregate((a, s) => a + "," + s));
        }
    }

    List<Protocol> GetProtocols()
    {
        return GetNode("ProtocolsFirstRow").GetChildren().Cast<Protocol>()
            .Concat(GetNode("ProtocolsSecondRow").GetChildren().Cast<Protocol>()).ToList();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    void SetProtocols(String localProtocolsString, String oppProtocolsString)
    {
        localProtocols = oppProtocolsString.Split(",").ToList(); 
        oppProtocols = localProtocolsString.Split(",").ToList();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void OppResponse()
    {
        oppResponse = true;
    }

    public async Task WaitForOppResponse()
    {
        while (!oppResponse)
        {
            await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        }
        oppResponse = false;
    }

    public async Task WaitForDraft()
    {
        while (localProtocols.Count < 3 || oppProtocols.Count < 3)
        {
            await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        }
    }
}
