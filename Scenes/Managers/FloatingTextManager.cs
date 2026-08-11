using Game.UI;

using Godot;

namespace Game.Managers;

public partial class FloatingTextManager : Node
{

    [Export] private PackedScene _floatingTextScene;

    private static FloatingTextManager s_instance;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
        {
            s_instance = this;
        }
    }

    public static void ShowMessage(string message)
    {
        var floatingText = s_instance._floatingTextScene.Instantiate<FloatingText>();
        s_instance.AddChild(floatingText);
        floatingText.SetText(message);
        floatingText.GlobalPosition = floatingText.GetGlobalMousePosition();
    }

}
