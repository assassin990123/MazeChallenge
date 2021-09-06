using System;
using System.Collections.Generic;
using System.Text;

namespace MazeRunner
{
    public  class MazeGenerator
    {
        public MazeDefinition Generate(int width, int height)
        {
            // Build the maze nodes.
            MazeNode[,] nodes = MakeNodes(width, height);

            // Build the spanning tree.
            FindSpanningTree(nodes[0, 0]);

            MazeDefinition result = new MazeDefinition();
            result.Width = width;
            result.Height = height;
            result.MazeUid = Guid.NewGuid();
            for (int r = 0; r < height; r++) //y
            {
                for (int c = 0; c < width; c++) //x
                {
                    var node = nodes[r, c];
                    var walls = node.DrawWalls();
                    MazeBlockView view = new MazeBlockView();
                    view.CoordX = c;
                    view.CoordY = r;
                    view.NorthBlocked = walls[0];
                    view.SouthBlocked = walls[1];
                    view.EastBlocked=walls[2];
                    view.WestBlocked = walls[3];
                    result.Blocks.Add(view);
                }
            }

            return result;
        }

        private void FindSpanningTree(MazeNode root)
        {
            Random rand = new Random();

            // Set the root node's predecessor so we know it's in the tree.
            root.Predecessor = root;

            // Make a list of candidate links.
            List<MazeLink> links = new List<MazeLink>();

            // Add the root's links to the links list.
            foreach (MazeNode neighbor in root.Neighbors)
            {
                if (neighbor != null)
                    links.Add(new MazeLink(root, neighbor));
            }

            // Add the other nodes to the tree.
            while (links.Count > 0)
            {
                // Pick a random link.
                int link_num = rand.Next(0, links.Count);
                MazeLink link = links[link_num];
                links.RemoveAt(link_num);

                // Add this link to the tree.
                MazeNode to_node = link.ToNode;
                link.ToNode.Predecessor = link.FromNode;

                // Remove any links from the list that point
                // to nodes that are already in the tree.
                // (That will be the newly added node.)
                for (int i = links.Count - 1; i >= 0; i--)
                {
                    if (links[i].ToNode.Predecessor != null)
                        links.RemoveAt(i);
                }

                // Add to_node's links to the links list.
                foreach (MazeNode neighbor in to_node.Neighbors)
                {
                    if ((neighbor != null) && (neighbor.Predecessor == null))
                        links.Add(new MazeLink(to_node, neighbor));
                }
            }
        }

        private MazeNode[,] MakeNodes(int wid, int hgt)
        {
            MazeNode[,] nodes = new MazeNode[hgt, wid];

            for (int r = 0; r < hgt; r++)
            {
                for (int c = 0; c < wid; c++)
                {
                    nodes[r, c] = new MazeNode(r, c, wid, hgt);
                }
            }

            // Initialize the nodes' neighbors.
            for (int r = 0; r < hgt; r++)
            {
                for (int c = 0; c < wid; c++)
                {
                    if (r > 0)
                        nodes[r, c].Neighbors[MazeNode.North] = nodes[r - 1, c];
                    if (r < hgt - 1)
                        nodes[r, c].Neighbors[MazeNode.South] = nodes[r + 1, c];
                    if (c > 0)
                        nodes[r, c].Neighbors[MazeNode.West] = nodes[r, c - 1];
                    if (c < wid - 1)
                        nodes[r, c].Neighbors[MazeNode.East] = nodes[r, c + 1];
                }
            }

            // Return the nodes.
            return nodes;
        }
    }
}
