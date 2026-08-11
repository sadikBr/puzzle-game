using System.Linq;

using Godot;

namespace Game.Components;

public partial class BuildingAnimatorComponent : Node2D
{
    [Signal]
    public delegate void DestroyAnimationFinishedEventHandler();

    [Export]
    private PackedScene _impactParticleScene;
    [Export]
    private PackedScene _destroyParticleScene;
    [Export]
    private Texture2D _maskTexture;

    private Tween _activeTween;
    private Node2D _animationRootNode;
    private Sprite2D _maskNode;

    public override void _Ready()
    {
        YSortEnabled = false;
        SetupNodes();
    }

    public void PlayInAnimation()
    {
        if (_animationRootNode == null) return;

        if (_activeTween != null && _activeTween.IsValid())
        {
            _activeTween.Kill();
        }

        _activeTween = CreateTween();
        _activeTween
            .TweenProperty(_animationRootNode, "position", Vector2.Zero, .3f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .From(Vector2.Up * 128);
        _activeTween.TweenCallback(Callable.From(() =>
        {
            var impactParticles = _impactParticleScene.Instantiate<Node2D>();
            Owner.GetParent().AddChild(impactParticles);
            impactParticles.GlobalPosition = GlobalPosition;
            GameCamera.Shake();
        }));
        _activeTween
            .TweenProperty(_animationRootNode, "position", Vector2.Up * 16, .1f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _activeTween
            .TweenProperty(_animationRootNode, "position", Vector2.Zero, .1f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }

    public void PlayOutAnimation()
    {
        if (_animationRootNode == null) return;

        if (_activeTween != null && _activeTween.IsValid())
        {
            _activeTween.Kill();
        }

        _animationRootNode.Position = Vector2.Zero;

        _maskNode.ClipChildren = ClipChildrenMode.Only;
        _maskNode.Texture = _maskTexture;

        var destroyParticles = _destroyParticleScene.Instantiate<Node2D>();
        Owner.GetParent().AddChild(destroyParticles);
        destroyParticles.GlobalPosition = GlobalPosition;

        GameCamera.Shake();

        _activeTween = CreateTween();
        _activeTween.TweenProperty(_animationRootNode, "rotation_degrees", -5, .1f);
        _activeTween.TweenProperty(_animationRootNode, "rotation_degrees", 5, .1f);
        _activeTween.TweenProperty(_animationRootNode, "rotation_degrees", -2, .1f);
        _activeTween.TweenProperty(_animationRootNode, "rotation_degrees", 2, .1f);
        _activeTween.TweenProperty(_animationRootNode, "rotation_degrees", 0, .1f);

        _activeTween.TweenProperty(_animationRootNode, "position", Vector2.Down * 300, .4f)
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.In);
        _activeTween.Finished += () => EmitSignal(SignalName.DestroyAnimationFinished);
    }

    private void SetupNodes()
    {
        if (GetChildren().FirstOrDefault() is not Node2D spriteNode)
        {
            return;
        }

        RemoveChild(spriteNode);
        Position = new Vector2(spriteNode.Position.X, spriteNode.Position.Y);

        _maskNode = new Sprite2D
        {
            Centered = false,
            Offset = new Vector2(-160, -256),
        };
        AddChild(_maskNode);

        _animationRootNode = new Node2D();
        _maskNode.AddChild(_animationRootNode);

        _animationRootNode.AddChild(spriteNode);
        spriteNode.Position = Vector2.Zero;
    }
}