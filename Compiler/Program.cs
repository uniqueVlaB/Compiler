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
                    TreeNode root = treeBuilder.BuildTree(tokens);
                    foreach (var message in treeBuilder.optimizationsLog)
                    {
                        Console.WriteLine(message);
                    } 
                    var pks = new GridPKS(3);
                    var tr = pks.ConvertToOperationTree(root);
                    treeBuilder.DrawTree(root);
                    pks.DrawTree(tr);
                    pks.Execute(tr);
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
