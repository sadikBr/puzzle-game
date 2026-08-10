using System.Linq;

using Game.Buildings;
using Game.Components;
using Game.Resources.BuildingResources;
using Game.UI;

using Godot;

namespace Game.Managers;

public partial class BuildingManager : Node
{
    private readonly StringName _actionBuild = "build";
    private readonly StringName _actionCancel = "cancel";
    private readonly StringName _actionDestroy = "destroy";

    [Signal]
    public delegate void AvailableResourceCountChangedEventHandler(int availableResourceCount);

    [Export]
    private GridManager _gridManager;
    [Export]
    private GameUI _gameUI;
    [Export]
    private Node2D _ySortRoot;
    [Export]
    private PackedScene _buildingGhostScene;

    private enum State
    {
        Normal,
        PlacingBuilding
    }

    private int _currentResourceCount;
    private int _currentlyUsedResourceCount;
    private State _currentState;
    private int _startingResourceCount;

    private BuildingResource _toPlaceBuildingResource;
    private BuildingGhost _buildingGhost;
    private Vector2 _buildingGhostDimensions;

    private Rect2I _hoveredGridArea = new(Vector2I.Zero, Vector2I.One);

    private int AvailableResourceCount => _startingResourceCount + _currentResourceCount - _currentlyUsedResourceCount;

    public override void _Ready()
    {
        _gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
        _gameUI.BuildingResourceSelected += OnBuildingResourceSelected;

        Callable.From(() => EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount)).CallDeferred();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (_currentState)
        {
            case State.Normal:
                if (@event.IsActionPressed(_actionDestroy))
                {
                    DestroyBuildingAtHoveredCellPosition();
                }
                break;
            case State.PlacingBuilding:
                if (@event.IsActionPressed(_actionCancel))
                {
                    ChangeState(State.Normal);
                }
                else if (
                    _toPlaceBuildingResource != null &&
                    @event.IsActionPressed(_actionBuild) &&
                    IsBuildingPlaceableAtArea(_hoveredGridArea))
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

        switch (_currentState)
        {
            case State.Normal:
                gridPosition = _gridManager.GetMouseGridPosition();
                break;
            case State.PlacingBuilding:
                gridPosition = _gridManager.GetMouseGridCellPositionWithDimensionOffset(_buildingGhostDimensions);
                _buildingGhost.GlobalPosition = gridPosition * 64;
                break;
        }

        if (_hoveredGridArea.Position != gridPosition)
        {
            _hoveredGridArea.Position = gridPosition;
            UpdateHoveredGridArea();
        }
    }

    public void SetStartingResourceCount(int count)
    {
        _startingResourceCount = count;
    }

    private void UpdateGridDisplay()
    {
        _gridManager.ClearHighlightedTiles();

        if (_toPlaceBuildingResource.IsAttackBuilding())
        {
            _gridManager.HighlightGoblinOccupiedTiles();
            _gridManager.HighlightBuildableTiles(true);
        }
        else
        {
            _gridManager.HighlightBuildableTiles();
            _gridManager.HighlightGoblinOccupiedTiles();
        }

        if (IsBuildingPlaceableAtArea(_hoveredGridArea))
        {
            if (_toPlaceBuildingResource.IsAttackBuilding())
            {
                _gridManager.HighlightAttackTiles(_hoveredGridArea, _toPlaceBuildingResource.AttackRadius);
            }
            else
            {
                _gridManager.HighlightExpandedBuildableTiles(_hoveredGridArea, _toPlaceBuildingResource.BuildableRadius);
            }
            _gridManager.HighlightResourceTiles(_hoveredGridArea, _toPlaceBuildingResource.ResourceRadius);
            _buildingGhost.SetValid();
        }
        else
        {
            _buildingGhost.SetInvalid();
        }

        _buildingGhost.DoHoverAnimation();
    }

    private void PlaceBuildingAtHoveredCellPosition()
    {
        Node2D building = _toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
        _ySortRoot.AddChild(building);

        building.GlobalPosition = _hoveredGridArea.Position * 64;
        building.GetFirstNodeOfType<BuildingAnimatorComponent>()?.PlayInAnimation();

        _currentlyUsedResourceCount += _toPlaceBuildingResource.ResourceCost;

        ChangeState(State.Normal);
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void DestroyBuildingAtHoveredCellPosition()
    {
        BuildingComponent buildingComponent = BuildingComponent.GetValidBuilidingComponents(this)
            .FirstOrDefault((buildingComponent) => buildingComponent.BuildingResource.IsDeletable && buildingComponent.IsTileInBuildingArea(_hoveredGridArea.Position));

        if (buildingComponent == null) return;

        _currentlyUsedResourceCount -= buildingComponent.BuildingResource.ResourceCost;
        buildingComponent.Destroy();
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void ClearBuildingGhost()
    {
        _gridManager.ClearHighlightedTiles();
        if (IsInstanceValid(_buildingGhost))
        {
            _buildingGhost.QueueFree();
        }
        _buildingGhost = null;
    }

    private bool IsBuildingPlaceableAtArea(Rect2I tileArea)
    {
        var allTilesBuildable = _gridManager.IsTileAreaBuildable(tileArea, _toPlaceBuildingResource.IsAttackBuilding());

        return allTilesBuildable && AvailableResourceCount >= _toPlaceBuildingResource.ResourceCost;
    }

    // private List<Vector2I> GetTilePositionsInTileArea(Rect2I tileArea)
    // {
    //     var result = new List<Vector2I>();
    //     for (int x = tileArea.Position.X; x < tileArea.End.X; x++)
    //     {
    //         for (int y = tileArea.Position.Y; y < tileArea.End.Y; y++)
    //         {
    //             result.Add(new Vector2I(x, y));
    //         }
    //     }
    //     return result;
    // }

    private void UpdateHoveredGridArea()
    {
        switch (_currentState)
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
        switch (_currentState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                ClearBuildingGhost();
                _toPlaceBuildingResource = null;
                break;
        }

        _currentState = toState;

        switch (_currentState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                _buildingGhost = _buildingGhostScene.Instantiate<BuildingGhost>();
                _ySortRoot.AddChild(_buildingGhost);
                break;
        }
    }

    private void OnResourceTilesUpdated(int resourceCount)
    {
        _currentResourceCount = resourceCount;
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void OnBuildingResourceSelected(BuildingResource buildingResource)
    {
        ChangeState(State.PlacingBuilding);
        _hoveredGridArea.Size = buildingResource.Dimensions;

        var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
        _buildingGhost.AddSpriteNode(buildingSprite);
        _buildingGhost.SetDimensions(buildingResource.Dimensions);
        _buildingGhostDimensions = buildingResource.Dimensions;

        _toPlaceBuildingResource = buildingResource;
        UpdateGridDisplay();
    }
}