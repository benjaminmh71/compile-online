using CompileOnline.Game;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;

public partial class Draft : Control
{
    public enum DraftType { Random, Draft, BanDraft };
    public List<String> localProtocols = new List<String>();
    public List<String> oppProtocols = new List<String>();
    public List<String> bannedProtocols = new List<String>();

    HBoxContainer protocolsFirstRow;
    HBoxContainer protocolsSecondRow;
    Label promptLabel;

    bool oppResponse = false;
    int oppId;

    public async Task Init(DraftType draftType, int _oppId)
    {
        oppId = _oppId;
        protocolsFirstRow = GetNode<HBoxContainer>("ProtocolsFirstRow");
        protocolsSecondRow = GetNode<HBoxContainer>("ProtocolsSecondRow");
        promptLabel = GetNode<Label>("PromptLabel");
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
        else
        {
            Visible = true;
            List<String> protocols = Cardlist.protocols.Keys.ToList();
            for (int i = 0; i < (draftType == DraftType.Draft ? 7 : 8); i++)
            {
                String protocolString = protocols[Utility.random.RandiRange(0, protocols.Count - 1)];
                protocols.Remove(protocolString);
                PackedScene protocolScene = GD.Load("res://Game/Protocol.tscn") as PackedScene;
                Protocol protocol = protocolScene.Instantiate<Protocol>();
                protocol.info = Cardlist.protocols[protocolString];
                if (i < 4) protocolsFirstRow.AddChild(protocol);
                else protocolsSecondRow.AddChild(protocol);
            }
            String protocolNames = "";
            foreach (Protocol p in GetProtocols()) protocolNames =
                    protocolNames + (protocolNames == "" ? "" : ",") + p.info.name;
            RpcId(oppId, nameof(OppInit), protocolNames, Multiplayer.GetUniqueId());
            await WaitForOppResponse();
        }

        foreach (Protocol p in GetProtocols()) p.OnClick += PromptManager.OnProtocolClicked;

        if (draftType == DraftType.BanDraft)
        {
            promptLabel.Text = "Select a protocol to ban.";
            PromptManager.PromptAction([PromptManager.Prompt.Select], GetProtocols().
                FindAll(p => !localProtocols.Contains(p.info.name) && !oppProtocols.Contains(p.info.name)
                && !bannedProtocols.Contains(p.info.name)));
            Response banResponse = await WaitForResponse();
            banResponse.protocol.GetNode<Control>("BannedSelectionIndicator").Visible = true;
            bannedProtocols.Add(banResponse.protocol.info.name);
            RpcId(oppId, nameof(OppBanProtocol), banResponse.protocol.info.name);
            promptLabel.Text = "";

            RpcId(oppId, nameof(OppCommandBan));
            await WaitForOppResponse();
        }

        promptLabel.Text = "Select a protocol to draft.";
        PromptManager.PromptAction([PromptManager.Prompt.Select], GetProtocols().
            FindAll(p => !localProtocols.Contains(p.info.name) && !oppProtocols.Contains(p.info.name)
                && !bannedProtocols.Contains(p.info.name)));
        Response response = await WaitForResponse();
        response.protocol.GetNode<Control>("LocalSelectionIndicator").Visible = true;
        localProtocols.Add(response.protocol.info.name);
        RpcId(oppId, nameof(OppSelectProtocol), response.protocol.info.name);
        promptLabel.Text = "";

        RpcId(oppId, nameof(OppCommandSelect));
        await WaitForOppResponse();

        RpcId(oppId, nameof(OppCommandSelect));
        await WaitForOppResponse();

        promptLabel.Text = "Select a protocol to draft.";
        PromptManager.PromptAction([PromptManager.Prompt.Select], GetProtocols().
            FindAll(p => !localProtocols.Contains(p.info.name) && !oppProtocols.Contains(p.info.name)
                && !bannedProtocols.Contains(p.info.name)));
        response = await WaitForResponse();
        response.protocol.GetNode<Control>("LocalSelectionIndicator").Visible = true;
        localProtocols.Add(response.protocol.info.name);
        RpcId(oppId, nameof(OppSelectProtocol), response.protocol.info.name);

        PromptManager.PromptAction([PromptManager.Prompt.Select], GetProtocols().
            FindAll(p => !localProtocols.Contains(p.info.name) && !oppProtocols.Contains(p.info.name)
                && !bannedProtocols.Contains(p.info.name)));
        response = await WaitForResponse();
        response.protocol.GetNode<Control>("LocalSelectionIndicator").Visible = true;
        localProtocols.Add(response.protocol.info.name);
        RpcId(oppId, nameof(OppSelectProtocol), response.protocol.info.name);
        promptLabel.Text = "";

        RpcId(oppId, nameof(OppCommandSelect));
        await WaitForOppResponse();
    }

