using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Files
{
    public class FL_WorldObject
    {
        public int Tile { get; set; } = int.MinValue;

        public float Points { get; set; } = float.MinValue;

        public string MainPartDef { get; set; } = string.Empty;

        public List<string> PartDefNames { get; set; } = new List<string>();
    }
}
