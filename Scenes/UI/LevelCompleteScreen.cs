using Game.Autoloads;

using Godot;

namespace Game.UI;

public partial class LevelCompleteScreen : CanvasLayer
{
    [Export(PropertyHint.File, "*.tscn")] private string _mainMenuScenePath;

    private Button _nextLevelButton;

    public override void _Ready()
    {
        _nextLevelButton = GetNode<Button>("%NextLevelButton");

        if (LevelManager.IsLastLevel())
        {
            _nextLevelButton.Text = "Return to Menu";
        }

        _nextLevelButton.Pressed += OnNextLevelButtonPressed;
    }

    private void OnNextLevelButtonPressed()
    {
        if (LevelManager.IsLastLevel())
        {
            GetTree().ChangeSceneToFile(_mainMenuScenePath);
            return;
        }

        LevelManager.ChangeToNextLevel();
    }
}