    List<Protocol> GetProtocols()
    {
        List<Protocol> protocols = new List<Protocol>();
        foreach (Protocol p in GetNode("ProtocolsFirstRow").GetChildren()) protocols.Add(p);
        foreach (Protocol p in GetNode("ProtocolsSecondRow").GetChildren()) protocols.Add(p);
        return protocols;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    void OppInit(String protocolString, int _oppId)
    {
        oppId = _oppId;
        protocolsFirstRow = GetNode<HBoxContainer>("ProtocolsFirstRow");
        protocolsSecondRow = GetNode<HBoxContainer>("ProtocolsSecondRow");
        promptLabel = GetNode<Label>("PromptLabel");
        Visible = true;
        List<String> protocolNames = protocolString.Split(',').ToList();
        for (int i = 0; i < protocolNames.Count; i++)
        {
            PackedScene protocolScene = GD.Load("res://Game/Protocol.tscn") as PackedScene;
            Protocol protocol = protocolScene.Instantiate<Protocol>();
            protocol.info = Cardlist.protocols[protocolNames[i]];
            if (i < (float)protocolNames.Count/2) protocolsFirstRow.AddChild(protocol);
            else protocolsSecondRow.AddChild(protocol);
        }
        foreach (Protocol p in GetProtocols()) p.OnClick += PromptManager.OnProtocolClicked;
        RpcId(oppId, nameof(OppResponse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    async void OppCommandSelect()
    {
        promptLabel.Text = "Select a protocol to draft.";
        PromptManager.PromptAction([PromptManager.Prompt.Select], GetProtocols().
            FindAll(p => !localProtocols.Contains(p.info.name) && !oppProtocols.Contains(p.info.name)
                && !bannedProtocols.Contains(p.info.name)));
        Response response = await WaitForResponse();
        response.protocol.GetNode<Control>("LocalSelectionIndicator").Visible = true;
        localProtocols.Add(response.protocol.info.name);
        RpcId(oppId, nameof(OppSelectProtocol), response.protocol.info.name);
        promptLabel.Text = "";

        RpcId(oppId, nameof(OppResponse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    async void OppCommandBan()
    {
        promptLabel.Text = "Select a protocol to ban.";
        PromptManager.PromptAction([PromptManager.Prompt.Select], GetProtocols().
            FindAll(p => !localProtocols.Contains(p.info.name) && !oppProtocols.Contains(p.info.name)
                && !bannedProtocols.Contains(p.info.name)));
        Response response = await WaitForResponse();
        response.protocol.GetNode<Control>("BannedSelectionIndicator").Visible = true;
        bannedProtocols.Add(response.protocol.info.name);
        RpcId(oppId, nameof(OppBanProtocol), response.protocol.info.name);
        promptLabel.Text = "";

        RpcId(oppId, nameof(OppResponse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    void OppSelectProtocol(String protocolName)
    {
        Protocol protocol = GetProtocols().Find(p => p.info.name == protocolName);
        protocol.GetNode<Control>("OppSelectionIndicator").Visible = true;
        oppProtocols.Add(protocolName);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    void OppBanProtocol(String protocolName)
    {
        Protocol protocol = GetProtocols().Find(p => p.info.name == protocolName);
        protocol.GetNode<Control>("BannedSelectionIndicator").Visible = true;
        bannedProtocols.Add(protocolName);
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

    public async Task<Response> WaitForResponse()
    {
        while (PromptManager.response == null)
        {
            await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        }
        Response response = PromptManager.response;
        PromptManager.response = null;
        return response;
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
