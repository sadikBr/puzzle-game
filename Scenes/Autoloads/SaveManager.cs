using System;

using Game.Resources.LevelResources;

using Godot;

using Newtonsoft.Json;

namespace Game.Autoloads;

public partial class SaveManager : Node
{

    public static SaveManager Instance { get; private set; }
    private static SaveData s_saveData = new();

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
        {
            Instance = this;
            LoadSaveData();
        }
    }

    public static bool IsLevelCompleted(string levelId)
    {
        s_saveData.LevelCompletionStatus.TryGetValue(levelId, out var data);
        return data?.IsCompleted ?? false;
    }

    public static void SaveLevelCompletion(LevelDefinitionResource levelDefinitionResource)
    {
        s_saveData.SaveLevelCompletion(levelDefinitionResource.Id, true);
        WriteSaveData();
    }

    private static void WriteSaveData()
    {
        var dataString = JsonConvert.SerializeObject(s_saveData);

        using var saveFile = FileAccess.Open("user://save.json", FileAccess.ModeFlags.Write);
        saveFile.StoreLine(dataString);
    }

    private static void LoadSaveData()
    {
        if (!FileAccess.FileExists("user://save.json"))
        {
            return;
        }

        using var saveFile = FileAccess.Open("user://save.json", FileAccess.ModeFlags.Read);
        var dataString = saveFile.GetLine();

        try
        {
            s_saveData = JsonConvert.DeserializeObject<SaveData>(dataString);
        }
        catch (Exception)
        {
            GD.PushWarning("JSON Save file is corrupted");
        }
    }

}
