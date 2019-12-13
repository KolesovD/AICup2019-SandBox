using AiCup2019.Model;
using System;
using System.Collections.Generic;

namespace AiCup2019.Graph
{
    class PathGraph
    {
        BigNode[,] nodeMap;

        public static readonly int ticksForJump = 33;
        public static readonly float maxSpeed = 10f;

        private static readonly int tickScale = 6;
        private static readonly int bigTicksNumberOfJump = 5;

        private static readonly int hvCost = 10;
        private static readonly int diagonalCost = 14;

        private static readonly int maxDepth = 50;

        private int levelXLength;
        private int levelYLength;

        private List<BigPathNode> bigPath = null;

        private Vec2Double previousTargetPosition;

        private Vec2Double unitSize;

        public PathGraph(Tile[][] tiles, Vec2Double unitSize)
        {
            levelXLength = tiles.Length;
            levelYLength = tiles[0].Length;

            this.unitSize = unitSize;

            nodeMap = new BigNode[levelXLength, levelYLength];

            for (int x = 0; x < levelXLength; x++)
            {
                nodeMap[x, 0] = new BigNode();

                for (int y = 1; y < levelYLength; y++)
                {
                    if (tiles[x][y] == Tile.Wall)
                        nodeMap[x, y] = new BigNode();
                    else
                    {
                        bool canMoveThrough = true;
                        int ceilingY = (int)Math.Ceiling(y + unitSize.Y);
                        if (ceilingY < levelYLength)
                        {
                            for (int subY = y; subY <= ceilingY; subY++)
                            {
                                if (tiles[x][subY] == Tile.Wall)
                                    canMoveThrough = false;
                            }
                        }
                        else canMoveThrough = false;

                        if (!canMoveThrough)
                            nodeMap[x, y] = new BigNode();
                        else
                        {
                            bool canStand = false;
                            bool isGround = false;
                            bool canFallDown = true;

                            if (tiles[x][y - 1] == Tile.Wall || tiles[x][y - 1] == Tile.JumpPad)
                            {
                                isGround = true;
                                canFallDown = false;
                            }
                            else if (tiles[x][y - 1] == Tile.Platform || tiles[x][y - 1] == Tile.Ladder)
                            {
                                isGround = true;
                            }

                            if (tiles[x][y] == Tile.Ladder)
                            {
                                canStand = true;
                            }

                            nodeMap[x, y] = new BigNode(canMoveThrough, canStand, isGround, canFallDown);
                        }
                    }
                }
            }
        }

        #region BigPath

        public void GenerateBigPathRoute(Vec2Double from, Vec2Double to, int jumpTicksLeft = 0)
        {
            List<BigPathNode> closedNodes = new List<BigPathNode>();
            List<BigPathNode> openNodes = new List<BigPathNode>();

            int bigJumpTicks = jumpTicksLeft % tickScale > 0 ? jumpTicksLeft / tickScale + 1 : jumpTicksLeft / tickScale;

            int startX = (int)from.X;
            int startY = (int)from.Y;

            int finishX = (int)to.X;
            int finishY = (int)to.Y;

            BigPathNode startNode = CreateBigNodeFromCoordinates(startX, startY, finishX, finishY, null, 0, bigJumpTicks);
            openNodes.Add(startNode);

            BigPathNode finishNode = null;

            while ((openNodes.Count > 0) && (finishNode == null))
            {
                openNodes.Sort();
                BigPathNode nextNode = openNodes[0];
                openNodes.RemoveAt(0);

                finishNode = AddBigNeighbors(nextNode, openNodes, closedNodes, finishX, finishY);
                closedNodes.Add(nextNode);
            }

            previousTargetPosition = to;

            if (finishNode == null)
                bigPath = null;
            else
            {
                bigPath = new List<BigPathNode>(finishNode.depth + 1);

                while (finishNode != null)
                {
                    bigPath.Add(finishNode);
                    finishNode = finishNode.previousNode;
                }
                bigPath.Reverse();
            }
        }

        private BigPathNode CreateBigNodeFromCoordinates(int x, int y, int finishX, int finishY, BigPathNode previousNode = null, int deltaPath = 0, int jumpTicksLeft = 0)
        {
            bool nodeCanStand = false;
            if (nodeMap[x, y].canStand || nodeMap[x, y].isGrounded)
                nodeCanStand = true;
            bool nodeCanFallDown = false;
            if (nodeMap[x, y].canFallDown)
                nodeCanFallDown = true;
            int nodeJumpTicksLeft;
            if (nodeCanStand)
                nodeJumpTicksLeft = bigTicksNumberOfJump;
            else nodeJumpTicksLeft = jumpTicksLeft;

            return new BigPathNode(x, y, nodeCanStand, nodeCanFallDown, nodeJumpTicksLeft, previousNode == null ? 0 : previousNode.pathFromStart + deltaPath, Math.Abs(finishX - x) * 10 + Math.Abs(finishY - y) * 10, previousNode == null ? 0 : previousNode.depth + 1, previousNode);
        }

