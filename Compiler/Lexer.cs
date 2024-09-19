using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Compiler
{
    public enum TokenType
    {
        Number,
        Varialble,
        Operator,
        OpenParenthesis,
        CloseParenthesis
    }
    public class Lexer
    {
        private static readonly string numberPattern = @"^\d+(\.\d+)?";
        private static readonly string incorrectNumberPattern = @"^(\.+\d+)+";
        private static readonly string variablePattern = @"^[a-zA-Z_][a-zA-Z0-9_]*";
        private static readonly string operatorPattern = @"^[\+\-\*\/]";
        private static readonly string openParenthesis = @"^[\(]";
        private static readonly string closeParenthesis = @"^[\)]";

        public List<(string token, Range position, TokenType type)> Tokenize(string expression, ref List<(string message, Range position)> errors)
        {
            List<(string, Range, TokenType)> tokens = new List<(string, Range, TokenType)>();
            int i = 0;

            while (i < expression.Length)
            {
                char current = expression[i];

                if (char.IsWhiteSpace(current))
                {
                    i++;
                    continue;
                }

                string remainingExpr = expression.Substring(i);

                // Match a number
                Match numberMatch = Regex.Match(remainingExpr, numberPattern);
                if (numberMatch.Success)
                {
                    tokens.Add((numberMatch.Value, new Range(i, i + numberMatch.Length - 1), TokenType.Number));
                    i += numberMatch.Length;
                    continue;
                }
                // Match an incorrect number
                Match incorrectNumberMatch = Regex.Match(remainingExpr, incorrectNumberPattern);
                if (incorrectNumberMatch.Success)
                {
                    errors.Add(($"Error: Incorrect number '{incorrectNumberMatch.Value}' at position {i}...{i + incorrectNumberMatch.Length - 1}", new Range(i, i + incorrectNumberMatch.Length - 1)));
                    i += incorrectNumberMatch.Length;
                    continue;
                }
                // Match a variable
                Match variableMatch = Regex.Match(remainingExpr, variablePattern);
                if (variableMatch.Success)
                {
                    tokens.Add((variableMatch.Value, new Range(i, i + variableMatch.Length - 1), TokenType.Varialble));
                    i += variableMatch.Length;
                    continue;
                }

                // Match an operator
                if (Regex.IsMatch(current.ToString(), operatorPattern))
                {
                    tokens.Add((current.ToString(), new Range(i, i), TokenType.Operator));
                    i++;
                    continue;
                }

                // Match an open parenthesis
                if (Regex.IsMatch(current.ToString(), openParenthesis))
                {
                    tokens.Add((current.ToString(), new Range(i, i), TokenType.OpenParenthesis));
                    i++;
                    continue;
                }

                // Match a close parenthesis
                if (Regex.IsMatch(current.ToString(), closeParenthesis))
                {
                    tokens.Add((current.ToString(), new Range(i, i), TokenType.CloseParenthesis));
                    i++;
                    continue;
                }

                errors.Add(($"Unexpected token '{current}' at position {i}", new Range(i, i)));
                i++;
            }

            return tokens;
        }
    }
}
