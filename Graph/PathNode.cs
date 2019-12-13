using System;
using System.Collections.Generic;
using System.Text;

namespace AiCup2019.Graph
{
    class PathNode : IComparable
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public bool canStand { get; private set; }
        public bool canFallDown { get; private set; }
        public int jumpTicksLeft { get; private set; }
        public PathNode previousNode { get; private set; }
        public int pathFromStart { get; private set; }
        public int pathToFinish { get; private set; }
        public int fullPathCost { get => pathFromStart + pathToFinish; }

        public int depth { get; private set; }

        public int bigPathNodeDepth { get; private set; }

        public float commandSpeed { get; private set; }
        public bool commandJump { get; private set; }
        public bool commandJumpDown { get; private set; }

        public PathNode(int x, int y, bool canStand, bool canFallDown, int jumpTicksLeft, int pathFromStart, int pathToFinish, int depth, int bigPathNodeDepth,
            PathNode previousNode = null, float commandSpeed = 0f, bool commandJump = false, bool commandJumpDown = false)
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
            this.bigPathNodeDepth = bigPathNodeDepth;
            this.commandSpeed = commandSpeed;
            this.commandJump = commandJump;
            this.commandJumpDown = commandJumpDown;
        }

        public int CompareTo(object obj)
        {
            return fullPathCost.CompareTo((obj as PathNode).fullPathCost);
        }

        public void ChangeTo(PathNode other)
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
            bigPathNodeDepth = other.bigPathNodeDepth;
            commandSpeed = other.commandSpeed;
            commandJump = other.commandJump;
            commandJumpDown = other.commandJumpDown;
        }
    }
}
