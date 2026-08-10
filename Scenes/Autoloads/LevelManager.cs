using System.Linq;

using Game.Resources.LevelResources;

using Godot;

namespace Game.Autoloads;

public partial class LevelManager : Node
{
    public static LevelManager Instance { get; private set; }

    [Export]
    private LevelDefinitionResource[] _levelDefinitions;

    private int _currentLevelIndex = 0;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
        {
            Instance = this;
        }
    }

    public static LevelDefinitionResource[] GetLevelDefinitions()
    {
        return [.. Instance._levelDefinitions];
    }

    public void ChangeToLevel(int levelIndex)
    {
        if (levelIndex >= _levelDefinitions.Length || levelIndex < 0) return;
        _currentLevelIndex = levelIndex;

        var levelDefinition = _levelDefinitions[_currentLevelIndex];
        GetTree().ChangeSceneToFile(levelDefinition.LevelScenePath);
    }

    public void ChangeToNextLevel()
    {
        ChangeToLevel(_currentLevelIndex + 1);
    }
}