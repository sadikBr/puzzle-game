using System;
using System.Collections.Generic;
using System.Linq;

using Game.Autoloads;
using Game.Components;
using Game.Levels.Utils;

using Godot;

namespace Game.Managers;

public partial class GridManager : Node
{
    private const string IS_BUILDABLE = "IsBuildable";
    private const string IS_IGNORED = "IsIgnored";
    private const string IS_WOOD = "IsWood";

    [Signal]
    public delegate void ResourceTilesUpdatedEventHandler(int collectedTiles);
    [Signal]
    public delegate void GridStateUpdatedEventHandler();

    private readonly HashSet<Vector2I> _validBuildableTiles = [];
    private readonly HashSet<Vector2I> _validBuildableAttackTiles = [];
    private readonly HashSet<Vector2I> _allTilesInBuildingRadius = [];
    private readonly HashSet<Vector2I> _collectedResourceTiles = [];
    private readonly HashSet<Vector2I> _occupiedTiles = [];
    private readonly HashSet<Vector2I> _dangerOccupiedTiles = [];
    private readonly HashSet<Vector2I> _attackTiles = [];

    [Export]
    private TileMapLayer _highlightTileMapLayer;
    [Export]
    private TileMapLayer _baseTerrainTileMapLayer;

    private List<TileMapLayer> _allTileMapLayers = [];
    private readonly Dictionary<TileMapLayer, ElevationLayer> _tileMapLayerToElevationLayer = [];
    private readonly Dictionary<BuildingComponent, HashSet<Vector2I>> _buildingToBuildableTiles = [];
    private readonly Dictionary<BuildingComponent, HashSet<Vector2I>> _dangerBuildingToTiles = [];
    private readonly Dictionary<BuildingComponent, HashSet<Vector2I>> _attackBuildingToTiles = [];

