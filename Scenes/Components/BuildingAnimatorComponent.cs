using Godot;
using System.Linq;

namespace Game.Components;

public partial class BuildingAnimatorComponent : Node2D
{
    [Signal]
    public delegate void DestroyAnimationFinishedEventHandler();

    [Export]
    private PackedScene impactParticleScene;
    [Export]
    private PackedScene destroyParticleScene;
    [Export]
    private Texture2D maskTexture;

    private Tween activeTween;
    private Node2D animationRootNode;
    private Sprite2D maskNode;

    public override void _Ready()
    {
        YSortEnabled = false;
        SetupNodes();
    }

    public void PlayInAnimation()
    {
        if (animationRootNode == null) return;

        if (activeTween != null && activeTween.IsValid())
        {
            activeTween.Kill();
        }

        activeTween = CreateTween();
        activeTween
            .TweenProperty(animationRootNode, "position", Vector2.Zero, .3f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .From(Vector2.Up * 128);
        activeTween.TweenCallback(Callable.From(() =>
        {
            var impactParticles = impactParticleScene.Instantiate<Node2D>();
            Owner.GetParent().AddChild(impactParticles);
            impactParticles.GlobalPosition = GlobalPosition;
        }));
        activeTween
            .TweenProperty(animationRootNode, "position", Vector2.Up * 16, .1f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        activeTween
            .TweenProperty(animationRootNode, "position", Vector2.Zero, .1f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }

    public void PlayOutAnimation()
    {
        if (animationRootNode == null) return;

        if (activeTween != null && activeTween.IsValid())
        {
            activeTween.Kill();
        }

        animationRootNode.Position = Vector2.Zero;

        maskNode.ClipChildren = ClipChildrenMode.Only;
        maskNode.Texture = maskTexture;

        var destroyParticles = destroyParticleScene.Instantiate<Node2D>();
        Owner.GetParent().AddChild(destroyParticles);
        destroyParticles.GlobalPosition = GlobalPosition;

        activeTween = CreateTween();
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", -5, .1f);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 5, .1f);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", -2, .1f);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 2, .1f);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 0, .1f);

        activeTween.TweenProperty(animationRootNode, "position", Vector2.Down * 300, .4f)
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.In);
        activeTween.Finished += () => EmitSignal(SignalName.DestroyAnimationFinished);
    }

    private void SetupNodes()
    {
        var spriteNode = GetChildren().FirstOrDefault() as Node2D;
        if (spriteNode == null)
        {
            return;
        }

        RemoveChild(spriteNode);
        Position = new Vector2(spriteNode.Position.X, spriteNode.Position.Y);

        maskNode = new Sprite2D
        {
            Centered = false,
            Offset = new Vector2(-160, -256),
        };
        AddChild(maskNode);

        animationRootNode = new Node2D();
        maskNode.AddChild(animationRootNode);

        animationRootNode.AddChild(spriteNode);
        spriteNode.Position = Vector2.Zero;
    }
}
