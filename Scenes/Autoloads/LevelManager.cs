using System.Linq;
using Game.Resources.LevelResources;
using Godot;

namespace Game.Autoloads;

public partial class LevelManager : Node
{
    public static LevelManager Instance { get; private set; }

    [Export]
    private LevelDefinitionResource[] levelDefinitions;

    private int currentLevelIndex = 0;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
        {
            Instance = this;
        }
    }

    public static LevelDefinitionResource[] GetLevelDefinitions()
    {
        return Instance.levelDefinitions.ToArray();
    }

    public void ChangeToLevel(int levelIndex)
    {
        if (levelIndex >= levelDefinitions.Length || levelIndex < 0) return;
        currentLevelIndex = levelIndex;

        var levelDefinition = levelDefinitions[currentLevelIndex];
        GetTree().ChangeSceneToFile(levelDefinition.LevelScenePath);
    }

    public void ChangeToNextLevel()
    {
        ChangeToLevel(currentLevelIndex + 1);
    }
}