        private BigPathNode AddBigNeighbors(BigPathNode node, List<BigPathNode> openNodes, List<BigPathNode> closedNodes, int finishX, int finishY)
        {
            List<BigPathNode> nodeNeighbors = new List<BigPathNode>();

            int nodeX = node.X;
            int nodeY = node.Y;

            BigPathNode nextNode;

            int nextY;
            int nextX = nodeX + 1;
            if (nodeX != levelXLength - 1)
            {
                if (nodeMap[nextX, nodeY].canMoveThrough)
                {
                    nextY = nodeY + 1;
                    //rightTop
                    if (nodeY != levelYLength - 1)
                        if (node.canStand || node.jumpTicksLeft > 0)
                        {
                            nextNode = CreateBigNodeFromCoordinates(nextX, nextY, finishX, finishY, node, diagonalCost, node.canStand ? bigTicksNumberOfJump - 1 : node.jumpTicksLeft - 1);
                            BigPathNode finalNode = FinishingNode(nextNode, openNodes, closedNodes, nodeNeighbors, finishX, finishY);
                            if (finalNode != null)
                                return finalNode;
                        }

                    nextY = nodeY;
                    //right
                    if (node.canStand)
                    {
                        nextNode = CreateBigNodeFromCoordinates(nextX, nextY, finishX, finishY, node, hvCost);
                        BigPathNode finalNode = FinishingNode(nextNode, openNodes, closedNodes, nodeNeighbors, finishX, finishY);
                        if (finalNode != null)
                            return finalNode;
                    }

                    nextY = nodeY - 1;
                    //rightBottom
                    if (nodeY != 0)
                        if (node.canFallDown)
                        {
                            nextNode = CreateBigNodeFromCoordinates(nextX, nextY, finishX, finishY, node, diagonalCost);
                            BigPathNode finalNode = FinishingNode(nextNode, openNodes, closedNodes, nodeNeighbors, finishX, finishY);
                            if (finalNode != null)
                                return finalNode;
                        }
                }
            }

            nextX = nodeX;
            nextY = nodeY + 1;
            //top
            if (nodeY != levelYLength - 1)
                if ((node.canStand || node.jumpTicksLeft > 0) && nodeMap[nextX, nextY].canMoveThrough)
                {
                    nextNode = CreateBigNodeFromCoordinates(nextX, nextY, finishX, finishY, node, diagonalCost, node.canStand ? bigTicksNumberOfJump - 1 : node.jumpTicksLeft - 1);
                    BigPathNode finalNode = FinishingNode(nextNode, openNodes, closedNodes, nodeNeighbors, finishX, finishY);
                    if (finalNode != null)
                        return finalNode;
                }

            nextY = nodeY - 1;
            //bottom
            if (nodeY != 0)
                if (node.canFallDown && nodeMap[nextX, nextY].canMoveThrough)
                {
                    nextNode = CreateBigNodeFromCoordinates(nextX, nextY, finishX, finishY, node, diagonalCost);
                    BigPathNode finalNode = FinishingNode(nextNode, openNodes, closedNodes, nodeNeighbors, finishX, finishY);
                    if (finalNode != null)
                        return finalNode;
                }

            nextX = nodeX - 1;
            if (nodeX != 0)
            {
                if (nodeMap[nextX, nodeY].canMoveThrough)
                {
                    nextY = nodeY + 1;
                    //leftTop
                    if (nodeY != (levelYLength * 6) - 1)
                        if (node.canStand || node.jumpTicksLeft > 0)
                        {
                            nextNode = CreateBigNodeFromCoordinates(nextX, nextY, finishX, finishY, node, diagonalCost, node.canStand ? bigTicksNumberOfJump - 1 : node.jumpTicksLeft - 1);
                            BigPathNode finalNode = FinishingNode(nextNode, openNodes, closedNodes, nodeNeighbors, finishX, finishY);
                            if (finalNode != null)
                                return finalNode;
                        }

                    nextY = nodeY;
                    //left
                    if (node.canStand)
                    {
                        nextNode = CreateBigNodeFromCoordinates(nextX, nextY, finishX, finishY, node, hvCost);
                        BigPathNode finalNode = FinishingNode(nextNode, openNodes, closedNodes, nodeNeighbors, finishX, finishY);
                        if (finalNode != null)
                            return finalNode;
                    }

                    nextY = nodeY - 1;
                    //leftBottom
                    if (nodeY != 0)
                        if (node.canFallDown)
                        {
                            nextNode = CreateBigNodeFromCoordinates(nextX, nextY, finishX, finishY, node, diagonalCost);
                            BigPathNode finalNode = FinishingNode(nextNode, openNodes, closedNodes, nodeNeighbors, finishX, finishY);
                            if (finalNode != null)
                                return finalNode;
                        }
                }
            }

            openNodes.AddRange(nodeNeighbors);
            return null;
        }

