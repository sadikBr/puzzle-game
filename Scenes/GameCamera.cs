using Godot;

namespace Game;

public partial class GameCamera : Camera2D
{
    private const float PAN_SPEED = 500;
    private const int TILE_SIZE = 64;

    private readonly StringName ACTION_PAN_LEFT = new StringName("pan_left");
    private readonly StringName ACTION_PAN_RIGHT = new StringName("pan_right");
    private readonly StringName ACTION_PAN_UP = new StringName("pan_up");
    private readonly StringName ACTION_PAN_DOWN = new StringName("pan_down");

    public override void _Process(double delta)
    {
        var movementVector = Input.GetVector(ACTION_PAN_LEFT, ACTION_PAN_RIGHT, ACTION_PAN_UP, ACTION_PAN_DOWN);

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
