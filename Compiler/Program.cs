using Compiler;

class Program
{
    static void Main(string[] args)
    {
        while (true)
        {
            Lexer lexer = new Lexer();
            Parser parser = new Parser();
            TreeBuilder treeBuilder = new TreeBuilder();
            Optimizer optimizer = new Optimizer();

            Console.WriteLine("Enter an arithmetic expression:");
            string? input = Console.ReadLine();
            if (input == "exit") return;

            List<(string message, Range position)> errors = new List<(string message, Range position)>();
            try
            {
                // Робимо лексичний аналіз
                var tokens = lexer.Tokenize(input, ref errors);

                // Проводимо синтаксичний аналіз
                errors = parser.Parse(tokens, errors);

                // Виводимо результат
                if (errors.Count == 0)
                {
                    Console.WriteLine("No syntax errors found.");

                    var pks = new GridPKS(3);
                    var trees = new OperationNode[6];

                    Console.WriteLine("===Base tokens===");
                    trees[0] = pks.ConvertToOperationTree(treeBuilder.BuildTree(tokens), tokens);
                    Console.WriteLine();

                    Console.WriteLine("===Commutative===");
                    var tmpTokens = optimizer.PerformCommutative(tokens);
                    trees[1] = pks.ConvertToOperationTree(treeBuilder.BuildTree(tmpTokens), tokens);
                    Console.WriteLine();

                    Console.WriteLine("===Distributive===");
                    tmpTokens = optimizer.PerformDistibutive(tokens);
                    trees[2] = pks.ConvertToOperationTree(treeBuilder.BuildTree(tmpTokens), tokens);
                    Console.WriteLine();

                    Console.WriteLine("===Distributive + Reverse Distributive===");
                    tmpTokens = optimizer.PerformContraction(optimizer.PerformDistibutive(tokens));
                    trees[3] = pks.ConvertToOperationTree(treeBuilder.BuildTree(tmpTokens), tokens);
                    Console.WriteLine();

                    Console.WriteLine("===Optimize===");
                    tmpTokens = optimizer.OptimizeExpression(tokens);
                    trees[4] = pks.ConvertToOperationTree(treeBuilder.BuildTree(tmpTokens), tokens);
                    Console.WriteLine();

                    Console.WriteLine("===Commutative + Distributive + Optimize===");
                    tmpTokens = optimizer.OptimizeExpression(optimizer.PerformDistibutive(optimizer.PerformCommutative(tokens)));
                    trees[5] = pks.ConvertToOperationTree(treeBuilder.BuildTree(tmpTokens), tokens);
                    Console.WriteLine();

                    foreach (var message in optimizer.optimizationsLog)
                    {
                        Console.WriteLine(message);
                    }
                    pks.Compare(trees);
                }
                else
                {
                    Console.Clear();
                    foreach (var error in errors)
                        Console.WriteLine(error.message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.Write("\n\n\n");
        }
    }
}
