using System;
using System.Collections.Generic;
using System.Text;

namespace MazeRunner.Dtos
{
    public class GameResponse
    {
        public GameDefinition Game { get; set; }
        public MazeBlockView MazeBlockView { get; set; }
    }
}
