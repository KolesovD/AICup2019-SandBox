using System;
using System.Collections.Generic;
using System.Text;

namespace AiCup2019.Graph
{
    class BigNode
    {
        public bool canMoveThrough { get; private set; }
        public bool canStand { get; private set; }
        public bool isGrounded { get; private set; }
        public bool canFallDown { get; private set; }

        public BigNode()
        {
            canMoveThrough = false;
            canStand = false;
            isGrounded = false;
            canFallDown = false;
        }

        public BigNode(bool canMoveThrough, bool canStand, bool isGrounded, bool canFallDown)
        {
            this.canMoveThrough = canMoveThrough;
            this.canStand = canStand;
            this.isGrounded = isGrounded;
            this.canFallDown = canFallDown;
        }
    }
}
