using Godot;

namespace Game.Buildings;

public partial class BuildingGhost : Node2D
{
    private Node2D _topLeft;
    private Node2D _topRight;
    private Node2D _bottomLeft;
    private Node2D _bottomRight;
    private Node2D _upDownRoot;
    private Node2D _spriteRoot;

    private Tween _spriteTween;

    public override void _Ready()
    {
        _topLeft = GetNode<Node2D>("TopLeft");
        _bottomLeft = GetNode<Node2D>("BottomLeft");
        _topRight = GetNode<Node2D>("TopRight");
        _bottomRight = GetNode<Node2D>("BottomRight");
        _upDownRoot = GetNode<Node2D>("%UpDownRoot");
        _spriteRoot = GetNode<Node2D>("SpriteRoot");

        var upDownTween = CreateTween();
        upDownTween.SetLoops(0);
        upDownTween.TweenProperty(_upDownRoot, "position", Vector2.Down * 6, .2f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Quad);
        upDownTween.TweenProperty(_upDownRoot, "position", Vector2.Up * 6, .2f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Quad);
    }

    public void SetInvalid()
    {
        Modulate = Colors.Red;
        _upDownRoot.Modulate = Modulate;
    }

    public void SetValid()
    {
        Modulate = Colors.White;
        _upDownRoot.Modulate = Modulate;
    }

    public void SetDimensions(Vector2I dimensions)
    {
        _bottomLeft.Position = dimensions * new Vector2I(0, 64);
        _topRight.Position = dimensions * new Vector2I(64, 0);
        _bottomRight.Position = dimensions * new Vector2I(64, 64);
    }

    public void AddSpriteNode(Node2D spriteNode)
    {
        _upDownRoot.AddChild(spriteNode);
    }

    public void DoHoverAnimation()
    {
        if (_spriteTween != null && _spriteTween.IsValid())
        {
            _spriteTween.Kill();
        }

        _spriteTween = CreateTween();
        _spriteTween.TweenProperty(_spriteRoot, "global_position", GlobalPosition, .3f)
            .SetTrans(Tween.TransitionType.Bounce)
            .SetEase(Tween.EaseType.Out);
    }
}