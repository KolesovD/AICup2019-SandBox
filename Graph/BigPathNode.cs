using System;
using System.Collections.Generic;
using System.Text;

namespace AiCup2019.Graph
{
    class BigPathNode : IComparable
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public bool canStand { get; private set; }
        public bool canFallDown { get; private set; }
        public int jumpTicksLeft { get; private set; }
        public BigPathNode previousNode { get; private set; }
        public int pathFromStart { get; private set; }
        public int pathToFinish { get; private set; }
        public int fullPathCost { get => pathFromStart + pathToFinish; }

        public int depth { get; private set; }

        public BigPathNode(int x, int y, bool canStand, bool canFallDown, int jumpTicksLeft, int pathFromStart, int pathToFinish, int depth, BigPathNode previousNode = null)
        {
            X = x;
            Y = y;
            this.canStand = canStand;
            this.canFallDown = canFallDown;
            this.jumpTicksLeft = jumpTicksLeft;
            this.pathFromStart = pathFromStart;
            this.pathToFinish = pathToFinish;
            this.previousNode = previousNode;
            this.depth = depth;
        }

        public int CompareTo(object obj)
        {
            return fullPathCost.CompareTo((obj as BigPathNode).fullPathCost);
        }

        public void ChangeTo(BigPathNode other)
        {
            X = other.X;
            Y = other.Y;
            canStand = other.canStand;
            canFallDown = other.canFallDown;
            jumpTicksLeft = other.jumpTicksLeft;
            previousNode = other.previousNode;
            pathFromStart = other.pathFromStart;
            pathToFinish = other.pathToFinish;
            depth = other.depth;
        }
    }
}
