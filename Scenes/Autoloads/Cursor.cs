using Godot;

namespace Game.Autoloads;

public partial class Cursor : CanvasLayer
{

    private Sprite2D _cursorSprite;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        _cursorSprite = GetNode<Sprite2D>("Sprite2D");
    }

    public override void _Process(double delta)
    {
        _cursorSprite.GlobalPosition = _cursorSprite.GetGlobalMousePosition();
    }
}
