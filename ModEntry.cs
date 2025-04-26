using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.Tools;

namespace AutomaticStardew
{
    internal sealed class ModEntry : Mod
    {
        //751 Cobre
        //764 Ouro
        //146 Carvao
        //290 Ferro

        //84 Lágrima Congelada
        //80 Quartz
        private static readonly int[] OreNodeIds = { 
                                                        751, //Cobre
                                                        290, //Ferro
                                                        764, //Ouro
                                                        146  //Carvão 
                                                    };

        private PathFindController? _pathController;
        private ModConfig _config = null!;
        private int pathfindingTickCounter = 0;

        public override void Entry(IModHelper helper)
        {
            Monitor.Log("AutomaticStardew is running!", LogLevel.Info);
            _config = Helper.ReadConfig<ModConfig>();
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button != _config.FindOreKey && e.Button != _config.DebbugButton)
                return;

            if (!Context.IsWorldReady || e.Button == _config.FindOreKey)
            {
                Monitor.Log($"Key pressed: {e.Button}", LogLevel.Debug);

                var closestNode = GetClosestOreNodePosition();
                if (!closestNode.HasValue)
                {    
                    Monitor.Log("Nenhum nó de minério encontrado.", LogLevel.Info);
                    return;
                }

                var targetTile = new Point((int)closestNode.Value.X + 1, (int)closestNode.Value.Y);
                StartPathfinding(targetTile, ()=> BreakOreNode(closestNode.Value));               
                
            }

            if(e.Button == _config.DebbugButton)
            {
                Monitor.Log("Debug key pressed", LogLevel.Debug);
                GetAllObjetsOnLocation();
            }
        }

        private void StartPathfinding(Point destination, Action? onArrival = null)
        {
            var player = Game1.player;
            var location = Game1.currentLocation;
            var facing = player.FacingDirection;

            player.controller = new PathFindController(player, location, destination, facing, OnDonePathfinding);

            void OnDonePathfinding(Character c, GameLocation l)
            {
                onArrival?.Invoke();
            }
        }

        private void GetAllObjetsOnLocation()
        {
            var location = Game1.currentLocation;
            var objects = location.Objects.Pairs.ToList();
            foreach (var obj in objects)
            {
                Monitor.Log($"ID: {obj.Value.ParentSheetIndex}, " +
                            $"Position: {obj.Value.TileLocation}, " +
                            $"Name: {obj.Value.BaseName} {obj.Value.DisplayName}", LogLevel.Debug);
            }
        }

        private List<Point> GetOresNodesPositions()
        {
            return Game1.currentLocation.Objects.Pairs
                .Where(kvp => OreNodeIds.Contains(kvp.Value.ParentSheetIndex) && kvp.Value.name.Contains("Stone"))
                .Select(kvp => new Point((int)kvp.Key.X, (int)kvp.Key.Y))
                .ToList();
        }

        private Point? GetClosestOreNodePosition()
        {
            var playerPos = Game1.player.TilePoint;
            var nodes = GetOresNodesPositions();
            
            if(nodes.Count == 0)
            {
                Monitor.Log("Nenhum nó de minério encontrado.", LogLevel.Info);
                return null;
            }
            else
            {

                return nodes
                    .MinBy(pos => Math.Abs(playerPos.X - pos.X)
                                + Math.Abs(playerPos.Y - pos.Y));
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            var controller = Game1.player.controller as PathFindController;
            if (controller == null)
                return;

            // Incrementa nosso contador
            pathfindingTickCounter++;

            // Só atualiza a cada 15 ticks (15/60 segundos ≈ 0,25s)
            if (pathfindingTickCounter >= 10)
            {
                pathfindingTickCounter = 0; // Reseta

                if (controller.update(Game1.currentGameTime))
                {
                    Game1.player.controller = null;
                }
            }
        }

        private void BreakOreNode(Point tile)
        {
            var location = Game1.currentLocation;
            var player = Game1.player;

            if (location.Objects.TryGetValue(tile.ToVector2(), out StardewValley.Object obj))
            {
                if (obj.ParentSheetIndex == 751 || obj.ParentSheetIndex == 290 || obj.ParentSheetIndex == 764 || obj.ParentSheetIndex == 146)
                {
                    if (player.CurrentTool is Pickaxe pickaxe)
                    {
                        // Primeiro vira pro tile
                        var pixelPosition = new Vector2(tile.X * Game1.tileSize, tile.Y * Game1.tileSize);
                        player.faceGeneralDirection(pixelPosition, 0);
                                           
                        player.BeginUsingTool();

                        // Começar a animação de uso
                        pickaxe.beginUsing(location, (int)player.GetToolLocation().X, (int)player.GetToolLocation().Y, player);

                        // Depois executa o golpe
                        pickaxe.DoFunction(
                            location,
                            (int)player.GetToolLocation().X,
                            (int)player.GetToolLocation().Y,
                            0,
                            player
                        );
                    }
                }
            }
        }

    }
}
