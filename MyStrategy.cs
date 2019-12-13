using AiCup2019.Model;
using AiCup2019.Graph;
using System;
using System.Collections.Generic;

namespace AiCup2019
{
    public class MyStrategy
    {
        //Initial and const block
        bool initialized = false;
        int levelXLength = 40;
        int levelYLength = 40;
        double levelDiagonal = 4;
        double playerMaxHorSpeed = 1;
        double playerMaxJumpSpeed = 1;
        double playerMaxJumpTime = 1;
        int playerMaxHealth = 20;

        int playerRadius = 6;
        int playerBazookaRadius = 10;

        PathGraph pathGraph;

        //To remember
        int? nextTargetId;
        int currentNumberOfJumpTickLeft = 0;

        //Readonly const
        readonly ColorFloat transparentViolet = new ColorFloat(0.8f, 0, 0.8f, 0.2f);
        readonly ColorFloat transparentGreen = new ColorFloat(0, 0.8f, 0, 0.2f);
        readonly int ticksToOneUnit = 6; //Speed = 10, ticks = 6

        Stack<PathNode> pathNodes = new Stack<PathNode>();

        readonly int findPathCD = 20;
        int currentFindPathCD = 0;
        PathNode lastPathNode;
        Vec2Double lastTargetPosition;

        static double DistanceSquare(Vec2Double a, Vec2Double b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
        }

        public UnitAction GetAction(Unit unit, Game game, Debug debug)
        {
            if (!initialized)
                InitialSetup(unit, game, debug);

            UnitAction action = new UnitAction();
            action.PlantMine = false;

            Unit? targetUnit = GetNextTargetById(game.Units, unit);
            if (!targetUnit.HasValue)
                targetUnit = FindNextTarget(game.Units, unit);

            bool hasDirectSightLine = false;
            if (unit.Weapon.HasValue)
            {
                hasDirectSightLine = CheckDirectSightLine(new Vec2Double(unit.Position.X, unit.Position.Y + unit.Size.Y / 2d), new Vec2Double(targetUnit.Value.Position.X, targetUnit.Value.Position.Y + targetUnit.Value.Size.Y / 2d), game.Level.Tiles, debug/*, true*/);
                //DrawSpread(unit, unit.Weapon.Value, debug, hasDirectSightLine);
            }

            //Find Target Position
            Vec2Double? targetPosition = null;

            if (unit.Health <= playerMaxHealth / 2)
            {
                targetPosition = FindNearest(unit.Position, game, (LootBox lb) => { return lb.Item is Item.HealthPack; });
            }
            else if (!unit.Weapon.HasValue)
            {
                targetPosition = FindNearest(unit.Position, game, (LootBox lb) => { return lb.Item is Item.Weapon; });
            }
            
            if (!targetPosition.HasValue && targetUnit.HasValue)
            {
                targetPosition = targetUnit.Value.Position;
                if (DistanceSquare(unit.Position, targetPosition.Value) < 1d)
                    action.PlantMine = true;

                Vec2Double? radiusPosition = FindNearestRadius(unit.Position, targetPosition.Value, unit.Weapon.Value.Typ == WeaponType.RocketLauncher ? playerBazookaRadius : playerRadius, game.Level.Tiles, debug);
                if (radiusPosition.HasValue)
                    targetPosition = radiusPosition;
            }
            else targetPosition = lastTargetPosition;

            if (targetPosition.HasValue)
                lastTargetPosition = targetPosition.Value;
            ///////////////////////////

            //PathFinding
            if (currentFindPathCD <= 0 && targetPosition.HasValue)
            {
                GeneratePath(unit.Position, targetPosition.Value);
            }

            currentFindPathCD--;

            //pathGraph.DrawPathNode(lastPathNode, debug);
            ///////////////////////////


            LootBox? nearestWeapon = null;
            foreach (var lootBox in game.LootBoxes)
            {
                if (lootBox.Item is Item.Weapon)
                {
                    if (!nearestWeapon.HasValue || DistanceSquare(unit.Position, lootBox.Position) < DistanceSquare(unit.Position, nearestWeapon.Value.Position))
                    {
                        nearestWeapon = lootBox;
                    }
                }
            }

            Vec2Double aim = new Vec2Double(0, 0);
            if (targetUnit.HasValue)
            {
                aim = new Vec2Double(targetUnit.Value.Position.X - unit.Position.X, targetUnit.Value.Position.Y - unit.Position.Y);
            }
            action.Aim = aim;
            //debug.Draw(new CustomData.Log("Aim  X:" + aim.X + "  Y:" + aim.Y));

            if (pathNodes.Count > 0)
            {
                PathNode pn = pathNodes.Pop();
                action.Velocity = pn.commandSpeed;
                action.Jump = pn.commandJump;
                action.JumpDown = pn.commandJumpDown;
                currentNumberOfJumpTickLeft = pn.jumpTicksLeft;
                //debug.Draw(new CustomData.Log("Depth: " + pn.depth + "  Velocity: " + action.Velocity + "  Jump: " + action.Jump + "  Down: " + action.JumpDown));
            }
            else if (!hasDirectSightLine && targetPosition.HasValue)
            {
                bool jump = targetPosition.Value.Y > unit.Position.Y;
                if (targetPosition.Value.X > unit.Position.X && game.Level.Tiles[(int)(unit.Position.X + 1)][(int)(unit.Position.Y)] == Tile.Wall)
                {
                    jump = true;
                }
                if (targetPosition.Value.X < unit.Position.X && game.Level.Tiles[(int)(unit.Position.X - 1)][(int)(unit.Position.Y)] == Tile.Wall)
                {
                    jump = true;
                }
                action.Velocity = Math.Sign(targetPosition.Value.X - unit.Position.X) * PathGraph.maxSpeed;
                action.Jump = jump;
                action.JumpDown = !jump;
            }
            else
            {
                action.Velocity = 0;
                action.Jump = false;
                action.JumpDown = false;
            }

            if (hasDirectSightLine || (unit.Weapon.HasValue && unit.Weapon.Value.Magazine < unit.Weapon.Value.Parameters.MagazineSize))
            {
                action.Shoot = true;
                if (unit.Weapon.Value.Parameters.Explosion.HasValue)
                    if (unit.Weapon.Value.Spread > 0.3d && Math.Abs(aim.Y / aim.X) > 2d)
                        action.Shoot = false;
            }
            else action.Shoot = false;

            action.SwapWeapon = false;
            action.Reload = false;

            return action;
        }

