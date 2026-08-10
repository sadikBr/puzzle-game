using Game.Managers;
using Game.Resources.LevelResources;
using Game.UI;

using Godot;

namespace Game;

public partial class BaseLevel : Node
{
    [Export]
    private PackedScene _levelCompleteScreenScene;
    [Export]
    private LevelDefinitionResource _levelDefinitionResource;

    private GridManager _gridManager;
    private GoldMine _goldMine;
    private GameCamera _gameCamera;
    private TileMapLayer _baseTerrainTileMapLayer;
    private Node2D _baseBuilding;
    private GameUI _gameUI;
    private BuildingManager _buildingManager;

    public override void _Ready()
    {
        _gridManager = GetNode<GridManager>("GridManager");
        _goldMine = GetNode<GoldMine>("%GoldMine");
        _gameCamera = GetNode<GameCamera>("GameCamera");
        _baseTerrainTileMapLayer = GetNode<TileMapLayer>("%BaseTerrainTileMapLayer");
        _baseBuilding = GetNode<Node2D>("%Base");
        _gameUI = GetNode<GameUI>("GameUI");
        _buildingManager = GetNode<BuildingManager>("BuildingManager");

        _buildingManager.SetStartingResourceCount(_levelDefinitionResource.StartingResourceCount);

        _gameCamera.SetBoundingRect(_baseTerrainTileMapLayer.GetUsedRect());
        _gameCamera.CenterOnPosition(_baseBuilding.GlobalPosition);

        _gridManager.GridStateUpdated += OnGridStateUpdated;
    }

    private void OnGridStateUpdated()
    {
        var goldMineTilePosition = _gridManager.ConvertWorldPositionToTilePosition(_goldMine.GlobalPosition);
        if (_gridManager.IsTilePositionInAnyBuildingRadius(goldMineTilePosition))
        {
            var levelCompleteScreen = _levelCompleteScreenScene.Instantiate<LevelCompleteScreen>();
            AddChild(levelCompleteScreen);
            _goldMine.SetActive();
            _gameUI.HideUI();
        }
    }
}