        private BigPathNode FinishingNode(BigPathNode nextNode, List<BigPathNode> openNodes, List<BigPathNode> closedNodes, List<BigPathNode> nodeNeighbors, int finishX, int finishY)
        {
            int nextX = nextNode.X;
            int nextY = nextNode.Y;
            if ((nextX == finishX) && (nextY == finishY))
                return nextNode;
            BigPathNode findNode = FindNode(nextX, nextY, openNodes);
            if (findNode == null)
            {
                findNode = FindNode(nextX, nextY, closedNodes);
                if (findNode == null)
                    nodeNeighbors.Add(nextNode);
            }
            else
            {
                if (nextNode.fullPathCost < findNode.fullPathCost)
                    findNode.ChangeTo(nextNode);
            }
            return null;
        }

        private BigPathNode FindNode(int x, int y, List<BigPathNode> nodes)
        {
            foreach (BigPathNode bpn in nodes)
            {
                if ((bpn.X == x) && (bpn.Y == y))
                {
                    return bpn;
                }
            }
            return null;
        }

        #endregion

        #region SmallPath

        public PathNode GeneratePath(Vec2Double from, Vec2Double to, int jumpTicksLeft = 0)
        {
            if (bigPath == null)
                return null;

            List<PathNode> closedNodes = new List<PathNode>();
            List<PathNode> openNodes = new List<PathNode>();

            int startX = (int)(from.X * 6);
            int startY = (int)(from.Y * 6);

            int finishX = (int)(to.X * 6);
            int finishY = (int)(to.Y * 6);

            PathNode startNode = CreateNodeFromCoordinates(startX, startY, finishX, finishY, null, 0, jumpTicksLeft);
            openNodes.Add(startNode);

            PathNode finishNode = null;

            while ((openNodes.Count > 0) && (finishNode == null))
            {
                openNodes.Sort();
                PathNode nextNode = openNodes[0];
                openNodes.RemoveAt(0);

                finishNode = AddNeighbors(nextNode, openNodes, closedNodes, finishX, finishY);
                closedNodes.Add(nextNode);
            }

            previousTargetPosition = to;

            return finishNode;
        }

        private PathNode CreateNodeFromCoordinates(int x, int y, int finishX, int finishY, PathNode previousNode = null, int deltaPath = 0, int jumpTicksLeft = 0,
            float commandSpeed = 0f, bool commandJump = false, bool commandJumpDown = false)
        {
            int nodeX = (int)(x / 6d);
            int nodeY = (int)(y / 6d);
            bool nodeCanStand = false;
            if (nodeMap[nodeX, nodeY].canStand || (nodeMap[nodeX, nodeY].isGrounded && (y % 6 == 0)))
                nodeCanStand = true;
            bool nodeCanFallDown = false;
            if (nodeMap[nodeX, nodeY].canFallDown || (y % 6 != 0))
                nodeCanFallDown = true;
            int nodeJumpTicksLeft;
            if (nodeCanStand)
                nodeJumpTicksLeft = ticksForJump;
            else nodeJumpTicksLeft = jumpTicksLeft;

            int currentBigPathDepth = 0;
            if(previousNode != null)
            {
                currentBigPathDepth = previousNode.bigPathNodeDepth;
                if (currentBigPathDepth < (bigPath.Count - 1) && nodeX == bigPath[currentBigPathDepth + 1].X && nodeY == bigPath[currentBigPathDepth + 1].Y)
                {
                    currentBigPathDepth++;
                }
            }

            return new PathNode(x, y, nodeCanStand, nodeCanFallDown, nodeJumpTicksLeft, previousNode == null ? 0 : previousNode.pathFromStart + deltaPath,
                Math.Abs(finishX - x) * 10 + Math.Abs(finishY - y) * 10, previousNode == null ? 0 : previousNode.depth + 1, currentBigPathDepth, previousNode,
                commandSpeed, commandJump, commandJumpDown);
        }