        private void GeneratePath(Vec2Double unitPosition, Vec2Double targetPosition)
        {
            pathGraph.GenerateBigPathRoute(unitPosition, targetPosition, currentNumberOfJumpTickLeft);
            PathNode path = pathGraph.GeneratePath(unitPosition, targetPosition, currentNumberOfJumpTickLeft);
            if (path != null)
            {
                lastPathNode = path;
                pathNodes.Clear();
                while (path != null && path.previousNode != null)
                {
                    pathNodes.Push(path);
                    path = path.previousNode;
                }
            }
            currentFindPathCD = findPathCD;
        }

        private Vec2Double? FindNearest(Vec2Double unitPosition, Game game, Predicate<LootBox> predicate)
        {
            LootBox? lootBox = null;
            foreach (LootBox lb in game.LootBoxes)
            {
                if (predicate(lb))
                {
                    if (!lootBox.HasValue || DistanceSquare(unitPosition, lb.Position) < DistanceSquare(unitPosition, lootBox.Value.Position))
                    {
                        lootBox = lb;
                    }
                }
            }

            if (lootBox.HasValue)
                return lootBox.Value.Position;
            else return null;
        }

        private void InitialSetup(Unit unit, Game game, Debug debug)
        {
            levelXLength = game.Level.Tiles.Length;
            levelYLength = game.Level.Tiles[0].Length;
            levelDiagonal = Math.Sqrt(levelXLength * levelXLength + levelYLength * levelYLength);
            playerMaxHorSpeed = game.Properties.UnitMaxHorizontalSpeed;
            playerMaxJumpSpeed = game.Properties.UnitJumpSpeed;
            playerMaxJumpTime = game.Properties.UnitJumpTime;
            playerMaxHealth = game.Properties.UnitMaxHealth;

            FindNextTarget(game.Units, unit);

            pathGraph = new PathGraph(game.Level.Tiles, unit.Size);

            initialized = true;
        }

