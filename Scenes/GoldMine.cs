using Godot;

namespace Game;

public partial class GoldMine : Node2D
{
    [Export]
    private Texture2D _activeTexture;

    private Sprite2D _sprite;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
    }

    public void SetActive()
    {
        _sprite.Texture = _activeTexture;
    }
}