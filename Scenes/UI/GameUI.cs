using Game.Managers;
using Game.Resources.BuildingResources;

using Godot;

namespace Game.UI;

public partial class GameUI : CanvasLayer
{
    [Signal]
    public delegate void BuildingResourceSelectedEventHandler(BuildingResource buildingResource);

    private VBoxContainer _buildingSectionContainer;
    private Label _resourceLabel;

    [Export]
    private BuildingManager _buildingManager;
    [Export]
    private BuildingResource[] _buildingResources;
    [Export]
    private PackedScene _buildingSectionScene;

    public override void _Ready()
    {
        _buildingSectionContainer = GetNode<VBoxContainer>("%BuildingSectionContainer");
        _resourceLabel = GetNode<Label>("%ResourceLabel");
        CreateBuildingSections();

        _buildingManager.AvailableResourceCountChanged += OnAvailableResourceCountChanged;
    }

    public void HideUI()
    {
        Visible = false;
    }

    private void CreateBuildingSections()
    {
        foreach (var buildingResource in _buildingResources)
        {
            var buildingSection = _buildingSectionScene.Instantiate<BuildingSection>();
            _buildingSectionContainer.AddChild(buildingSection);

            buildingSection.SetBuildingResource(buildingResource);
            buildingSection.SelectButtonPressed += () => EmitSignal(SignalName.BuildingResourceSelected, buildingResource);
        }
    }

    private void OnAvailableResourceCountChanged(int availableResourceCount)
    {
        _resourceLabel.Text = $"{availableResourceCount}";
    }
}