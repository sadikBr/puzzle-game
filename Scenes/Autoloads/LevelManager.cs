using System.Linq;

using Game.Resources.LevelResources;

using Godot;

namespace Game.Autoloads;

public partial class LevelManager : Node
{
    private static LevelManager s_instance;

    [Export]
    private LevelDefinitionResource[] _levelDefinitions;

    private static int s_currentLevelIndex = 0;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
        {
            s_instance = this;
        }
    }

    public static LevelDefinitionResource[] GetLevelDefinitions()
    {
        return [.. s_instance._levelDefinitions];
    }

    public static bool IsLastLevel()
    {
        return s_currentLevelIndex == s_instance._levelDefinitions.Length - 1;
    }

    public static void ChangeToLevel(int levelIndex)
    {
        if (levelIndex >= s_instance._levelDefinitions.Length || levelIndex < 0) return;
        s_currentLevelIndex = levelIndex;

        var levelDefinition = s_instance._levelDefinitions[s_currentLevelIndex];
        s_instance.GetTree().ChangeSceneToFile(levelDefinition.LevelScenePath);
    }

    public static void ChangeToNextLevel()
    {
        ChangeToLevel(s_currentLevelIndex + 1);
    }
}