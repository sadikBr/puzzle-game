using Game.UI;
using Game.Resources.BuildingResources;
using Godot;
using Game.Buildings;
using Game.Components;
using System.Linq;
using System.Collections.Generic;

namespace Game.Managers;

public partial class BuildingManager : Node
{
    private readonly StringName ACTION_BUILD = "build";
    private readonly StringName ACTION_CANCEL = "cancel";
    private readonly StringName ACTION_DESTROY = "destroy";

    [Signal]
    public delegate void AvailableResourceCountChangedEventHandler(int availableResourceCount);

    [Export]
    private GridManager gridManager;
    [Export]
    private GameUI gameUI;
    [Export]
    private Node2D ySortRoot;
    [Export]
    private PackedScene buildingGhostScene;

    private enum State
    {
        Normal,
        PlacingBuilding
    }

    private int currentResourceCount;
    private int currentlyUsedResourceCount;
    private State currentState;
    private int startingResourceCount;

    private BuildingResource toPlaceBuildingResource;
    private BuildingGhost buildingGhost;
    private Vector2 buildingGhostDimensions;

    private Rect2I hoveredGridArea = new(Vector2I.Zero, Vector2I.One);

    private int AvailableResourceCount => (startingResourceCount + currentResourceCount) - currentlyUsedResourceCount;

    public override void _Ready()
    {
        gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
        gameUI.BuildingResourceSelected += OnBuildingResourceSelected;

        Callable.From(() => EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount)).CallDeferred();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (currentState)
        {
            case State.Normal:
                if (@event.IsActionPressed(ACTION_DESTROY))
                {
                    DestroyBuildingAtHoveredCellPosition();
                }
                break;
            case State.PlacingBuilding:
                if (@event.IsActionPressed(ACTION_CANCEL))
                {
                    ChangeState(State.Normal);
                }
                else if (
                    toPlaceBuildingResource != null &&
                    @event.IsActionPressed(ACTION_BUILD) &&
                    IsBuildingPlaceableAtArea(hoveredGridArea))
                {
                    PlaceBuildingAtHoveredCellPosition();
                }
                break;
            default:
                break;
        }
    }

    public override void _Process(double delta)
    {
        Vector2I gridPosition = Vector2I.Zero;

        switch (currentState)
        {
            case State.Normal:
                gridPosition = gridManager.GetMouseGridPosition();
                break;
            case State.PlacingBuilding:
                gridPosition = gridManager.GetMouseGridCellPositionWithDimensionOffset(buildingGhostDimensions);
                buildingGhost.GlobalPosition = gridPosition * 64;
                break;
        }

        if (hoveredGridArea.Position != gridPosition)
        {
            hoveredGridArea.Position = gridPosition;
            UpdateHoveredGridArea();
        }
    }

    public void SetStartingResourceCount(int count)
    {
        startingResourceCount = count;
    }

    private void UpdateGridDisplay()
    {
        gridManager.ClearHighlightedTiles();

        if (toPlaceBuildingResource.IsAttackBuilding())
        {
            gridManager.HighlightGoblinOccupiedTiles();
            gridManager.HighlightBuildableTiles(true);
        }
        else
        {
            gridManager.HighlightBuildableTiles();
            gridManager.HighlightGoblinOccupiedTiles();
        }

        if (IsBuildingPlaceableAtArea(hoveredGridArea))
        {
            if (toPlaceBuildingResource.IsAttackBuilding())
            {
                gridManager.HighlightAttackTiles(hoveredGridArea, toPlaceBuildingResource.AttackRadius);
            }
            else
            {
                gridManager.HighlightExpandedBuildableTiles(hoveredGridArea, toPlaceBuildingResource.BuildableRadius);
            }
            gridManager.HighlightResourceTiles(hoveredGridArea, toPlaceBuildingResource.ResourceRadius);
            buildingGhost.SetValid();
        }
        else
        {
            buildingGhost.SetInvalid();
        }

        buildingGhost.DoHoverAnimation();
    }

    private void PlaceBuildingAtHoveredCellPosition()
    {
        Node2D building = toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
        ySortRoot.AddChild(building);

        building.GlobalPosition = hoveredGridArea.Position * 64;
        building.GetFirstNodeOfType<BuildingAnimatorComponent>()?.PlayInAnimation();

        currentlyUsedResourceCount += toPlaceBuildingResource.ResourceCost;

        ChangeState(State.Normal);
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void DestroyBuildingAtHoveredCellPosition()
    {
        BuildingComponent buildingComponent = BuildingComponent.GetValidBuilidingComponents(this)
            .FirstOrDefault((buildingComponent) => buildingComponent.BuildingResource.IsDeletable && buildingComponent.IsTileInBuildingArea(hoveredGridArea.Position));

        if (buildingComponent == null) return;

        currentlyUsedResourceCount -= buildingComponent.BuildingResource.ResourceCost;
        buildingComponent.Destroy();
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void ClearBuildingGhost()
    {
        gridManager.ClearHighlightedTiles();
        if (IsInstanceValid(buildingGhost))
        {
            buildingGhost.QueueFree();
        }
        buildingGhost = null;
    }

    private bool IsBuildingPlaceableAtArea(Rect2I tileArea)
    {
        var allTilesBuildable = gridManager.IsTileAreaBuildable(tileArea, toPlaceBuildingResource.IsAttackBuilding());

        return allTilesBuildable && AvailableResourceCount >= toPlaceBuildingResource.ResourceCost;
    }

    private List<Vector2I> GetTilePositionsInTileArea(Rect2I tileArea)
    {
        var result = new List<Vector2I>();
        for (int x = tileArea.Position.X; x < tileArea.End.X; x++)
        {
            for (int y = tileArea.Position.Y; y < tileArea.End.Y; y++)
            {
                result.Add(new Vector2I(x, y));
            }
        }
        return result;
    }

    private void UpdateHoveredGridArea()
    {
        switch (currentState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                UpdateGridDisplay();
                break;
        }
    }

    private void ChangeState(State toState)
    {
        switch (currentState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                ClearBuildingGhost();
                toPlaceBuildingResource = null;
                break;
        }

        currentState = toState;

        switch (currentState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                buildingGhost = buildingGhostScene.Instantiate<BuildingGhost>();
                ySortRoot.AddChild(buildingGhost);
                break;
        }
    }

    private void OnResourceTilesUpdated(int resourceCount)
    {
        currentResourceCount = resourceCount;
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void OnBuildingResourceSelected(BuildingResource buildingResource)
    {
        ChangeState(State.PlacingBuilding);
        hoveredGridArea.Size = buildingResource.Dimensions;

        var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
        buildingGhost.AddSpriteNode(buildingSprite);
        buildingGhost.SetDimensions(buildingResource.Dimensions);
        buildingGhostDimensions = buildingResource.Dimensions;

        toPlaceBuildingResource = buildingResource;
        UpdateGridDisplay();
    }
}