    public override void _Ready()
    {
        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingPlaced, Callable.From<BuildingComponent>(OnBuildingPlaced));
        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingDestroyed, Callable.From<BuildingComponent>(OnBuildingDestroyed));
        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingEnabled, Callable.From<BuildingComponent>(OnBuildingEnabled));
        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingDisabled, Callable.From<BuildingComponent>(OnBuildingDisabled));
        _allTileMapLayers = GetAllTileMapLayers(_baseTerrainTileMapLayer);
        MapTileMapLayersToElevationLayers();
    }

    public (TileMapLayer, bool) GetTileCustomData(Vector2I tilePosition, string dataName)
    {
        foreach (var tileMapLayer in _allTileMapLayers)
        {
            TileData cellData = tileMapLayer.GetCellTileData(tilePosition);
            if (cellData == null || (bool)cellData.GetCustomData(IS_IGNORED)) continue;
            return (tileMapLayer, (bool)cellData.GetCustomData(dataName));
        }

        return (null, false);
    }

    public bool IsTilePositionInAnyBuildingRadius(Vector2I tilePosition)
    {
        return _allTilesInBuildingRadius.Contains(tilePosition);
    }

    public bool IsTileAreaBuildable(Rect2I tileArea, bool isAttackTiles = false)
    {
        var tiles = tileArea.ToTiles();

        if (tiles.Count() == 0) return false;

        (TileMapLayer firstTileMapLayer, _) = GetTileCustomData(tiles[0], IS_BUILDABLE);
        var targetElevationLayer = firstTileMapLayer != null ? _tileMapLayerToElevationLayer[firstTileMapLayer] : null;

        var tileSetToCheck = GetBuildableTileSet(isAttackTiles);
        if (isAttackTiles)
        {
            tileSetToCheck = [.. tileSetToCheck.Except(_occupiedTiles)];
        }

        return tiles.All(tilePosition =>
        {
            (TileMapLayer tileMapLayer, bool isBuildable) = GetTileCustomData(tilePosition, IS_BUILDABLE);
            var elevationLayer = tileMapLayer != null ? _tileMapLayerToElevationLayer[tileMapLayer] : null;
            return isBuildable && tileSetToCheck.Contains(tilePosition) && elevationLayer == targetElevationLayer;
        });
    }

    public void HighlightDangerOccupiedTiles()
    {
        var atlasCoords = new Vector2I(2, 0);
        foreach (var tilePosition in _dangerOccupiedTiles)
        {
            _highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
        }
    }

    public void HighlightBuildableTiles(bool isAttackTiles = false)
    {
        foreach (var tilePosition in GetBuildableTileSet(isAttackTiles))
        {
            _highlightTileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
        }
    }

    public void HighlightExpandedBuildableTiles(Rect2I tileArea, int radius)
    {
        HashSet<Vector2I> validTiles = [.. GetValidTilesInRadius(tileArea, radius)];
        var expandedTiles = validTiles.Except(_validBuildableTiles).Except(_occupiedTiles);
        Vector2I atlasCoords = new(1, 0);
        foreach (var tilePosition in expandedTiles)
        {
            _highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
        }
    }

    public void HighlightAttackTiles(Rect2I tileArea, int radius)
    {
        var buildingAreaTiles = tileArea.ToTiles().ToHashSet();
        var validTiles = GetValidTilesInRadius(tileArea, radius).ToHashSet().Except(_validBuildableAttackTiles).Except(buildingAreaTiles);
        Vector2I atlasCoords = new(1, 0);
        foreach (var tilePosition in validTiles)
        {
            _highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
        }
    }

    public void HighlightResourceTiles(Rect2I tileArea, int radius)
    {
        var resourceTiles = GetResourceTilesInRadius(tileArea, radius);
        Vector2I atlasCoords = new(1, 0);
        foreach (var tilePosition in resourceTiles)
        {
            _highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
        }
    }

    public void ClearHighlightedTiles()
    {
        _highlightTileMapLayer.Clear();
    }

    public Vector2I GetMouseGridCellPositionWithDimensionOffset(Vector2 dimensions)
    {
        var mousePosition = _highlightTileMapLayer.GetGlobalMousePosition() / 64;
        mousePosition -= dimensions / 2;
        mousePosition = mousePosition.Round();
        return new Vector2I((int)mousePosition.X, (int)mousePosition.Y);
    }

    public Vector2I GetMouseGridPosition()
    {
        return ConvertWorldPositionToTilePosition(_highlightTileMapLayer.GetGlobalMousePosition());
    }

    public Vector2I ConvertWorldPositionToTilePosition(Vector2 worldPosition)
    {
        Vector2 tilePosition = (worldPosition / 64).Floor();
        return new Vector2I((int)tilePosition.X, (int)tilePosition.Y);
    }

    public bool CanDestroyBuilding(BuildingComponent toDestroyBuildingComponent)
    {
        if (toDestroyBuildingComponent.BuildingResource.BuildableRadius > 0)
        {
            return !WillBuildingDestructionCreateOrphanBuildings(toDestroyBuildingComponent) &&
                IsBuildingNetworkConnected(toDestroyBuildingComponent);
        }
        else if (toDestroyBuildingComponent.BuildingResource.IsAttackBuilding())
        {
            return CanDestroyBarracks(toDestroyBuildingComponent);
        }
        return true;
    }

    private bool CanDestroyBarracks(BuildingComponent toDestroyBuildingComponent)
    {
        var disabledDangerBuildings = BuildingComponent.GetDangerBuildingComponents(this)
            .Where(buildingComponent => buildingComponent.GetOccupiedCellPositions()
                .Any(tilePosition => _attackBuildingToTiles[toDestroyBuildingComponent].Contains(tilePosition))
            );

        if (disabledDangerBuildings.Count() == 0) return true;

        var allDangerBuildingsStillDisabled = disabledDangerBuildings.All(
            dangerBuilding => dangerBuilding.GetOccupiedCellPositions()
                .Any(tilePosition => _attackBuildingToTiles.Keys.Where(key => key != toDestroyBuildingComponent)
                    .Any(key => _attackBuildingToTiles[key].Contains(tilePosition))
                )
        );

        if (allDangerBuildingsStillDisabled) return true;

        var nonDangerBuildings = BuildingComponent.GetNonDangerBuildingComponents(this)
            .Where(nonDangerBuilding => nonDangerBuilding != toDestroyBuildingComponent);
        var anyDangerBuildingContainsPlayerBuilding = disabledDangerBuildings.Any(dangerBuilding =>
        {
            var dangerTiles = _dangerBuildingToTiles[dangerBuilding];
            return nonDangerBuildings.Any(
                nonDangerBuilding => nonDangerBuilding.GetOccupiedCellPositions()
                    .Any(tilePosition => dangerTiles.Contains(tilePosition))
            );
        });

        return !anyDangerBuildingContainsPlayerBuilding;
    }

    private bool WillBuildingDestructionCreateOrphanBuildings(BuildingComponent toDestroyBuildingComponent)
    {
        var dependentBuildings = BuildingComponent.GetNonDangerBuildingComponents(this)
            .Where(buildingComponent =>
            {
                if (buildingComponent == toDestroyBuildingComponent) return false;
                if (buildingComponent.BuildingResource.IsBase) return false;

                var anyTilesInRadius = buildingComponent.GetOccupiedCellPositions()
                    .Any(tilePosition => _buildingToBuildableTiles[toDestroyBuildingComponent].Contains(tilePosition));
                return anyTilesInRadius;
            });

        var allBuildingsStillValid = dependentBuildings.All(dependentBuilding =>
        {
            var tilesForBuilding = dependentBuilding.GetOccupiedCellPositions();
            var buildingsToCheck = _buildingToBuildableTiles.Keys.Where(key => key != toDestroyBuildingComponent && key != dependentBuilding);
            return tilesForBuilding.All(
                tilePosition => buildingsToCheck
                    .Any(buildingComponent => _buildingToBuildableTiles[buildingComponent].Contains(tilePosition))
            );
        });

        return !allBuildingsStillValid;
    }

    private bool IsBuildingNetworkConnected(BuildingComponent toDestroyBuildingComponent)
    {
        var baseBuilding = BuildingComponent.GetValidBuildingComponents(this)
            .First(buildingComponent => buildingComponent.BuildingResource.IsBase);

        var visitedBuildings = new HashSet<BuildingComponent>();
        VisitAllConnectedBuildings(baseBuilding, toDestroyBuildingComponent, visitedBuildings);

        var totalBuildingsToVisit = BuildingComponent.GetValidBuildingComponents(this)
            .Count(buildingComponent => buildingComponent != toDestroyBuildingComponent && buildingComponent.BuildingResource.BuildableRadius > 0);
        var visitedBuildingsCount = visitedBuildings.Count;

        return visitedBuildingsCount == totalBuildingsToVisit;
    }

    private void VisitAllConnectedBuildings(BuildingComponent rootBuilding, BuildingComponent excludeBuilding, HashSet<BuildingComponent> visitedBuildings)
    {
        var dependentBuildings = BuildingComponent.GetNonDangerBuildingComponents(this)
            .Where(buildingComponent =>
            {
                if (buildingComponent.BuildingResource.BuildableRadius == 0) return false;
                if (visitedBuildings.Contains(buildingComponent)) return false;

                var anyTilesInRadius = buildingComponent.GetOccupiedCellPositions()
                    .All(tilePosition => _buildingToBuildableTiles[rootBuilding].Contains(tilePosition));
                return buildingComponent != excludeBuilding && anyTilesInRadius;
            }).ToList();

        visitedBuildings.UnionWith(dependentBuildings);
        foreach (var dependentBuilding in dependentBuildings)
        {
            VisitAllConnectedBuildings(dependentBuilding, excludeBuilding, visitedBuildings);
        }
    }

    private HashSet<Vector2I> GetBuildableTileSet(bool isAttackTiles = false)
    {
        return isAttackTiles ? _validBuildableAttackTiles : _validBuildableTiles;
    }

    private List<TileMapLayer> GetAllTileMapLayers(Node2D rootNode)
    {
        List<TileMapLayer> result = [];
        var children = rootNode.GetChildren();
        children.Reverse();

        foreach (var child in children)
        {
            if (child is Node2D childNode)
            {
                result.AddRange(GetAllTileMapLayers(childNode));
            }
        }

        if (rootNode is TileMapLayer tileMapLayer)
        {

            result.Add(tileMapLayer);
        }

        return result;
    }

    private void MapTileMapLayersToElevationLayers()
    {
        foreach (var layer in _allTileMapLayers)
        {
            ElevationLayer elevationLayer;
            Node startNode = layer;
            do
            {
                var parent = startNode.GetParent();
                elevationLayer = parent as ElevationLayer;
                startNode = parent;
            } while (elevationLayer == null && startNode != null);

            _tileMapLayerToElevationLayer[layer] = elevationLayer;
        }
    }

    private void UpdateDangerOccupiedTiles(BuildingComponent buildingComponent)
    {
        _occupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPositions());

        if (buildingComponent.BuildingResource.IsDangerBuilding())
        {
            var tileArea = buildingComponent.GetTileArea();
            var tilesInRadius = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.DangerRadius).ToHashSet();

            _dangerBuildingToTiles[buildingComponent] = [.. tilesInRadius];

            if (!buildingComponent.IsDisabled)
            {
                tilesInRadius.ExceptWith(_occupiedTiles);
                _dangerOccupiedTiles.UnionWith(tilesInRadius);
            }
        }
    }

    private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
    {
        _occupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPositions());
        var tileArea = buildingComponent.GetTileArea();

        if (buildingComponent.BuildingResource.BuildableRadius > 0)
        {
            var allTiles = GetTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius, (_) => true);
            _allTilesInBuildingRadius.UnionWith(allTiles);

            var validTiles = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius);
            _buildingToBuildableTiles[buildingComponent] = [.. validTiles];
            _validBuildableTiles.UnionWith(validTiles);
        }
        _validBuildableTiles.ExceptWith(_occupiedTiles);
        _validBuildableAttackTiles.UnionWith(_validBuildableTiles);
        _validBuildableTiles.ExceptWith(_dangerOccupiedTiles);

        EmitSignal(SignalName.GridStateUpdated);
    }

    private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent)
    {
        var tileArea = buildingComponent.GetTileArea();
        List<Vector2I> resourceTiles = GetResourceTilesInRadius(tileArea, buildingComponent.BuildingResource.ResourceRadius);

        int oldResourceTileCount = _collectedResourceTiles.Count;
        _collectedResourceTiles.UnionWith(resourceTiles);

        if (oldResourceTileCount != _collectedResourceTiles.Count)
        {
            EmitSignal(SignalName.ResourceTilesUpdated, _collectedResourceTiles.Count);
        }

        EmitSignal(SignalName.GridStateUpdated);
    }

    private void UpdateAttackTiles(BuildingComponent buildingComponent)
    {
        if (!buildingComponent.BuildingResource.IsAttackBuilding()) return;

        var tileArea = buildingComponent.GetTileArea();
        var newAttackTiles = GetTilesInRadius(tileArea, buildingComponent.BuildingResource.AttackRadius, _ => true).ToHashSet();
        _attackBuildingToTiles[buildingComponent] = newAttackTiles;
        _attackTiles.UnionWith(newAttackTiles);
    }

    private void RecalculateGrid()
    {
        _occupiedTiles.Clear();
        _validBuildableTiles.Clear();
        _validBuildableAttackTiles.Clear();
        _allTilesInBuildingRadius.Clear();
        _collectedResourceTiles.Clear();
        _dangerOccupiedTiles.Clear();
        _attackTiles.Clear();
        _buildingToBuildableTiles.Clear();
        _dangerBuildingToTiles.Clear();
        _attackBuildingToTiles.Clear();

        var buildingComponents = BuildingComponent.GetValidBuildingComponents(this);

        foreach (var buildingComponent in buildingComponents)
        {
            UpdateBuildingComponentGridState(buildingComponent);
        }

        CheckDangerBuildingDestruction();

        EmitSignal(SignalName.ResourceTilesUpdated, _collectedResourceTiles.Count);
        EmitSignal(SignalName.GridStateUpdated);
    }

    private void RecalculateDangerOccupiedTiles()
    {
        _dangerOccupiedTiles.Clear();
        var dangerBuildings = BuildingComponent.GetDangerBuildingComponents(this);
        foreach (var building in dangerBuildings)
        {
            UpdateDangerOccupiedTiles(building);
        }
    }

    private void CheckDangerBuildingDestruction()
    {
        var dangerBuildings = BuildingComponent.GetDangerBuildingComponents(this);
        foreach (var building in dangerBuildings)
        {
            var tileArea = building.GetTileArea();
            var isInsideAttackTile = tileArea.ToTiles().Any(_attackTiles.Contains);

            if (isInsideAttackTile)
            {
                building.Disable();
            }
            else
            {
                building.Enable();
            }
        }
    }

    private bool IsTileInsideCircle(Vector2 centerPosition, Vector2 tilePosition, float radius)
    {
        var distanceX = centerPosition.X - (tilePosition.X + 0.5f);
        var distanceY = centerPosition.Y - (tilePosition.Y + 0.5f);
        return distanceX * distanceX + distanceY * distanceY <= radius * radius;
    }

    private List<Vector2I> GetTilesInRadius(Rect2I tileArea, int radius, Func<Vector2I, bool> filterFn)
    {
        List<Vector2I> result = [];
        var tileAreaF = tileArea.ToRect2F();
        var tileAreaCenter = tileAreaF.GetCenter();
        var radiusMod = Mathf.Max(tileAreaF.Size.X, tileAreaF.Size.Y) / 2f;

        for (var x = tileArea.Position.X - radius; x < tileArea.End.X + radius; x++)
        {
            for (var y = tileArea.Position.Y - radius; y < tileArea.End.Y + radius; y++)
            {
                Vector2I tilePosition = new(x, y);
                if (!IsTileInsideCircle(tileAreaCenter, tilePosition, radius + radiusMod) || !filterFn(tilePosition)) continue;
                result.Add(tilePosition);
            }
        }

        return result;
    }

    private List<Vector2I> GetValidTilesInRadius(Rect2I tileArea, int radius)
    {
        return GetTilesInRadius(tileArea, radius, (tilePosition) => GetTileCustomData(tilePosition, IS_BUILDABLE).Item2);
    }

    private List<Vector2I> GetResourceTilesInRadius(Rect2I tileArea, int radius)
    {
        return GetTilesInRadius(tileArea, radius, (tilePosition) => GetTileCustomData(tilePosition, IS_WOOD).Item2);
    }

    private void UpdateBuildingComponentGridState(BuildingComponent buildingComponent)
    {
        UpdateDangerOccupiedTiles(buildingComponent);
        UpdateValidBuildableTiles(buildingComponent);
        UpdateCollectedResourceTiles(buildingComponent);
        UpdateAttackTiles(buildingComponent);
    }

    private void OnBuildingPlaced(BuildingComponent buildingComponent)
    {
        UpdateBuildingComponentGridState(buildingComponent);
        CheckDangerBuildingDestruction();
    }

    private void OnBuildingDestroyed(BuildingComponent buildingComponent)
    {
        RecalculateGrid();
    }

    private void OnBuildingDisabled(BuildingComponent buildingComponent)
    {
        RecalculateGrid();
    }

    private void OnBuildingEnabled(BuildingComponent buildingComponent)
    {
        UpdateBuildingComponentGridState(buildingComponent);
    }
}