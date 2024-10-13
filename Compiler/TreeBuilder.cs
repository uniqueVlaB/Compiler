using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using System.Diagnostics;
using System.Globalization;

namespace Compiler
{
    public class TreeBuilder
    {
        public List<string> optimizationsLog = new List<string>();

        public TreeNode BuildTree(List<(string token, Range position, TokenType type)> tokens)
        {
            optimizationsLog.Clear();
            tokens = OptimizeExpression(tokens);
            var postfix = ConvertToPostfix(tokens);
            var tree = CreateTreeFromPostfix(postfix);
            return tree;
        }

        public List<(string token, Range position, TokenType type)> OptimizeExpression(List<(string token, Range position, TokenType type)> tokens)
        {

            if (tokens[0].token == "-")
            {
                tokens.Insert(0, ("0", new Range(), TokenType.Number));
                optimizationsLog.Add("Added 0 before unary minus at the beginning");
            }

            bool optimized;
            do
            {
                optimized = false;

                for (int i = 0; i < tokens.Count; i++)
                {
                    #region addZeroBeforeMinus(-A)
                    if (tokens[i].type == TokenType.OpenParenthesis && tokens[i + 1].token == "-")
                    {
                        tokens.Insert(i + 1, ("0", new Range(), TokenType.Number));
                        optimized = true;
                        optimizationsLog.Add("Added 0 between '(' and '-' to avoid error");
                    }
                    #endregion
                    #region remove-0*x
                    
                    if (tokens[i].token == "0" && i >= 3 && tokens[i - 1].token == "*")
                    {
                        if (tokens[i - 2].type == TokenType.Variable || tokens[i + 2].type == TokenType.Number)
                        {
                            tokens.RemoveRange(i - 3, 2);
                            optimized = true;
                            optimizationsLog.Add("Simplifyed multiplication by 0");
                        }
                        else if (tokens[i - 2].type == TokenType.CloseParenthesis)
                        {
                            var closes = 1;
                            var end = i - 1;
                            var start = 0;
                            for (int j = i - 3; j >= 0; j--)
                            {
                                if (tokens[j].type == TokenType.CloseParenthesis)
                                {
                                    closes++;
                                }
                                else if (tokens[j].type == TokenType.OpenParenthesis)
                                {
                                    closes--;
                                }

                                if (closes == 0)
                                {
                                    start = j;
                                    optimized = true;
                                    break;
                                }
                            }
                            tokens.RemoveRange(start, end - start + 1);
                            optimizationsLog.Add("Simplifyed multiplication by 0");
                        }
                    }
                    else if (tokens[i].token == "0" && i + 2 < tokens.Count && tokens[i + 1].token == "*")
                    {
                        if (tokens[i + 2].type == TokenType.Variable || tokens[i + 2].type == TokenType.Number)
                        {
                            tokens.RemoveRange(i + 1, 2);
                            optimizationsLog.Add("Simplifyed multiplication by 0");
                            optimized = true;
                        }
                        else if (tokens[i + 2].type == TokenType.OpenParenthesis)
                        {
                            var opens = 1;
                            var start = i + 1;
                            var end = 0;
                            for (int j = i + 3; j < tokens.Count; j++)
                            {
                                if (tokens[j].type == TokenType.OpenParenthesis)
                                {
                                    opens++;
                                }
                                else if (tokens[j].type == TokenType.CloseParenthesis)
                                {
                                    opens--;
                                }

                                if (opens == 0)
                                {
                                    end = j;
                                    optimized = true;
                                    break;
                                }
                            }
                            tokens.RemoveRange(start, end - start + 1);
                            optimizationsLog.Add("Simplifyed multiplication by 0");
                        }
                    }
                    #endregion
                    #region const*const
                    if (i + 2 < tokens.Count && tokens[i].type == TokenType.Number && tokens[i + 1].type == TokenType.Operator && tokens[i + 2].type == TokenType.Number)
                    {
                        double result;
                        double left = double.Parse(tokens[i].token, CultureInfo.InvariantCulture);
                        double right = double.Parse(tokens[i + 2].token, CultureInfo.InvariantCulture);

                        switch (tokens[i + 1].token)
                        {
                            case "+":
                                result = left + right;
                                break;
                            case "-":
                                result = left - right;
                                break;
                            case "*":
                                result = left * right;
                                break;
                            case "/":
                                result = left / right;
                                break;
                            default: throw new Exception("unknown operator");
                        }
                        optimizationsLog.Add($"Performed: {left} {tokens[i + 1].token} {right} = {result}");
                        tokens[i] = (result.ToString(), new Range(), TokenType.Number);
                        tokens.RemoveRange(i + 1, 2);
                        optimized = true;

                    }
                    #endregion
                    #region remove-1*x
                    if (i + 2 < tokens.Count && tokens[i].token == "1" && tokens[i + 1].token == "*")
                    {
                        if (tokens[i + 2].type == TokenType.Variable || tokens[i + 2].type == TokenType.Number || tokens[i + 2].type == TokenType.OpenParenthesis)
                        {
                            tokens.RemoveRange(i, 2);
                            optimized = true;
                            optimizationsLog.Add("Removed unnecsessary multiplication by 1");
                        }
                    }
                    else if (tokens[i].token == "1" && i >= 2 && tokens[i - 1].token == "*")
                    {
                        if (tokens[i - 2].type == TokenType.Variable || tokens[i - 2].type == TokenType.Number || tokens[i - 2].type == TokenType.CloseParenthesis)
                        {
                            tokens.RemoveRange(i - 1, 2);
                            optimized = true;
                            optimizationsLog.Add("Removed unnecsessary multiplication by 1");
                        }
                    }
                    #endregion
                    #region remove-1/x
                    if (tokens[i].token == "1" && i >= 2 && tokens[i - 1].token == "/")
                    {
                        if (tokens[i - 2].type == TokenType.Variable || tokens[i - 2].type == TokenType.Number || tokens[i - 2].type == TokenType.CloseParenthesis)
                        {
                            tokens.RemoveRange(i - 1, 2);
                            optimized = true;
                            optimizationsLog.Add("Removed unnecsessary division by 1");
                            break;
                        }
                    }
                    #endregion
                    #region remove-0/x
                    if (tokens[i].token == "0" && i + 2 < tokens.Count && tokens[i + 1].token == "/")
                    {
                        if (tokens[i + 2].type == TokenType.Variable || tokens[i + 2].type == TokenType.Number)
                        {
                            tokens.RemoveRange(i + 1, 2);
                            optimized = true;
                            optimizationsLog.Add("Removed 0/x case");
                        }
                        else if (tokens[i + 2].type == TokenType.OpenParenthesis)
                        {
                            var opens = 1;
                            var start = i + 1;
                            var end = 0;
                            for (int j = i + 3; j < tokens.Count; j++)
                            {
                                if (tokens[j].type == TokenType.OpenParenthesis)
                                {
                                    opens++;
                                }
                                else if (tokens[j].type == TokenType.CloseParenthesis)
                                {
                                    opens--;
                                }

                                if (opens == 0)
                                {
                                    end = j;
                                    optimized = true;
                                    break;
                                }
                            }
                            tokens.RemoveRange(start, end - start + 1);
                            optimizationsLog.Add("Removed 0/x case");
                        }
                    }
                    #endregion
                    #region throw-x/0
                    if (tokens[i].token == "/" && tokens[i + 1].token == "0") throw new DivideByZeroException();
                    #endregion
                }
            } while (optimized);

            foreach (var token in tokens)
            {
                Console.Write(token.token);
            }
            Console.WriteLine();
            return tokens;
        }

        private bool IsOperand(string token)
        {
            return double.TryParse(token, out _) || !IsOperator(token);
        }

        private bool IsOperator(string token)
        {
            return token == "+" || token == "-" || token == "*" || token == "/";
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