using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MazeRunner
{
    public class MazeNode
    {
        public const int North = 0;
        public const int South = North + 1;
        public const int East = South + 1;
        public const int West = East + 1;

        // The node's neighbors in order North, South, East, West.
        public MazeNode[] Neighbors = new MazeNode[4];

        // The predecessor in the spanning tree.
        public MazeNode Predecessor = null;

        // The node's bounds.
        public Rectangle Bounds;

        // Return this node's center.
        public Point Center
        {
            get
            {
                int x = Bounds.Left + Bounds.Width / 2;
                int y = Bounds.Top + Bounds.Height / 2;
                return new Point(x, y);
            }
        }

        // Constructor.
        public MazeNode(int x, int y, int wid, int hgt)
        {
            Bounds = new Rectangle(x, y, wid, hgt);
        }

        // Draw the walls that don't cross a predecessor link.
        public bool[] DrawWalls()
        {
            bool[] result = new bool[4];
            for (int side = 0; side < 4; side++)
            {
                if ((Neighbors[side] == null) ||
                    ((Neighbors[side].Predecessor != this) &&
                     (Neighbors[side] != this.Predecessor)))
                {
                    result[side] = true;
                }
                else
                {
                    result[side] = false;
                }
            }

            return result;
        }
    }
}
