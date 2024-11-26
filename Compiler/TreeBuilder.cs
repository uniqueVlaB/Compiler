using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Compiler
{
    public class TreeBuilder
    {
        public TreeNode BuildTree(List<(string token, Range position, TokenType type)> tokens)
        {
            for (int i = 0; i < tokens.Count; i++) {
                if (i + 1 < tokens.Count && 
                    (tokens[i].type == TokenType.Number || tokens[i].type == TokenType.Variable) &&
                    tokens[i+1].type == TokenType.OpenParenthesis) 
                {
                    tokens.Insert(i+1, new ("*", new Range(), TokenType.Operator));
                }
            }
            var postfix = ConvertToPostfix(tokens);
            var tree = CreateTreeFromPostfix(postfix);
            return tree;
        }

        private List<string> ConvertToPostfix(List<(string token, Range position, TokenType type)> tokens)
        {
            Stack<string> stack = new Stack<string>();
            List<string> postfix = new List<string>();
            Dictionary<string, int> precedence = new Dictionary<string, int>
            {
                { "+", 1 }, { "-", 1 }, { "*", 2 }, { "/", 2 }
            };

            foreach (var (token, position, type) in tokens)
            {
                if (type == TokenType.Number || type == TokenType.Variable)
                {
                    postfix.Add(token);
                }
                else if (token == "(")
                {
                    stack.Push(token);
                }
                else if (token == ")")
                {
                    while (stack.Peek() != "(")
                    {
                        postfix.Add(stack.Pop());
                    }
                    stack.Pop();
                }
                else
                {
                    while (stack.Count > 0 && stack.Peek() != "(" && precedence[token] <= precedence[stack.Peek()])
                    {
                        postfix.Add(stack.Pop());
                    }
                    stack.Push(token);
                }
            }

            while (stack.Count > 0)
            {
                postfix.Add(stack.Pop());
            }

            return postfix;
        }
        private bool IsOperand(string token)
        {
            return double.TryParse(token, out _) || !IsOperator(token);
        }

        private bool IsOperator(string token)
        {
            return token == "+" || token == "-" || token == "*" || token == "/";
        }

        private TreeNode CreateTreeFromPostfix(List<string> postfix)
        {
            Stack<TreeNode> stack = new Stack<TreeNode>();

            foreach (string token in postfix)
            {
                if (IsOperand(token))
                {
                    stack.Push(new TreeNode(token));
                }
                else if (IsOperator(token))
                {
                    TreeNode node = new TreeNode(token);
                    node.Right = stack.Pop();
                    node.Left = stack.Pop();
                    stack.Push(node);
                }
            }

            return stack.Peek();
        }
        public void DrawTree(TreeNode root)
        {
            if (root == null) return;

            var graph = new AdjacencyGraph<TreeNode, Edge<TreeNode>>();

            AddNodesToGraph(graph, root);

            var graphviz = new GraphvizAlgorithm<TreeNode, Edge<TreeNode>>(graph);

            graphviz.FormatVertex += (sender, args) =>
            {
                args.VertexFormatter.Label = args.Vertex.Value;
                args.VertexFormatter.Shape = GraphvizVertexShape.Ellipse;
            };

            string outputDot = graphviz.Generate();

            string dotFilePath = "binary_tree.dot";
            File.WriteAllText(dotFilePath, outputDot);

            var convProcess = new Process
            {
                StartInfo = new ProcessStartInfo(@"dot")
                {
                    Arguments = $"-Tpng binary_tree.dot -o binary_tree.png",
                    UseShellExecute = true
                }
            };
            convProcess.Start();
            convProcess.WaitForExit();

            new Process
            {
                StartInfo = new ProcessStartInfo(@"binary_tree.png")
                {
                    UseShellExecute = true
                }
            }.Start();

        }

        private void AddNodesToGraph(AdjacencyGraph<TreeNode, Edge<TreeNode>> graph, TreeNode node)
        {
            if (node == null) return;

            graph.AddVertex(node);

            if (node.Left != null)
            {
                graph.AddVertex(node.Left);
                graph.AddEdge(new Edge<TreeNode>(node, node.Left));
                AddNodesToGraph(graph, node.Left);
            }

            if (node.Right != null)
            {
                graph.AddVertex(node.Right);
                graph.AddEdge(new Edge<TreeNode>(node, node.Right));
                AddNodesToGraph(graph, node.Right);
            }
        }
    }
}