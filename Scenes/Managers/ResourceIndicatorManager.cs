using System.Collections.Generic;
using System.Linq;

using Game.UI;

using Godot;

namespace Game.Managers;

public partial class ResourceIndicatorManager : Node
{

    [Export] private GridManager _gridManager;
    [Export] private PackedScene _resourceIndicatorScene;

    private HashSet<Vector2I> _indicatedResourceTiles = [];
    private readonly Dictionary<Vector2I, ResourceIndicator> _tileToResourceIndicator = [];

    public override void _Ready()
    {
        _gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
    }

    private void UpdateIndicators(IEnumerable<Vector2I> newIndicatedTiles, IEnumerable<Vector2I> toRemoveTiles)
    {
        foreach (var tile in toRemoveTiles)
        {
            if (_tileToResourceIndicator.TryGetValue(tile, out var indicator))
            {
                if (IsInstanceValid(indicator))
                {
                    indicator.Destroy();
                }
                _tileToResourceIndicator.Remove(tile);
            }
        }

        foreach (var tile in newIndicatedTiles)
        {
            var indicator = _resourceIndicatorScene.Instantiate<ResourceIndicator>();
            AddChild(indicator);
            _tileToResourceIndicator[tile] = indicator;
            indicator.GlobalPosition = tile * 64;
        }
    }

    private void HandleResourceTileUpdate()
    {
        var currentResourceTiles = _gridManager.GetCollectedResourceTiles();

        var newlyIndicatedTiles = currentResourceTiles.Except(_indicatedResourceTiles);
        var toRemoveTiles = _indicatedResourceTiles.Except(currentResourceTiles);

        UpdateIndicators(newlyIndicatedTiles, toRemoveTiles);
        _indicatedResourceTiles = currentResourceTiles;
    }

    private void OnResourceTilesUpdated(int _)
    {
        Callable.From(HandleResourceTileUpdate).CallDeferred();
    }
}