        private PathNode AddNeighbors(PathNode node, List<PathNode> openNodes, List<PathNode> closedNodes, int finishX, int finishY)
        {
            List<PathNode> nodeNeighbors = new List<PathNode>();

            int nodeX = node.X;
            int nodeY = node.Y;

            int pathMaxDepth = bigPath.Count - 1;

            int subFinishX = node.bigPathNodeDepth >= pathMaxDepth ? finishX : bigPath[node.bigPathNodeDepth + 1].X * 6 + 3;
            int subFinishY = node.bigPathNodeDepth >= pathMaxDepth ? finishY : bigPath[node.bigPathNodeDepth + 1].Y * 6 + 3;

            double unitHalfSizeX = unitSize.X / 2d;

            List<PathNode> tempPathNodes = new List<PathNode>();

            int nextY;
            int nextX = nodeX + 1;
            if (nodeX != (levelXLength * 6) - 1)
            {
                if (nodeMap[(int)((nextX / 6d) + unitHalfSizeX), (int)(nodeY / 6d)].canMoveThrough)
                {
                    nextY = nodeY + 1;
                    //rightTop
                    if (nodeY != (levelYLength * 6) - 1)
                        if ((node.canStand || node.jumpTicksLeft > 0) && nodeMap[(int)(nextX / 6d), (int)(nextY / 6d)].canMoveThrough)
                            tempPathNodes.Add(CreateNodeFromCoordinates(nextX, nextY, subFinishX, subFinishY, node, diagonalCost, node.canStand ? ticksForJump - 1 : node.jumpTicksLeft - 1,
                                maxSpeed, true, false));

                    nextY = nodeY;
                    //right
                    if (node.canStand)
                        tempPathNodes.Add(CreateNodeFromCoordinates(nextX, nextY, subFinishX, subFinishY, node, hvCost, 0, maxSpeed, false, false));

                    nextY = nodeY - 1;
                    //rightBottom
                    if (nodeY != 0)
                        if (node.canFallDown && nodeMap[(int)(nextX / 6d), (int)(nextY / 6d)].canMoveThrough)
                            tempPathNodes.Add(CreateNodeFromCoordinates(nextX, nextY, subFinishX, subFinishY, node, diagonalCost, 0, maxSpeed, false, true));
                }
            }

            nextX = nodeX;
            nextY = nodeY + 1;
            //top
            if (nodeY != (levelYLength * 6) - 1)
                if ((node.canStand || node.jumpTicksLeft > 0) && nodeMap[(int)(nextX / 6d), (int)(nextY / 6d)].canMoveThrough)
                    tempPathNodes.Add(CreateNodeFromCoordinates(nextX, nextY, subFinishX, subFinishY, node, hvCost, node.canStand ? ticksForJump - 1 : node.jumpTicksLeft - 1,
                        0f, true, false));

            nextY = nodeY - 1;
            //bottom
            if (nodeY != 0)
                if (node.canFallDown && nodeMap[(int)(nextX / 6d), (int)(nextY / 6d)].canMoveThrough)
                    tempPathNodes.Add(CreateNodeFromCoordinates(nextX, nextY, subFinishX, subFinishY, node, hvCost, 0, 0f, false, true));

            nextX = nodeX - 1;
            if (nodeX != 0)
            {
                if (nodeMap[(int)((nextX / 6d) - unitHalfSizeX), (int)(nodeY / 6d)].canMoveThrough)
                {
                    nextY = nodeY + 1;
                    //leftTop
                    if (nodeY != (levelYLength * 6) - 1)
                        if ((node.canStand || node.jumpTicksLeft > 0) && nodeMap[(int)(nextX / 6d), (int)(nextY / 6d)].canMoveThrough)
                            tempPathNodes.Add(CreateNodeFromCoordinates(nextX, nextY, subFinishX, subFinishY, node, diagonalCost, node.canStand ? ticksForJump - 1 : node.jumpTicksLeft - 1, 
                                -maxSpeed, true, false));

                    nextY = nodeY;
                    //left
                    if (node.canStand)
                        tempPathNodes.Add(CreateNodeFromCoordinates(nextX, nextY, subFinishX, subFinishY, node, hvCost, 0, -maxSpeed, false, false));

                    nextY = nodeY - 1;
                    //leftBottom
                    if (nodeY != 0)
                        if (node.canFallDown && nodeMap[(int)(nextX / 6d), (int)(nextY / 6d)].canMoveThrough)
                            tempPathNodes.Add(CreateNodeFromCoordinates(nextX, nextY, subFinishX, subFinishY, node, diagonalCost, 0, -maxSpeed, false, true));
                }
            }

            foreach (PathNode pn in tempPathNodes)
            {
                if (pn.bigPathNodeDepth > node.bigPathNodeDepth)
                {
                    openNodes.Clear();
                    closedNodes.Clear();
                    nodeNeighbors.Clear();
                    openNodes.Add(pn);
                    return null;
                }
                PathNode finalNode = FinishingNode(pn, openNodes, closedNodes, nodeNeighbors, finishX, finishY);
                if (finalNode != null)
                    return finalNode;
            }

            openNodes.AddRange(nodeNeighbors);
            return null;
        }

