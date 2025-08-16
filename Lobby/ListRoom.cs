using Godot;
using System;

public partial class ListRoom : Control
{
    public int creatorId;

    public Label nameLabel;
    public Label draftTypeLabel;
    public Label passwordLabel;
    public Button joinButton;

    public override void _Ready()
    {
        VBoxContainer vBox = GetNode<VBoxContainer>("VBoxContainer");
        nameLabel = vBox.GetNode<HBoxContainer>("HBoxContainer").GetNode<Label>("NameLabel");
        draftTypeLabel = vBox.GetNode<Label>("DraftTypeLabel");
        passwordLabel = vBox.GetNode<Label>("PasswordLabel");
        joinButton = vBox.GetNode<HBoxContainer>("HBoxContainer").GetNode<Button>("JoinButton");
    }
}
