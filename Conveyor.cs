using Godot;
using System;
using System.Collections.Generic;
using Kesa.Conveyors;

public enum ConveyorDirection
{
    Up,
    Down,
    Left,
    Right,
}

public class Conveyor : Node2D
{
    private ConveyorDirection _direction;

    private HashSet<PushableEntity> _entities;

    public ConveyorDirection Direction
    {
        get => _direction;
        set
        {
            _direction = value;

            const float singleTurn = (float) Math.PI / 2;

            Rotation = _direction switch
            {
                ConveyorDirection.Up => 0,
                ConveyorDirection.Right => singleTurn * 1,
                ConveyorDirection.Down => singleTurn * 2,
                ConveyorDirection.Left => singleTurn * 3,
            };
        }
    }

    public AnimatedSprite Sprite { get; private set; }

    public Area2D Area { get; private set; }

    public override void _Ready()
    {
        _entities = new HashSet<PushableEntity>();

        Sprite = GetNode<AnimatedSprite>("Sprite");
        Area = GetNode<Area2D>("Area");

        SetProcess(true);
    }

    public override void _Process(float delta)
    {
        foreach (var entity in _entities)
        {
            if (entity.Conveyor == null)
            {
                entity.Conveyor = this;
                entity.Label.Text = "Owned By: " + Name;
            }

            if (entity.Conveyor == this)
            {
                var lane1Projection = GetNode<Line2D>("Lane1").Project(entity.GlobalPosition);
                var lane2Projection = GetNode<Line2D>("Lane2").Project(entity.GlobalPosition);
                var endProjection = GetNode<Line2D>("End").Project(entity.GlobalPosition);
                var directionToMove = ((entity.ConveyorLane == 1 ? lane1Projection : lane2Projection).Difference * 10) +
                                      endProjection.Difference;
                entity.Position += directionToMove.Normalized();
            }
        }
    }

    public void OnAreaEntered(Area2D body)
    {
        if (body.Owner is PushableEntity entity)
        {
            _entities.Add(entity);

            entity.Conveyor = this;
            entity.Label.Text = "Owned By: " + Name;

            var lane1Projection = GetNode<Line2D>("Lane1").Project(entity.GlobalPosition);
            var lane2Projection = GetNode<Line2D>("Lane2").Project(entity.GlobalPosition);
        }
    }

    public void OnAreaExited(Area2D body)
    {
        if (body.Owner is PushableEntity entity)
        {
            _entities.Remove(entity);

            if (entity.Conveyor == this)
            {
                entity.Conveyor = null;
                entity.Label.Text = "Unowned";
            }
        }
    }
}