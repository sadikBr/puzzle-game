using Game.Components;

using Godot;

namespace Game.Buildings;

public partial class GoblinCamp : Node2D
{

    private const string DESTROYED_ANIMATION = "destroyed";
    private const string DEFAULT_ANIMATION = "default";

    [Export] private BuildingComponent _buildingComponent;
    [Export] private AnimatedSprite2D _animatedSprite2D;
    [Export] private AnimatedSprite2D _fireAnimatedSprite2D;

    public override void _Ready()
    {
        _fireAnimatedSprite2D.Visible = false;

        _buildingComponent.Disabled += OnBuildingDisabled;
        _buildingComponent.Enabled += OnBuildingEnabled;
    }

    private void OnBuildingDisabled()
    {
        _animatedSprite2D.Play(DESTROYED_ANIMATION);
        _fireAnimatedSprite2D.Visible = true;
    }

    private void OnBuildingEnabled()
    {
        _animatedSprite2D.Play(DEFAULT_ANIMATION);
        _fireAnimatedSprite2D.Visible = false;
    }

}
