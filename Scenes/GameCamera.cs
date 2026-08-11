using Godot;

namespace Game;

public partial class GameCamera : Camera2D
{
    private const float PAN_SPEED = 500;
    private const int TILE_SIZE = 64;
    private const float NOISE_SAMPLE_GROWTH = 0.1f;
    private const float MAX_CAMERA_OFFSET = 24f;
    private const float NOISE_FREQUENCY_MULTIPLIER = 100f;
    private const float SHAKE_DECAY = 3.0f;

    private readonly StringName _actionPanLeft = new("pan_left");
    private readonly StringName _actionPanRight = new("pan_right");
    private readonly StringName _actionPanUp = new("pan_up");
    private readonly StringName _actionPanDown = new("pan_down");

    [Export] private FastNoiseLite _shakeNoise;

    private static GameCamera s_instance;

    private Vector2 _noiseSample;
    private float _currentShakePercentage;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
        {
            s_instance = this;
        }
    }

    public override void _Process(double delta)
    {
        var movementVector = Input.GetVector(_actionPanLeft, _actionPanRight, _actionPanUp, _actionPanDown);
        GlobalPosition += movementVector * PAN_SPEED * (float)delta;

        var viewportRect = GetViewportRect();
        var viewportHalfSize = viewportRect.Size / 2;

        var xClamped = Mathf.Clamp(GlobalPosition.X, LimitLeft + viewportHalfSize.X, LimitRight - viewportHalfSize.X);
        var yClamped = Mathf.Clamp(GlobalPosition.Y, LimitTop + viewportHalfSize.Y, LimitBottom - viewportHalfSize.Y);

        GlobalPosition = new Vector2(xClamped, yClamped);

        ApplyCameraShake(delta);
    }

    public static void Shake()
    {
        s_instance._currentShakePercentage = 1.0f;
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

    private void ApplyCameraShake(double delta)
    {
        if (_currentShakePercentage > 0)
        {
            _noiseSample.X += NOISE_SAMPLE_GROWTH * NOISE_FREQUENCY_MULTIPLIER * (float)delta;
            _noiseSample.Y += NOISE_SAMPLE_GROWTH * NOISE_FREQUENCY_MULTIPLIER * (float)delta;

            _currentShakePercentage = Mathf.Clamp(_currentShakePercentage - (SHAKE_DECAY * (float)delta), 0, 1);
        }

        var xSample = _shakeNoise.GetNoise2D(_noiseSample.X, 0);
        var ySample = _shakeNoise.GetNoise2D(0, _noiseSample.Y);

        Offset = new Vector2(xSample, ySample) * MAX_CAMERA_OFFSET * _currentShakePercentage;
    }
}