using Game.Autoloads;

using Godot;

namespace Game.UI;

public partial class LevelSelectScreen : MarginContainer
{
    [Signal]
    public delegate void BackPressedEventHandler();

    [Export]
    private PackedScene _levelSelectSectionScene;

    private GridContainer _gridContainer;
    private Button _backButton;

    public override void _Ready()
    {
        _gridContainer = GetNode<GridContainer>("%GridContainer");
        _backButton = GetNode<Button>("BackButton");

        var levelDefinitions = LevelManager.GetLevelDefinitions();
        for (int i = 0; i < levelDefinitions.Length; i++)
        {
            var levelDefinition = levelDefinitions[i];
            var levelSelectSection = _levelSelectSectionScene.Instantiate<LevelSelectSection>();
            _gridContainer.AddChild(levelSelectSection);

            levelSelectSection.SetLevelDefinition(levelDefinition);
            levelSelectSection.SetLevelIndex(i);

            levelSelectSection.LevelSelected += OnLevelSelected;
        }

        _backButton.Pressed += OnBackButtonPressed;
    }

    private void OnLevelSelected(int levelIndex)
    {
        LevelManager.Instance.ChangeToLevel(levelIndex);
    }

    private void OnBackButtonPressed()
    {
        EmitSignal(SignalName.BackPressed);
    }
}