using System;

using Game.Autoloads;
using Game.Resources.LevelResources;

using Godot;

namespace Game.UI;

public partial class LevelSelectScreen : MarginContainer
{
    private const int PAGE_SIZE = 6;

    [Signal]
    public delegate void BackPressedEventHandler();

    [Export]
    private PackedScene _levelSelectSectionScene;

    private GridContainer _gridContainer;
    private Button _backButton;
    private Button _previousPageButton;
    private Button _nextPageButton;

    private int _pageIndex;
    private int _totalPages;
    private LevelDefinitionResource[] _levelDefinitions;

    public override void _Ready()
    {
        _gridContainer = GetNode<GridContainer>("%GridContainer");
        _backButton = GetNode<Button>("BackButton");
        _previousPageButton = GetNode<Button>("%PreviousPageButton");
        _nextPageButton = GetNode<Button>("%NextPageButton");

        _levelDefinitions = LevelManager.GetLevelDefinitions();
        _totalPages = (int)Mathf.Ceil(_levelDefinitions.Length / (float)PAGE_SIZE);

        if (_totalPages == 1)
        {
            _previousPageButton.Disabled = true;
            _previousPageButton.Modulate = Colors.Transparent;
            _nextPageButton.Disabled = true;
            _nextPageButton.Modulate = Colors.Transparent;
        }

        ShowPage();

        _backButton.Pressed += OnBackButtonPressed;
        _previousPageButton.Pressed += OnPreviousPageButtonPressed;
        _nextPageButton.Pressed += OnNextPageButtonPressed;
    }

    private void ShowPage()
    {
        foreach (var child in _gridContainer.GetChildren())
        {
            child.QueueFree();
        }

        var startIndex = _pageIndex * PAGE_SIZE;
        var endIndex = Mathf.Min(startIndex + PAGE_SIZE, _levelDefinitions.Length);

        for (int i = startIndex; i < endIndex; i++)
        {
            var levelDefinition = _levelDefinitions[i];
            var levelSelectSection = _levelSelectSectionScene.Instantiate<LevelSelectSection>();
            _gridContainer.AddChild(levelSelectSection);

            levelSelectSection.SetLevelDefinition(levelDefinition);
            levelSelectSection.SetLevelIndex(i);

            levelSelectSection.LevelSelected += OnLevelSelected;
        }
    }

    private void OnPageChanged(int direction)
    {
        _pageIndex = ((_pageIndex + direction) % _totalPages + _totalPages) % _totalPages;
        ShowPage();
    }

    private void OnLevelSelected(int levelIndex)
    {
        LevelManager.Instance.ChangeToLevel(levelIndex);
    }

    private void OnBackButtonPressed()
    {
        EmitSignal(SignalName.BackPressed);
    }

    private void OnNextPageButtonPressed()
    {
        OnPageChanged(1);
    }

    private void OnPreviousPageButtonPressed()
    {
        OnPageChanged(-1);
    }

}