        private PathNode FinishingNode(PathNode nextNode, List<PathNode> openNodes, List<PathNode> closedNodes, List<PathNode> nodeNeighbors, int finishX, int finishY)
        {
            int nextX = nextNode.X;
            int nextY = nextNode.Y;
            if (((nextX == finishX) && (nextY == finishY)) || nextNode.depth > maxDepth)
                return nextNode;
            PathNode findNode = FindNode(nextX, nextY, openNodes);
            if (findNode == null)
            {
                findNode = FindNode(nextX, nextY, closedNodes);
                if (findNode == null)
                    nodeNeighbors.Add(nextNode);
            }
            else
            {
                if (nextNode.fullPathCost < findNode.fullPathCost)
                    findNode.ChangeTo(nextNode);
            }
            return null;
        }

        private PathNode FindNode(int x, int y, List<PathNode> nodes)
        {
            foreach (PathNode pn in nodes)
            {
                if ((pn.X == x) && (pn.Y == y))
                {
                    return pn;
                }
            }
            return null;
        }

        #endregion

        #region Drawing
        public void DrawNodes(Debug debug)
        {
            int levelXLength = nodeMap.GetLength(0);
            int levelYLength = nodeMap.GetLength(1);
            for (int x = 0; x < levelXLength; x++)
            {
                for (int y = 0; y < levelYLength; y++)
                {
                    if (nodeMap[x, y] != null)
                    {
                        if (!nodeMap[x, y].canMoveThrough)
                            debug.Draw(new CustomData.Rect(new Vec2Float(x + 0.4f, y + 0.4f), new Vec2Float(0.2f, 0.2f), new ColorFloat(0.1f, 0.1f, 0.1f, 0.9f)));
                        else if (nodeMap[x, y].isGrounded)
                            debug.Draw(new CustomData.Rect(new Vec2Float(x + 0.4f, y + 0.4f), new Vec2Float(0.2f, 0.2f), new ColorFloat(0.9f, 0.0f, 0.0f, 0.9f)));
                        else if (nodeMap[x, y].canStand)
                            debug.Draw(new CustomData.Rect(new Vec2Float(x + 0.4f, y + 0.4f), new Vec2Float(0.2f, 0.2f), new ColorFloat(0.0f, 0.9f, 0.0f, 0.9f)));
                        else debug.Draw(new CustomData.Rect(new Vec2Float(x + 0.4f, y + 0.4f), new Vec2Float(0.2f, 0.2f), new ColorFloat(0.9f, 0.9f, 0.9f, 0.9f)));
                        if (nodeMap[x, y].canFallDown)
                            debug.Draw(new CustomData.Rect(new Vec2Float(x + 0.4f, y + 0.4f), new Vec2Float(0.2f, 0.2f), new ColorFloat(0.9f, 0.9f, 0.9f, 0.9f)));
                    }
                }
            }
        }

        public void DrawPathNode(PathNode node, Debug debug)
        {
            PathNode nextNode = node;
            float ss = 0;
            while (nextNode != null)
            {
                debug.Draw(new CustomData.Rect(new Vec2Float(nextNode.X / 6f, nextNode.Y / 6f), new Vec2Float(0.1f, 0.1f), new ColorFloat(0.9f, 0.9f, 0.9f, 0.9f)));
                nextNode = nextNode.previousNode;
                ss += 0.2f;
            }
            if (bigPath != null)
            {
                foreach (BigPathNode bpn in bigPath)
                {
                    debug.Draw(new CustomData.Rect(new Vec2Float(bpn.X + 0.4f, bpn.Y + 0.4f), new Vec2Float(0.2f, 0.2f), new ColorFloat(0.9f, 0.9f, 0.9f, 0.9f)));
                    debug.Draw(new CustomData.PlacedText(bpn.depth.ToString(), new Vec2Float(bpn.X + 0.65f, bpn.Y + 0.4f), TextAlignment.Left, 10, new ColorFloat(0.9f, 0.9f, 0.9f, 0.9f)));
                }
            }
        }

        #endregion
    }
}
