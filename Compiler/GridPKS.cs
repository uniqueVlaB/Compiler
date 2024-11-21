using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using System.Diagnostics;

namespace Compiler
{
    public class GridPKS
    {
        private int[,] CPUs = new int[0, 0];
        private static readonly HashSet<string> SupportedOperations = new HashSet<string> { "+", "-", "*", "/" };
        private Dictionary<string, int> opCost = new Dictionary<string, int>
        {
            ["+"] = 2,
            ["-"] = 3,
            ["*"] = 4,
            ["/"] = 8,
        };
        private int _idCounter = 0;
        private string[,] ExecuteMatrix = new string[200, 9];
        private int sequentialCycles = 0;


        public GridPKS(int size)
        {
            CPUs = new int[size, size];

            int value = 1;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    CPUs[i, j] = value++;
                }
            }
            Console.WriteLine("\nCPUs topology:\n");
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    Console.Write(CPUs[i, j]);
                    if (j != 2) Console.Write("-");
                }
                Console.WriteLine();
                if (i != 2)
                    Console.WriteLine("| | |");
            }
        }
        public void DrawTree(OperationNode root)
        {
            if (root == null) return;

            var graph = new AdjacencyGraph<OperationNode, Edge<OperationNode>>();

            AddNodesToGraph(graph, root);

            var graphviz = new GraphvizAlgorithm<OperationNode, Edge<OperationNode>>(graph);

            graphviz.FormatVertex += (sender, args) =>
            {
                args.VertexFormatter.Label = $"op{args.Vertex.Id}\\nDepth: {args.Vertex.Depth}\\n{args.Vertex.Value}";
                args.VertexFormatter.Shape = GraphvizVertexShape.Ellipse;
            };

            string outputDot = graphviz.Generate();

            string dotFilePath = "operation_tree.dot";
            File.WriteAllText(dotFilePath, outputDot);

            var convProcess = new Process
            {
                StartInfo = new ProcessStartInfo(@"dot")
                {
                    Arguments = $"-Tpng operation_tree.dot -o operation_tree.png",
                    UseShellExecute = true
                }
            };
            convProcess.Start();
            convProcess.WaitForExit();

            new Process
            {
                StartInfo = new ProcessStartInfo(@"operation_tree.png")
                {
                    UseShellExecute = true
                }
            }.Start();
        }
        private void AddNodesToGraph(AdjacencyGraph<OperationNode, Edge<OperationNode>> graph, OperationNode node)
        {
            if (node == null) return;

            graph.AddVertex(node);

            if (node.Left != null)
            {
                graph.AddVertex(node.Left);
                graph.AddEdge(new Edge<OperationNode>(node, node.Left));
                AddNodesToGraph(graph, node.Left);
            }

            if (node.Right != null)
            {
                graph.AddVertex(node.Right);
                graph.AddEdge(new Edge<OperationNode>(node, node.Right));
                AddNodesToGraph(graph, node.Right);
            }

            if (node.Parent != null)
            {
                graph.AddEdge(new Edge<OperationNode>(node, node.Parent)); // Додаємо зворотній зв'язок
            }
        }
        public OperationNode? ConvertToOperationTree(TreeNode expressionTree)
        {
            return ConvertNode(expressionTree, null, 0);
        }
        private OperationNode? ConvertNode(TreeNode? currentNode, OperationNode? parent, int depth)
        {
            if (currentNode == null)
                return null;

            // Перевіряємо, чи є поточний вузол операцією
            if (!SupportedOperations.Contains(currentNode.Value))
            {
                // Якщо вузол — не операція, обробляємо дочірні вузли
                return currentNode.Left != null ? ConvertNode(currentNode.Left, parent, depth) : null;
            }

            // Створюємо вузол операції з унікальним ідентифікатором
            var operationNode = new OperationNode(currentNode.Value, opCost[currentNode.Value], depth, _idCounter++)
            {
                Parent = parent
            };

            // Рекурсивно обробляємо ліве та праве піддерева
            operationNode.Left = ConvertNode(currentNode.Left, operationNode, depth + 1);
            operationNode.Right = ConvertNode(currentNode.Right, operationNode, depth + 1);

            return operationNode;
        }
        public Dictionary<int, List<OperationNode>> GetNodesByLevel(OperationNode root)
        {
            var result = new Dictionary<int, List<OperationNode>>();
            TraverseTree(root, 0, result);
            return result;
        }
        private void TraverseTree(OperationNode? currentNode, int currentDepth, Dictionary<int, List<OperationNode>> result)
        {
            if (currentNode == null)
                return;

            // Додаємо вузол до відповідного рівня
            if (!result.ContainsKey(currentDepth))
            {
                result[currentDepth] = new List<OperationNode>();
            }

            result[currentDepth].Add(currentNode);

            // Рекурсивно обходимо ліве і праве піддерево
            TraverseTree(currentNode.Left, currentDepth + 1, result);
            TraverseTree(currentNode.Right, currentDepth + 1, result);
        }
        private void queueOperations(Dictionary<int, List<OperationNode>> levels)
        {
            foreach (var level in levels)
            {
                int cpuID = 1;
                level.Value.ForEach(x => x.queuedCPUid = cpuID++);
            }
        }
        private List<int> FindShortestPath(int startProcessor, int endProcessor)
        {
            int size = CPUs.GetLength(0);
            var directions = new (int, int)[]
            {
            (-1, 0), // Вверх
            (1, 0),  // Вниз
            (0, -1), // Вліво
            (0, 1)   // Вправо
            };

            // Знайдемо координати початкового та кінцевого процесорів
            (int startX, int startY) = FindProcessorCoordinates(CPUs, startProcessor);
            (int endX, int endY) = FindProcessorCoordinates(CPUs, endProcessor);

            if (startX == -1 || endX == -1)
                throw new ArgumentException("Один з процесорів не знайдено.");

            // BFS
            var queue = new Queue<(int x, int y, List<int> path)>();
            var visited = new HashSet<(int, int)>();

            queue.Enqueue((startX, startY, new List<int> { startProcessor }));
            visited.Add((startX, startY));

            while (queue.Count > 0)
            {
                var (x, y, path) = queue.Dequeue();

                // Якщо досягнуто кінцевого процесора
                if (x == endX && y == endY)
                {
                    return path;
                }

                // Перевіряємо сусідів
                foreach (var (dx, dy) in directions)
                {
                    int newX = x + dx;
                    int newY = y + dy;

                    if (IsValid(newX, newY, size) && !visited.Contains((newX, newY)))
                    {
                        visited.Add((newX, newY));
                        var newPath = new List<int>(path) { CPUs[newX, newY] };
                        queue.Enqueue((newX, newY, newPath));
                    }
                }
            }

            // Якщо шляху немає
            return new List<int> { -1 };
        }

        private static (int, int) FindProcessorCoordinates(int[,] CPUs, int processor)
        {
            int size = CPUs.GetLength(0);

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (CPUs[i, j] == processor)
                        return (i, j);
                }
            }

            return (-1, -1); // Не знайдено
        }

        private static bool IsValid(int x, int y, int size)
        {
            return x >= 0 && x < size && y >= 0 && y < size;
        }

        private int PerformTransfers(List<OperationNode> ops, int cycle)
        {
            Console.WriteLine("\nCycle: " + cycle);
            int maxCycles = 0;
            foreach (var op in ops)
            {
                var way = new List<int>();
                for (int j = 0; j < 2; j++)
                {
                    switch (j)
                    {
                        case 0:
                            if (op.Left == null) continue;
                            else
                            {
                                way = FindShortestPath(op.Left.queuedCPUid, op.queuedCPUid);
                                Console.WriteLine($"op{op.Left.Id}: " + string.Join("->", way));
                            }
                            break;
                        case 1:
                            if (op.Right == null) continue;
                            else
                            {
                                way = FindShortestPath(op.Right.queuedCPUid, op.queuedCPUid);
                                Console.WriteLine($"op{op.Right.Id}: " + string.Join("->", way));
                            }
                            break;
                    }

                    if (way.Count < 2) continue;

                    if (way.Count > maxCycles) maxCycles = way.Count;

                    for (int i = 0; i < way.Count - 1; i++)
                    {
                        int matrixRow = cycle + i - 1;
                        int matrixColumn = way[i] - 1;

                        if (matrixRow >= 0 && matrixRow < ExecuteMatrix.GetLength(0) &&
                            matrixColumn >= 0 && matrixColumn < ExecuteMatrix.GetLength(1))
                        {
                            switch (j)
                            {
                                case 0:
                                    ExecuteMatrix[matrixRow, matrixColumn] = $"o{op.Left.Id}|P{op.Left.queuedCPUid}|P{op.queuedCPUid}";
                                    break;
                                case 1:
                                    ExecuteMatrix[matrixRow, matrixColumn] = $"o{op.Right.Id}|P{op.Right.queuedCPUid}|P{op.queuedCPUid}";
                                    break;
                            }
                        }
                        else
                        {
                            throw new IndexOutOfRangeException("Індекс виходить за межі ExecuteMatrix.");
                        }
                    }

                }

            }

            return cycle + maxCycles - 1;
        }


        private int PerformTransfersWithSync(List<OperationNode> ops, int cycle)
        {
            Console.WriteLine("\nCycle: " + cycle);
            bool hasTransfers = true;
            int totalCycles = 0;

            // Створюємо чергу передач для кожної операції
            var transferQueues = new Dictionary<int, Queue<(int source, int target, string operation)>>();

            // Ініціалізуємо чергу передач для всіх операцій
            foreach (var op in ops)
            {
                if (op.Left != null)
                {
                    var path = FindShortestPath(op.Left.queuedCPUid, op.queuedCPUid);
                    Console.WriteLine($"op{op.Left.Id}: " + string.Join("->", path));
                    foreach (var segment in GetPathSegments(path, $"o{op.Left.Id}"))
                    {
                        if (!transferQueues.ContainsKey(segment.target))
                            transferQueues[segment.target] = new Queue<(int, int, string)>();
                        transferQueues[segment.target].Enqueue(segment);
                    }
                }
                if (op.Right != null)
                {
                    var path = FindShortestPath(op.Right.queuedCPUid, op.queuedCPUid);
                    Console.WriteLine($"op{op.Right.Id}: " + string.Join("->", path));
                    foreach (var segment in GetPathSegments(path, $"o{op.Right.Id}"))
                    {
                        if (!transferQueues.ContainsKey(segment.target))
                            transferQueues[segment.target] = new Queue<(int, int, string)>();
                        transferQueues[segment.target].Enqueue(segment);
                    }
                }
            }

            // Поки є передачі, виконуємо їх потактово
            while (hasTransfers)
            {
                hasTransfers = false;
                var busyProcessors = new HashSet<int>(); // Процесори, зайняті в цьому циклі

                // Обробляємо всі черги передач
                foreach (var target in transferQueues.Keys.ToList())
                {
                    if (transferQueues[target].Count > 0)
                    {
                        var (source, dest, operation) = transferQueues[target].Peek();
                        if (!busyProcessors.Contains(source) && !busyProcessors.Contains(dest))
                        {
                            busyProcessors.Add(source);
                            busyProcessors.Add(dest);

                            // Записуємо передачу в ExecuteMatrix
                            int matrixRow = cycle - 1;
                            int matrixColumn = source - 1;

                            if (matrixRow >= 0 && matrixRow < ExecuteMatrix.GetLength(0) &&
                                matrixColumn >= 0 && matrixColumn < ExecuteMatrix.GetLength(1))
                            {
                                ExecuteMatrix[matrixRow, matrixColumn] = operation;
                            }

                            // Видаляємо виконану передачу з черги
                            transferQueues[target].Dequeue();
                            hasTransfers = true; // Все ще є активні передачі
                        }
                    }
                }

                // Збільшуємо цикл, якщо були виконані передачі
                if (hasTransfers) cycle++;
            }

            return cycle;
        }

        // Метод для розбивки шляху на сегменти
        private List<(int source, int target, string operation)> GetPathSegments(List<int> path, string operation)
        {
            var segments = new List<(int source, int target, string operation)>();
            for (int i = 0; i < path.Count - 1; i++)
            {
                segments.Add((path[i], path[i + 1], $"{operation}|P{path[i]}|P{path[i + 1]}"));
            }
            return segments;
        }
        private int ExecuteLevel(List<OperationNode> level, int cycle)
        {
            int maxCycles = 1;
            foreach (var op in level)
            {
                sequentialCycles += op.Cost;

                if (maxCycles < op.Cost) maxCycles = op.Cost;

                for (int i = cycle - 1; i < cycle - 1 + op.Cost; i++)
                {
                    ExecuteMatrix[i, op.queuedCPUid - 1] = $"op{op.Id}({op.Value})";
                }
            }
            return cycle + maxCycles;
        }
        public void Execute(OperationNode node)
        {
            var levels = GetNodesByLevel(node);
            queueOperations(levels);

            foreach (var level in levels)
            {
                Console.WriteLine($"\nLevel {level.Key}: {string.Join(", ", level.Value.Select(node => node.Value + $" (nodeID:{node.Id}, cpuID:{node.queuedCPUid})"))}");
            }

            var maxLevel = levels.Keys.Max();
            int currentCycle = 1;
            for (int i = maxLevel; i >= 0; i--)
            {
                if (currentCycle != 1) currentCycle = PerformTransfersWithSync(levels[i], currentCycle);
                //if (currentCycle != 1) currentCycle = PerformTransfers(levels[i], currentCycle);

                currentCycle = ExecuteLevel(levels[i], currentCycle);

            }
            ShowExecutionMatrix();
            CalculateEfficiency(sequentialCycles);
        }

        private void CalculateEfficiency(int sequentialCycles)
        {
            // Підрахунок загальної кількості тактів виконання
            int parallelCycles = 0;
            for (int i = 0; i < ExecuteMatrix.GetLength(0); i++)
            {
                bool hasExecution = false;
                for (int j = 0; j < ExecuteMatrix.GetLength(1); j++)
                {
                    if (!string.IsNullOrEmpty(ExecuteMatrix[i, j]))
                    {
                        hasExecution = true;
                        break;
                    }
                }
                if (hasExecution)
                {
                    parallelCycles++;
                }
            }

            // Коефіцієнт прискорення (speedup)
            double speedup = (double)sequentialCycles / parallelCycles;

            // Коефіцієнт ефективності (efficiency)
            int processorCount = CPUs.Length;
            double efficiency = speedup / processorCount;

            // Виведення результатів
            Console.WriteLine("\n=== Efficiency Results ===");
            Console.WriteLine($"Sequential Cycles: {sequentialCycles}");
            Console.WriteLine($"Parallel Cycles: {parallelCycles}");
            Console.WriteLine($"Speedup (S): {speedup:F2}");
            Console.WriteLine($"Efficiency (E): {efficiency:F2}");
        }

        public void ShowExecutionMatrix()
        {
            int rows = ExecuteMatrix.GetLength(0);
            int cols = ExecuteMatrix.GetLength(1);

            // Виведення заголовків колонок
            Console.Write("         "); // Відступ для рядків
            for (int j = 0; j < cols; j++)
            {
                Console.Write($"|{CenterText($"P{j + 1}", 9)}");
            }
            Console.WriteLine("|");

            // Виведення рядків
            for (int i = 0; i < rows; i++)
            {
                Console.Write($"T{i + 1}".PadLeft(9)); // Вирівнювання номера рядка справа
                for (int j = 0; j < cols; j++)
                {
                    string cellContent = ExecuteMatrix[i, j];
                    if (string.IsNullOrEmpty(cellContent))
                    {
                        cellContent = "x";
                    }

                    cellContent = CenterText(cellContent, 9);
                    Console.Write($"|{cellContent}");
                }
                Console.WriteLine("|");
            }
        }

        private string CenterText(string text, int width)
        {
            if (text.Length >= width)
            {
                return text.Substring(0, width);
            }

            int leftPadding = (width - text.Length) / 2;
            int rightPadding = width - text.Length - leftPadding;

            return new string(' ', leftPadding) + text + new string(' ', rightPadding);
        }
    }
}


