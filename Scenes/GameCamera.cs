using Godot;

namespace Game;

public partial class GameCamera : Camera2D
{
    private const float PAN_SPEED = 500;
    private const int TILE_SIZE = 64;

    private readonly StringName _actionPanLeft = new("pan_left");
    private readonly StringName _actionPanRight = new("pan_right");
    private readonly StringName _actionPanUp = new("pan_up");
    private readonly StringName _actionPanDown = new("pan_down");

    public override void _Process(double delta)
    {
        var movementVector = Input.GetVector(_actionPanLeft, _actionPanRight, _actionPanUp, _actionPanDown);

        GlobalPosition = GetScreenCenterPosition();
        GlobalPosition += movementVector * PAN_SPEED * (float)delta;
    }

    public void SetBoundingRect(Rect2I boundingRect)
    {
        LimitLeft = boundingRect.Position.X * TILE_SIZE;
        LimitRight = boundingRect.End.X * TILE_SIZE;
        LimitTop = boundingRect.Position.Y * TILE_SIZE;
        LimitBottom = boundingRect.End.Y * TILE_SIZE;
    }

    public void CenterOnPosition(Vector2 position)
    {
        GlobalPosition = position;
    }
}