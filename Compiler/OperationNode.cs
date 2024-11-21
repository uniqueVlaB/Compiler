using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public class OperationNode
    {
        public string Value { get; set; }
        public int Cost { get; set; }
        public OperationNode? Left { get; set; }
        public OperationNode? Right { get; set; }
        public OperationNode? Parent { get; set; }
        public int Depth { get; set; }
        public int Id { get; set; }
        public int queuedCPUid { get; set; }
        public bool executed { get; set; } = false;

        public OperationNode(string value, int cost, int depth, int id)
        {
            Value = value;
            Depth = depth;
            Id = id;
            Cost = cost;
            Left = null;
            Right = null;
            Parent = null;
        }
    }

}