        private bool CheckDirectSightLine(Vec2Double from, Vec2Double to, Tile[][] tiles, Debug debug, bool debuging = false)
        {
            double deltaX = from.X - to.X;
            double deltaY = from.Y - to.Y;
            double tang = deltaY / deltaX;

            bool directSightLine = true;

            if (Math.Abs(tang) < 1d)
            {
                double startXPos;
                int intStartYPos;

                if (from.X < to.X)
                {
                    startXPos = from.X;
                    intStartYPos = (int)from.Y;
                }
                else
                {
                    startXPos = to.X;
                    intStartYPos = (int)to.Y;
                }
                int intStartXPos = (int)startXPos;
                int intEndPos = (int)Math.Abs(deltaX);

                for (int i = 0; (i < intEndPos) && (i < levelXLength - 1); i++)
                {
                    int yTile = (int)(tang * (i + 0.99d + intStartXPos - startXPos)) + intStartYPos;
                    if (debuging) debug.Draw(new CustomData.Rect(new Vec2Float(intStartXPos + i + 0.5f, yTile + 0.5f), new Vec2Float(0.5f, 0.5f), new ColorFloat(0.9f, 0.9f, 0.9f, 0.9f)));
                    if (tiles[intStartXPos + i][yTile] == Tile.Wall)
                    {
                        directSightLine = false;
                        if (!debuging) break;
                    }/*

                    yTile = (int)(tang * (i + 1.01d + intStartXPos - startXPos)) + intStartYPos;
                    if (debuging) debug.Draw(new CustomData.Rect(new Vec2Float(intStartXPos + i + 0.5f, yTile + 0.5f), new Vec2Float(0.5f, 0.5f), new ColorFloat(0.9f, 0.9f, 0.9f, 0.9f)));
                    if (tiles[intStartXPos + i + 1][yTile] == Tile.Wall)
                    {
                        directSightLine = false;
                        if (!debuging) break;
                    }*/
                }
            }

            else if (Math.Abs(tang) >= 1d)
            {
                double startYPos;
                int intStartXPos;

                if (from.Y < to.Y)
                {
                    startYPos = from.Y;
                    intStartXPos = (int)from.X;
                }
                else
                {
                    startYPos = to.Y;
                    intStartXPos = (int)to.X;
                }
                int intStartYPos = (int)startYPos;
                int intEndPos = (int)Math.Abs(deltaY);

                for (int i = 0; (i < intEndPos) && (i < levelYLength - 1); i++)
                {
                    int xTile = (int)((i + 0.99d + intStartYPos - startYPos) / tang) + intStartXPos;
                    if (debuging) debug.Draw(new CustomData.Rect(new Vec2Float(xTile + 0.5f, intStartYPos + i + 0.5f), new Vec2Float(0.5f, 0.5f), new ColorFloat(0.9f, 0.9f, 0.9f, 0.9f)));
                    if (tiles[xTile][intStartYPos + i] == Tile.Wall)
                    {
                        directSightLine = false;
                        if (!debuging) break;
                    }/*

                    xTile = (int)((i + 1.01d + intStartYPos - startYPos) / tang) + intStartXPos;
                    if (debuging) debug.Draw(new CustomData.Rect(new Vec2Float(xTile + 0.5f, intStartYPos + i + 0.5f), new Vec2Float(0.5f, 0.5f), new ColorFloat(0.9f, 0.9f, 0.9f, 0.9f)));
                    if (tiles[xTile][intStartYPos + i + 1] == Tile.Wall)
                    {
                        directSightLine = false;
                        if (!debuging) break;
                    }*/
                }
            }

            return directSightLine;
        }

