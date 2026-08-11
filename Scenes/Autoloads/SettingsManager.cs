using Godot;

namespace Game.Autoloads;

public partial class SettingsManager : Node
{

    public override void _Ready()
    {
        RenderingServer.SetDefaultClearColor(new Color("47aba9"));
        GetViewport().GetWindow().MinSize = new Vector2I(1280, 720);
    }

}
