namespace Compiler
{
    public class TreeNode
    {
        public string Value { get; set; }
        public TreeNode? Left { get; set; }
        public TreeNode? Right { get; set; }

        public TreeNode(string value)
        {
            Value = value;
            Left = null;
            Right = null;
        }
    }
}
