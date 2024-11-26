using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Compiler
{
    public class Optimizer
    {
        public List<string> optimizationsLog = new List<string>();

        public List<(string token, Range position, TokenType type)> PerformCommutative(List<(string token, Range position, TokenType type)> tokens)
        {
            var result = new List<(string token, Range position, TokenType type)>();
            result.AddRange(tokens);

            for (int i = 0; i < tokens.Count; i++)
            {
                // Шукаємо комутативні оператори та перевіряємо сусідні операнди
                if (tokens[i].type == TokenType.Operator && (tokens[i].token == "+" || tokens[i].token == "*"))
                {
                    if (i > 0 && i < tokens.Count - 1 && // Перевірка меж списку
                        (tokens[i - 1].type == TokenType.Variable || tokens[i - 1].type == TokenType.Number) && // Лівий операнд - змінна
                        (tokens[i + 1].type == TokenType.Variable || tokens[i + 1].type == TokenType.Number))   // Правий операнд - змінна
                    {
                        if (tokens.Count - i > 2 && tokens[i].token == "+" && (tokens[i + 2].token == "*" || tokens[i + 2].token == "/" || tokens[i + 2].token == "(")) continue;

                        result[i + 1] = tokens[i - 1];
                        result[i - 1] = tokens[i + 1];

                        i++;
                        i++;
                        continue;
                    }
                }

            }
            Console.WriteLine(string.Join("", result.Select(x => x.token)));
            return result;
        }
        public List<(string token, Range position, TokenType type)> PerformDistibutive(List<(string token, Range position, TokenType type)> tokens)
        {
            var result = new List<(string token, Range position, TokenType type)>();
            result.AddRange(tokens);

            bool madeChanges;
            do
            {
                madeChanges = false;
                for (int i = 0; i < result.Count - 2; i++) // Змінено умову, бо тепер "*" може бути відсутнім
                {
                    // Перевіряємо чи поточний токен може бути множником
                    if ((result[i].type == TokenType.Number || result[i].type == TokenType.Variable) &&
                        i + 1 < result.Count &&
                        ((result[i + 1].token == "*" && result[i + 2].type == TokenType.OpenParenthesis) ||
                         (result[i + 1].type == TokenType.OpenParenthesis))) // Додана перевірка на неявне множення
                    {
                        int startParenIndex = result[i + 1].type == TokenType.OpenParenthesis ? i + 1 : i + 2;
                        int closingParenIndex = FindClosingParen(result, startParenIndex);
                        if (closingParenIndex == -1) continue;

                        // Перевіряємо чи це не виклик функції (якщо перед дужкою стоїть ідентифікатор функції)
                        if (i > 0 && IsFunction(result[i - 1]))
                        {
                            continue;
                        }

                        // Отримуємо всі частини виразу всередині дужок
                        var parts = SplitExpressionParts(result, startParenIndex + 1, closingParenIndex - 1);
                        if (parts.Count > 1)
                        {
                            // Створюємо новий вираз
                            var newExpression = new List<(string token, Range position, TokenType type)>();
                            var multiplier = result[i];

                            // Додаємо перший елемент
                            newExpression.Add(multiplier);
                            newExpression.Add(("*", new Range(0, 1), TokenType.Operator));
                            newExpression.AddRange(parts[0].tokens);

                            // Додаємо решту елементів з відповідними операторами
                            for (int j = 1; j < parts.Count; j++)
                            {
                                newExpression.Add((parts[j].operation, new Range(0, 1), TokenType.Operator));
                                newExpression.Add(multiplier);
                                newExpression.Add(("*", new Range(0, 1), TokenType.Operator));
                                newExpression.AddRange(parts[j].tokens);
                            }

                            // Замінюємо стару частину новою
                            result.RemoveRange(i, closingParenIndex - i + 1);
                            result.InsertRange(i, newExpression);
                            madeChanges = true;
                            break;
                        }
                    }
                    // Додаємо перевірку на функції
                    else if (IsFunction(result[i]) &&
                             i + 1 < result.Count &&
                             result[i + 1].type == TokenType.OpenParenthesis)
                    {
                        int closingParenIndex = FindClosingParen(result, i + 1);
                        if (closingParenIndex == -1) continue;

                        // Рекурсивно застосовуємо дистрибутивний закон до аргументів функції
                        var innerTokens = result.GetRange(i + 2, closingParenIndex - (i + 2));
                        var processedInner = PerformDistibutive(innerTokens);

                        // Замінюємо аргументи функції на оброблені
                        result.RemoveRange(i + 2, closingParenIndex - (i + 2));
                        result.InsertRange(i + 2, processedInner);
                        madeChanges = true;
                        break;
                    }
                }
            } while (madeChanges);

            Console.WriteLine(string.Join("", result.Select(x => x.token)));
            return result;
        }

        private bool IsFunction((string token, Range position, TokenType type) token)
        {
            // Список відомих функцій
            string[] knownFunctions = new[] { "sin", "cos", "tan", "log", "exp", "f1", "f2", "sqrt" };
            return token.type == TokenType.Variable && knownFunctions.Contains(token.token);
        }

        private class ExpressionPart
        {
            public List<(string token, Range position, TokenType type)> tokens { get; set; }
            public string operation { get; set; }  // "+" або "-"
        }

        private List<ExpressionPart> SplitExpressionParts(
            List<(string token, Range position, TokenType type)> tokens,
            int startIndex,
            int endIndex)
        {
            var parts = new List<ExpressionPart>();
            var currentPart = new List<(string token, Range position, TokenType type)>();
            string currentOperation = "+";  // За замовчуванням для першої частини

            int i = startIndex;
            while (i <= endIndex)
            {
                if (tokens[i].type == TokenType.OpenParenthesis)
                {
                    int closingIndex = FindClosingParen(tokens, i);
                    if (closingIndex != -1)
                    {
                        // Додаємо всю частину в дужках як один елемент
                        currentPart.AddRange(tokens.GetRange(i, closingIndex - i + 1));
                        i = closingIndex + 1;
                        continue;
                    }
                }

                if ((tokens[i].token == "+" || tokens[i].token == "-") &&
                    tokens[i].type == TokenType.Operator)
                {
                    if (currentPart.Any())
                    {
                        parts.Add(new ExpressionPart
                        {
                            tokens = new List<(string, Range, TokenType)>(currentPart),
                            operation = currentOperation
                        });
                        currentPart.Clear();
                    }
                    currentOperation = tokens[i].token;
                }
                else
                {
                    currentPart.Add(tokens[i]);
                }
                i++;
            }

            if (currentPart.Any())
            {
                parts.Add(new ExpressionPart
                {
                    tokens = new List<(string, Range, TokenType)>(currentPart),
                    operation = currentOperation
                });
            }

            return parts;
        }

        private int FindClosingParen(List<(string token, Range position, TokenType type)> tokens, int openParenIndex)
        {
            int count = 1;
            for (int i = openParenIndex + 1; i < tokens.Count; i++)
            {
                if (tokens[i].type == TokenType.OpenParenthesis) count++;
                else if (tokens[i].type == TokenType.CloseParenthesis)
                {
                    count--;
                    if (count == 0) return i;
                }
            }
            return -1;
        }
        public List<(string token, Range position, TokenType type)> PerformContraction(
     List<(string token, Range position, TokenType type)> tokens)
        {
            var result = new List<(string token, Range position, TokenType type)>();
            result.AddRange(tokens);

            bool madeChanges;
            do
            {
                madeChanges = false;

                for (int i = 0; i < result.Count - 4; i++)
                {
                    // Перевіряємо вираз a * b + a * c або a * b - a * c
                    if ((result[i].type == TokenType.Number || result[i].type == TokenType.Variable) &&
                        result[i + 1].token == "*" &&
                        (result[i + 2].type == TokenType.Number || result[i + 2].type == TokenType.Variable) &&
                        (result[i + 3].token == "+" || result[i + 3].token == "-") &&
                        result[i + 4].token == result[i].token &&
                        result[i + 5].token == "*" &&
                        (result[i + 6].type == TokenType.Number || result[i + 6].type == TokenType.Variable))
                    {
                        // Знаходимо спільний множник
                        var commonFactor = result[i];

                        // Створюємо новий вираз a * (b + c)
                        var newExpression = new List<(string token, Range position, TokenType type)>
                {
                    commonFactor,
                    ("*", new Range(0, 1), TokenType.Operator),
                    ("(", new Range(0, 1), TokenType.OpenParenthesis),
                    result[i + 2], // b
                    (result[i + 3].token, new Range(0, 1), TokenType.Operator), // + або -
                    result[i + 6], // c
                    (")", new Range(0, 1), TokenType.CloseParenthesis)
                };

                        // Замінюємо старий вираз новим
                        result.RemoveRange(i, 7);
                        result.InsertRange(i, newExpression);
                        madeChanges = true;
                        break;
                    }
                }
            } while (madeChanges);

            Console.WriteLine(string.Join("", result.Select(x => x.token)));
            return result;
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
                    else if (tokens[i].token == "0" && i > 1 && tokens[i - 1].token == "*")
                    {
                        if (tokens[i - 2].type == TokenType.Variable || tokens[i - 2].type == TokenType.Number)
                        {
                            tokens.RemoveRange(i - 2, 2);
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
                    else if (i + 2 < tokens.Count
                        && tokens[i].type == TokenType.Number
                        && tokens[i + 1].type == TokenType.Operator
                        && tokens[i + 2].type == TokenType.Number)
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
                    else if (i + 2 < tokens.Count && tokens[i].token == "1" && tokens[i + 1].token == "*")
                    {
                        if (tokens[i + 2].type == TokenType.Variable || tokens[i + 2].type == TokenType.Number || tokens[i + 2].type == TokenType.OpenParenthesis)
                        {
                            tokens.RemoveRange(i, 2);
                            optimized = true;
                            optimizationsLog.Add("Removed unnecsessary multiplication by 1");
                        }
                    }
                    else if (tokens[i].token == "1" && i >= 3 && tokens[i - 1].token == "*")
                    {
                        if (i - 2 >= 0 && (tokens[i - 2].type == TokenType.Variable || tokens[i - 2].type == TokenType.Number || tokens[i - 2].type == TokenType.CloseParenthesis))
                        {
                            tokens.RemoveRange(i - 1, 2);
                            optimized = true;
                            optimizationsLog.Add("Removed unnecessary multiplication by 1");
                        }
                    }
                    #endregion
                    #region remove-1/x
                    else if (tokens[i].token == "1" && i >= 2 && tokens[i - 1].token == "/")
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
                    else if (i + 2 < tokens.Count && tokens[i].token == "0" && tokens[i + 1].token == "/")
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
                    else if (tokens[i].token == "/" && tokens[i + 1].token == "0") throw new DivideByZeroException();
                    #endregion
                    #region remove-(x)
                    else if (i > 0 && i < tokens.Count && (tokens[i].type == TokenType.Number || tokens[i].type == TokenType.Variable)
                        && tokens[i - 1].type == TokenType.OpenParenthesis && tokens[i + 1].type == TokenType.CloseParenthesis)
                    {
                        tokens.RemoveAt(i + 1);
                        tokens.RemoveAt(i - 1);
                        optimized = true;
                        optimizationsLog.Add("(x) simplified to x");
                    }
                    #endregion
                }
            } while (optimized);
            return GroupSequences(tokens);
        }

        private List<(string token, Range position, TokenType type)> GroupSequences(List<(string token, Range position, TokenType type)> tokens)
        {
            var expression = string.Join("", tokens.Select(x => x.token));
            // Замінюємо всі послідовності додавань на згруповані
            expression = Regex.Replace(expression, @"([a-zA-Z0-9.]+(\+[a-zA-Z0-9.]+){3,})", match =>
            {
                return DivideMultipleOperations(match.Value);
            });

            // Замінюємо всі послідовності множень на згруповані
            expression = Regex.Replace(expression, @"([a-zA-Z0-9.]+(\*[a-zA-Z0-9.]+){3,})", match =>
            {
                return DivideMultipleOperations(match.Value);
            });
            var lexer = new Lexer();
            var nool = new List<(string, Range)>();
            Console.WriteLine(expression);
            return lexer.Tokenize(expression, ref nool); ;
        }

        private string DivideMultipleOperations(string expression)
        {
            // Визначаємо оператор ("+" чи "*") у виразі
            char operation = expression.Contains('+') ? '+' : '*';

            // Розбиваємо вираз на операнди
            var operands = Regex.Split(expression, @"\+|\*").Where(x => !string.IsNullOrEmpty(x)).ToList();

            return GroupOperands(operands, operation);
        }

        private string GroupOperands(List<string> operands, char operation)
        {
            int count = operands.Count;

            if (count <= 2)
            {
                // Якщо операндів два чи менше, повертаємо вираз як (a operation b)
                return $"({string.Join(operation.ToString(), operands)})";
            }

            if (count == 3)
            {
                // Якщо три операнди, повертаємо (a operation b) operation c
                return $"({operands[0]}{operation}{operands[1]}){operation}{operands[2]}";
            }

            // Рекурсивно ділимо список на дві частини
            int half = count / 2;

            // Ліва частина
            var leftGroup = GroupOperands(operands.GetRange(0, half), operation);

            // Права частина
            var rightGroup = GroupOperands(operands.GetRange(half, count - half), operation);

            return $"({leftGroup}{operation}{rightGroup})";
        }
    }
}
