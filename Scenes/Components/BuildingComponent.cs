using System.Collections.Generic;
using System.Linq;
using Game.Autoloads;
using Game.Resources.BuildingResources;
using Godot;

namespace Game.Components;

public partial class BuildingComponent : Node2D
{
    [Export(PropertyHint.File, "*.tres")]
    private string buildingResourcePath;
    [Export]
    private BuildingAnimatorComponent buildingAnimatorComponent;

    public BuildingResource BuildingResource { get; private set; }
    public bool IsDestroying { get; private set; }
    public bool IsDisabled { get; private set; }

    private HashSet<Vector2I> occupiedTiles = new();

    public static IEnumerable<BuildingComponent> GetValidBuilidingComponents(Node node)
    {
        return node.GetTree()
            .GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>()
            .Where(buildingComponent => !buildingComponent.IsDestroying);
    }

    public static IEnumerable<BuildingComponent> GetDangerBuildingComponents(Node node)
    {
        return GetValidBuilidingComponents(node).Where(buildingComponent => buildingComponent.BuildingResource.IsDangerBuilding());
    }

    public override void _Ready()
    {
        if (buildingResourcePath != null)
        {
            BuildingResource = GD.Load<BuildingResource>(buildingResourcePath);
        }

        if (buildingAnimatorComponent != null)
        {
            buildingAnimatorComponent.DestroyAnimationFinished += OnDestroyAnimationFinished;
        }

        AddToGroup(nameof(BuildingComponent));
        Callable.From(Initialize).CallDeferred();
    }

    public Vector2I GetGridCellPosition()
    {
        Vector2 gridPosition = (GlobalPosition / 64).Floor();
        return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
    }

    public HashSet<Vector2I> GetOccupiedCellPositions()
    {
        return occupiedTiles.ToHashSet();
    }

    public Rect2I GetTileArea()
    {
        var rootCell = GetGridCellPosition();
        return new Rect2I(rootCell, BuildingResource.Dimensions);
    }

    public bool IsTileInBuildingArea(Vector2I tilePosition)
    {
        return occupiedTiles.Contains(tilePosition);
    }

    public void Disable()
    {
        if (IsDisabled) return;
        IsDisabled = true;
        GameEvents.EmitBuildingDisabled(this);
    }

    public void Enable()
    {
        if (!IsDisabled) return;
        IsDisabled = false;
        GameEvents.EmitBuildingEnabled(this);
    }

    public void Destroy()
    {
        IsDestroying = true;
        GameEvents.EmitBuildingDestroyed(this);
        buildingAnimatorComponent?.PlayOutAnimation();

        if (buildingAnimatorComponent == null)
        {
            Owner.QueueFree();
        }
    }

    private void CalculateOccupiedCellPositions()
    {
        var gridPosition = GetGridCellPosition();
        for (int x = gridPosition.X; x < gridPosition.X + BuildingResource.Dimensions.X; x++)
        {
            for (int y = gridPosition.Y; y < gridPosition.Y + BuildingResource.Dimensions.Y; y++)
            {
                occupiedTiles.Add(new Vector2I(x, y));
            }
        }
    }

    private void Initialize()
    {
        CalculateOccupiedCellPositions();
        GameEvents.EmitBuildingPlaced(this);
    }

    private void OnDestroyAnimationFinished()
    {
        Owner.QueueFree();
    }
}
