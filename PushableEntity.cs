using Godot;
using System;

public class PushableEntity : Node2D
{
    public Area2D Area { get; private set; }

    public Label Label { get; private set; }

    public Conveyor Conveyor { get; set; }

    public int ConveyorLane { get; set; }

    public override void _Ready()
    {
        Area = GetNode<Area2D>("Area");
        Label = GetNode<Label>("Label");
    }
}