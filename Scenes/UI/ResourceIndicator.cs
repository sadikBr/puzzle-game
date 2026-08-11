using Godot;

namespace Game.UI;

public partial class ResourceIndicator : Node2D
{

    private AnimatedSprite2D _animatedSprite2D;

    private Tween _activeTween;

    public override void _Ready()
    {
        _animatedSprite2D = GetNode<AnimatedSprite2D>("%AnimatedSprite2D");

        var duration = GD.RandRange(0.5f, 0.55f);

        _activeTween = CreateTween();
        _activeTween.SetLoops();
        _activeTween.TweenProperty(_animatedSprite2D, "position", Vector2.Up * 4, duration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
        _activeTween.TweenProperty(_animatedSprite2D, "position", Vector2.Down * 4, duration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
    }

    public void Destroy()
    {
        if (_activeTween != null && _activeTween.IsValid())
        {
            _activeTween.Kill();
        }

        _activeTween = CreateTween();
        _activeTween.SetParallel();
        _activeTween.TweenInterval(GD.RandRange(0.1f, 0.3f));
        _activeTween.Chain();
        _activeTween.TweenProperty(_animatedSprite2D, "scale", Vector2.Zero, 0.3f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);
        _activeTween.TweenProperty(_animatedSprite2D, "position", Vector2.Up * 32, 0.3f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        _activeTween.Chain();
        _activeTween.TweenCallback(Callable.From(QueueFree));
    }

}
