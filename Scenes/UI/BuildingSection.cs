using Game.Resources.BuildingResources;

using Godot;

namespace Game.UI;

public partial class BuildingSection : PanelContainer
{
    [Signal]
    public delegate void SelectButtonPressedEventHandler();

    private Label _titleLabel;
    private Label _descriptionLabel;
    private Label _costLabel;
    private Button _selectButton;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("%TitleLabel");
        _costLabel = GetNode<Label>("%CostLabel");
        _descriptionLabel = GetNode<Label>("%DescriptionLabel");
        _selectButton = GetNode<Button>("%Button");

        _selectButton.Pressed += OnSelectButtonPressed;
    }

    public void SetBuildingResource(BuildingResource buildingResource)
    {
        _titleLabel.Text = buildingResource.DisplayName;
        _costLabel.Text = $"{buildingResource.ResourceCost}";
        _descriptionLabel.Text = buildingResource.Description;
    }

    private void OnSelectButtonPressed()
    {
        EmitSignal(SignalName.SelectButtonPressed);
    }
}