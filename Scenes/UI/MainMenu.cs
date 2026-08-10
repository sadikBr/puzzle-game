using Godot;

namespace Game.UI;

public partial class MainMenu : Node
{
    private Button _playButton;
    private Button _optionsButton;
    private Button _quitButton;

    private Control _mainMenuContainer;
    private LevelSelectScreen _levelSelectScreen;

    public override void _Ready()
    {
        _playButton = GetNode<Button>("%PlayButton");
        // optionsButton = GetNode<Button>("%OptionsButton");
        _quitButton = GetNode<Button>("%QuitButton");

        _mainMenuContainer = GetNode<Control>("%MainMenuContainer");
        _levelSelectScreen = GetNode<LevelSelectScreen>("%LevelSelectScreen");

        _levelSelectScreen.Visible = false;
        _mainMenuContainer.Visible = true;

        _playButton.Pressed += OnPlayButtonPressed;
        _quitButton.Pressed += OnQuitButtonPressed;
        _levelSelectScreen.BackPressed += OnLevelSelectBackPressed;
    }

    private void OnPlayButtonPressed()
    {
        _mainMenuContainer.Visible = false;
        _levelSelectScreen.Visible = true;
    }

    private void OnLevelSelectBackPressed()
    {
        _levelSelectScreen.Visible = false;
        _mainMenuContainer.Visible = true;
    }

    private void OnQuitButtonPressed()
    {
        GetTree().Quit();
    }
}