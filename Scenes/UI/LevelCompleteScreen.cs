using Game.Autoloads;

using Godot;

namespace Game.UI;

public partial class LevelCompleteScreen : CanvasLayer
{
    private Button _nextLevelButton;

    public override void _Ready()
    {
        _nextLevelButton = GetNode<Button>("%NextLevelButton");

        _nextLevelButton.Pressed += OnNextLevelButtonPressed;
    }

    private void OnNextLevelButtonPressed()
    {
        LevelManager.Instance.ChangeToNextLevel();
    }
}