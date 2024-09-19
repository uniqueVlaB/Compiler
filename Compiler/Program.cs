using Compiler;

class Program
{
    static void Main(string[] args)
    {
        while (true)
        {
           
            Lexer lexer = new Lexer();
            Parser parser = new Parser();
            
            Console.WriteLine("Enter an arithmetic expression:");
            string? input = Console.ReadLine();
            if (input == "exit") return;

            List<(string message, Range position)> errors = new List<(string message, Range position)>();
            try
            {
                // make a list of tokens from an expression
                var tokens = lexer.Tokenize(input, ref errors);

                //display list of tokens
                //foreach (var token in tokens)
                //{
                //    Console.WriteLine($"[{token.token}, {token.position}, {token.type}] ");
                //}

                //parse tokens
                errors = parser.Parse(tokens, errors);

                //display result
                if (errors.Count == 0)
                    Console.WriteLine("No syntax errors found.");
                else
                {
                    Console.Clear();
                    for (int i = 0; i < input.Length; i++)
                    {
                        Console.ResetColor();
                        (string message, Range position) err = errors.FirstOrDefault(err => err.position.Start.Value == i);
                        if (err.message != null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            for (int j = i; j <= err.position.End.Value; j++)
                            {
                                Console.Write(input[j]);
                            }
                            i = err.position.End.Value;
                        }
                        else
                        {
                            Console.Write(input[i]);
                        }

                    }
                    Console.ResetColor();
                    Console.WriteLine();
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

