using System;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.Pathfinding;
using StardewValley;
using System.Net;

namespace AutomaticStardew
{
    internal sealed class ModEntry : Mod
    {
        private static readonly int[] OreNodeIDs = { 751, 290, 764, 765 };
        private PathFindController? pathController;
        private ModConfig Config;
        public override void Entry(IModHelper helper)
        {
            this.Monitor.Log("AutomaticStardew is running!", LogLevel.Info);
            this.Config = this.Helper.ReadConfig<ModConfig>();
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        }

        //private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        //{
        //    if (!Context.IsWorldReady) return;

        //    if (e.Button == this.Config.FindOreKey)
        //    {
        //        var closest = GetClosestOreNodePosition();
        //        if (closest == null)
        //        {
        //            Monitor.Log("Nenhum nó de minério encontrado.", LogLevel.Info);
        //            return;
        //        }

        //        Monitor.Log($"Movendo jogador até o nó em tile {closest}", LogLevel.Info);
        //        MoveToOreNode(closest.Value); // Fix: Use the already checked `closest` variable
        //    }
        //}

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || e.Button != Config.FindOreKey)
                return;

            Monitor.Log($"Key pressed: {e.Button}", LogLevel.Debug);
            Farmer player = Game1.player;
            GameLocation location = Game1.currentLocation;

            Vector2? nodeMostClose = GetClosestOreNodePosition();
            Point nodeMostClosePoint;
            if (nodeMostClose == null)
            {
                Monitor.Log("Nenhum nó de minério encontrado.", LogLevel.Info);
                return;
            }
            else
            {
                nodeMostClosePoint = new Point((int)nodeMostClose.Value.X + 1, (int)nodeMostClose.Value.Y);
            }
                // Calcula o tile alvo (um bloco à direita)
            Rectangle bbox = player.GetBoundingBox();
            Point currentTile = new Point(
                bbox.Center.X / Game1.tileSize,
                bbox.Center.Y / Game1.tileSize
            );
            Point endPoint = new Point((int)currentTile.X + 1, (int)currentTile.Y);
            int finalFacingDirection = player.FacingDirection;

            // Inicializa o pathfind usando o construtor da versão 1.6
            player.controller = new PathFindController(player, location, nodeMostClosePoint, finalFacingDirection);
        }

        private List<Vector2> GetOreNodePositions()
        {
            GameLocation location = Game1.currentLocation;
            return location.Objects.Pairs
                .Where((KeyValuePair<Vector2, StardewValley.Object> kvp) => OreNodeIDs.Contains(kvp.Value.ParentSheetIndex))
                .Select(kvp => kvp.Key)
                .ToList();
        }

        private Vector2? GetClosestOreNodePosition()
        {
            var playerPosition = Game1.player.getLocalPosition(Game1.viewport);
            var positions = GetOreNodePositions();
            Monitor.Log($"Found {positions.Count} ore nodes", LogLevel.Debug);

            if (positions.Count == 0)
                return null;

            return positions
                .OrderBy(pos => Vector2.Distance(playerPosition, pos))
                .FirstOrDefault();
        }


        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            Farmer player = Game1.player;
            var controller = player.controller as PathFindController;
            if (controller == null)
                return;

            // Chama o método update da versão 1.6 passando o GameTime atual
            bool finished = controller.update(Game1.currentGameTime);
            if (finished)
                player.controller = null;
        }

        private void MoveToOreNode(Vector2? targetTile)
        {
            if (targetTile == null)
            {
                this.Monitor.Log("Nenhum nó de minério encontrado.", LogLevel.Info);
                return;
            }
    
            Point endTile = new Point((int)targetTile.Value.X, (int)targetTile.Value.Y);

            this.pathController = new PathFindController(
                c: Game1.player,
                location: Game1.currentLocation,
                endPoint: endTile,
                finalFacingDirection: 2,
                endBehaviorFunction: (c, endTile) =>
                {
                    this.Monitor.Log($"Chegou no nó de minério em tile {endTile}", LogLevel.Info);
                    this.pathController = null;
                }
            );
        }

    }
}