        private Vec2Double? FindNearestRadius(Vec2Double unitPos, Vec2Double targetPos, int radius, Tile[][] tiles, Debug debug)
        {
            int targetX = (int) targetPos.X;
            int targetY = (int) targetPos.Y;

            int currentX = 0;
            int currentY = 0;
            double currentDistance = double.MaxValue;

            int startX = Math.Max(targetX - radius, 0);
            int startY = Math.Max(targetY - radius, 0);
            int finishX = Math.Min(targetX + radius, levelXLength);
            int finishY = Math.Min(targetY + radius, levelYLength);

            for (int x = startX; x <= finishX; x++)
            {
                for (int y = startY; y <= finishY; y++)
                {
                    if (Math.Abs(x - targetX) + Math.Abs(y - targetY) == radius)
                    {
                        Vec2Double tilePos = new Vec2Double(x + 0.5d, y + 0.5d);
                        if ((currentX == 0 && currentY == 0) || (DistanceSquare(tilePos, unitPos) < currentDistance && CheckDirectSightLine(unitPos, targetPos, tiles, debug)))
                        {
                            currentX = x;
                            currentY = y;
                            currentDistance = DistanceSquare(tilePos, unitPos);
                        }
                    }
                }
            }

            if (currentX == 0 && currentY == 0)
                return null;
            else return new Vec2Double(currentX + 0.5d, currentY + 0.5d);
        }

        private Unit? FindNextTarget(Unit[] units, Unit playersUnit)
        {
            Unit? choosedUnit = null;
            foreach (Unit unit in units)
            {
                if (unit.PlayerId != playersUnit.PlayerId)
                {
                    if (!choosedUnit.HasValue || DistanceSquare(playersUnit.Position, unit.Position) < DistanceSquare(playersUnit.Position, choosedUnit.Value.Position))
                    {
                        choosedUnit = unit;
                    }
                }
            }

            if (choosedUnit.HasValue)
            {
                nextTargetId = choosedUnit.Value.Id;
                return choosedUnit;
            }
            else
            {
                nextTargetId = null;
                return null;
            }
        }

        private Unit? GetNextTargetById(Unit[] units, Unit playersUnit)
        {
            foreach (Unit unit in units)
            {
                if (unit.PlayerId != playersUnit.PlayerId && unit.Id == nextTargetId)
                {
                    return unit;
                }
            }
            return null;
        }

        private void DrawSpread(Unit unit, Weapon weapon, Debug debug, bool directSightLine = false)
        {
            if (weapon.LastAngle.HasValue)
            {
                double angle = weapon.LastAngle.Value;
                double spread = weapon.Spread;
                double X0Pos = unit.Position.X;
                double Y0Pos = unit.Position.Y + unit.Size.Y / 2d;

                double XLineOfSight = X0Pos + levelDiagonal * Math.Cos(angle);
                double YLineOfSight = Y0Pos + levelDiagonal * Math.Sin(angle);

                double X1Pos = X0Pos + levelDiagonal * Math.Cos(angle + spread);
                double Y1Pos = Y0Pos + levelDiagonal * Math.Sin(angle + spread);

                double X2Pos = X0Pos + levelDiagonal * Math.Cos(angle - spread);
                double Y2Pos = Y0Pos + levelDiagonal * Math.Sin(angle - spread);

                //debug.Draw(new CustomData.Log("playerMaxHorSpeed:" + playerMaxHorSpeed + "   playerMaxJumpSpeed:" + playerMaxJumpSpeed + "   playerMaxJumpTime:" + playerMaxJumpTime));

                //debug.Draw(new CustomData.Log("Ammo:" + weapon.Magazine + "/" + weapon.Parameters.MagazineSize));

                debug.Draw(new CustomData.Log("Spread:" + spread));

                //debug.Draw(new CustomData.Line(new Vec2Float((float)X0Pos, (float)Y0Pos), new Vec2Float((float)XLineOfSight, (float)YLineOfSight), 0.1f, new ColorFloat(1, 1, 1, 0.8f)));

                ColorFloat color = directSightLine ? transparentGreen : transparentViolet;
                ColoredVertex[] coloredVertexArray = new ColoredVertex[3];
                coloredVertexArray[0] = new ColoredVertex(new Vec2Float((float)X0Pos, (float)Y0Pos), color);
                coloredVertexArray[1] = new ColoredVertex(new Vec2Float((float)X1Pos, (float)Y1Pos), color);
                coloredVertexArray[2] = new ColoredVertex(new Vec2Float((float)X2Pos, (float)Y2Pos), color);
                CustomData.Polygon polygon = new CustomData.Polygon(coloredVertexArray);
                debug.Draw(polygon);
            }
        }
    }
}