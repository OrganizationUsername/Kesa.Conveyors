using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;
using Kesa.Conveyors;

public class World : Node2D
{
    private PackedScene _conveyorScene;
    private PackedScene _boxScene;
    private bool _leftDown;
    private Conveyor _lastPlaced;
    private ConveyorDirection? _lastPlacedDirection;

    private float _delta;
    private Camera2D _camera;
    private bool _midDown;

    //TODO: Make these records
    private List<(ButtonList Button, Action MousePress, Action MouseRelease, Action<bool> MouseChange)> _mouseActions;
    private List<(KeyList Key, Action KeyPress, Action KeyRelease, Action<bool> KeyChange)> _keyActions;

    private System.Collections.Generic.Dictionary<(int X, int Y), Conveyor> _conveyors;

    public override void _Ready()
    {
        _conveyorScene = (PackedScene) ResourceLoader.Load("res://Conveyor.tscn");
        _boxScene = (PackedScene) ResourceLoader.Load("res://PushableEntity.tscn");

        _camera = GetNode<Camera2D>("Camera");

        _conveyors = new System.Collections.Generic.Dictionary<(int X, int Y), Conveyor>();
        _mouseActions = new List<(ButtonList Button, Action MousePress, Action MouseRelease, Action<bool> MouseChange)>();
        _keyActions = new List<(KeyList Key, Action KeyPress, Action KeyRelease, Action<bool> KeyChange)>();

        Register(ButtonList.WheelUp, () => _camera.Zoom *= new Vector2(0.9f, 0.9f));
        Register(ButtonList.WheelDown, () => _camera.Zoom *= new Vector2(1.1f, 1.1f));
        Register(ButtonList.Middle, p => _midDown = p);
        Register(ButtonList.Left, OnLeftMousePressed, OnLeftMouseReleased);
        Register(ButtonList.Right, OnRightMousePressed);

        Register(KeyList.I, () =>
        {
            var inst = (PushableEntity) _boxScene.Instance();
            inst.Position = _camera.GetGlobalMousePosition();
            inst.ConveyorLane = GD.Randi() % 2 == 0 ? 1 : 2;
            AddChild(inst);
        });

        SetProcess(true);
    }

    private void OnLeftMousePressed()
    {
        _leftDown = true;
    }

    private void OnLeftMouseReleased()
    {
        _leftDown = false;
        _lastPlaced = null;
        _lastPlacedDirection = null;
    }

    private void OnRightMousePressed()
    {
        foreach (var node in FindByCollider<Conveyor>(maxResults: 1))
        {
            node.Direction = node.Direction switch
            {
                ConveyorDirection.Up => ConveyorDirection.Right,
                ConveyorDirection.Down => ConveyorDirection.Left,
                ConveyorDirection.Left => ConveyorDirection.Up,
                ConveyorDirection.Right => ConveyorDirection.Down,
            };
        }
    }

    private void Register(ButtonList button, Action<bool> change)
    {
        _mouseActions.Add((button, null, null, change));
    }

    private void Register(ButtonList button, Action press, Action release = null)
    {
        _mouseActions.Add((button, press, release, null));
    }

    private void Register(KeyList key, Action<bool> change)
    {
        _keyActions.Add((key, null, null, change));
    }

    private void Register(KeyList key, Action press, Action release = null)
    {
        _keyActions.Add((key, press, release, null));
    }

    public override void _Process(float delta)
    {
        delta *= 1000;

        _delta += delta;

        var animationStep = 1000f / 30;

        while (_delta > animationStep)
        {
            _delta -= animationStep;

            Conveyor first = null;
            AnimatedSprite firstSprite = null;

            //Keep all animation frames in sync.
            foreach (var conv in _conveyors.Values)
            {
                if (first == null)
                {
                    first = conv;
                    firstSprite = first.Sprite;
                    firstSprite.Frame = (firstSprite.Frame + 1) % 7;
                    continue;
                }

                var secondSprite = conv.Sprite;
                secondSprite.Frame = firstSprite.Frame;
            }
        }
    }

    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton mouseInput)
        {
            //TODO: Make class for handling different input types.
            foreach (var actionInfo in _mouseActions)
            {
                if ((int) actionInfo.Button == mouseInput.ButtonIndex)
                {
                    var action = mouseInput.Pressed
                        ? actionInfo.MousePress
                        : actionInfo.MouseRelease;
                    action?.Invoke();
                    actionInfo.MouseChange?.Invoke(mouseInput.Pressed);
                }
            }
        }
        else if (input is InputEventKey keyInput)
        {
            foreach (var actionInfo in _keyActions)
            {
                if ((uint) actionInfo.Key == keyInput.Scancode)
                {
                    var action = keyInput.Pressed
                        ? actionInfo.KeyPress
                        : actionInfo.KeyRelease;
                    action?.Invoke();
                    actionInfo.KeyChange?.Invoke(keyInput.Pressed);
                }
            }
        }
        else if (input is InputEventMouseMotion mouseMoveInput)
        {
            var mousePos = _camera.GetGlobalMousePosition().Align();
            var mousePosInt = ((int) mousePos.x, (int) mousePos.y);
            
            if (_leftDown && !_conveyors.ContainsKey(mousePosInt))
            {
                var delta = mouseMoveInput.Relative;
                var absX = Math.Abs(delta.x);
                var absY = Math.Abs(delta.y);
                var dir = absX > absY
                    ? delta.x > 0 ? ConveyorDirection.Right : ConveyorDirection.Left
                    : delta.y > 0
                        ? ConveyorDirection.Down
                        : ConveyorDirection.Up;

                var inst = (Conveyor) _conveyorScene.Instance();
                inst.Position = _camera.GetGlobalMousePosition().Align();
                inst.Direction = dir;
                _conveyors[mousePosInt] = inst;

                if (_lastPlacedDirection != null && _lastPlacedDirection != dir)
                {
                    _lastPlaced.Direction = dir;
                }

                _lastPlaced = inst;
                _lastPlacedDirection = dir;

                AddChild(inst);
            }

            if (_midDown)
            {
                _camera.Position += mouseMoveInput.Relative * -(Vector2.One / _camera.Zoom);
            }
        }
    }

    public IEnumerable<T> FindByCollider<T>(int maxResults = 32) => FindByCollider<T>(_camera.GetGlobalMousePosition(), maxResults);

    public IEnumerable<T> FindByCollider<T>(Vector2 pos, int maxResults = 32)
    {
        var nodes = GetWorld2d().DirectSpaceState.IntersectPoint(pos, maxResults, collideWithAreas: true);

        foreach (Dictionary dict in nodes)
        {
            if (dict["collider"] is CollisionObject2D collider && collider.Owner is T node)
            {
                yield return node;
            }
        }
    }
}