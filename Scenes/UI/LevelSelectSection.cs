using Game.Autoloads;
using Game.Resources.LevelResources;

using Godot;

namespace Game.UI;

public partial class LevelSelectSection : PanelContainer
{
    [Signal]
    public delegate void LevelSelectedEventHandler(int levelIndex);

    private Button _button;
    private Label _resourceCountLabel;
    private Label _levelNumberLabel;
    private TextureRect _completedIndicator;

    private int _levelIndex;

    public override void _Ready()
    {
        _button = GetNode<Button>("%Button");
        _resourceCountLabel = GetNode<Label>("%ResourceCountLabel");
        _levelNumberLabel = GetNode<Label>("%LevelNumberLabel");
        _completedIndicator = GetNode<TextureRect>("%CompletedIndicator");

        _button.Pressed += OnButtonPressed;
    }

    public void SetLevelDefinition(LevelDefinitionResource levelDefinitionResource)
    {
        _completedIndicator.Visible = SaveManager.IsLevelCompleted(levelDefinitionResource.Id);
        _resourceCountLabel.Text = levelDefinitionResource.StartingResourceCount.ToString();
    }

    public void SetLevelIndex(int index)
    {
        _levelIndex = index;
        _levelNumberLabel.Text = $"Level {index + 1}";
    }

    private void OnButtonPressed()
    {
        EmitSignal(SignalName.LevelSelected, _levelIndex);
    }
}