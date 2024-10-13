namespace Compiler
{
    public class Parser
    {
        private enum State
        {
            Start,
            NumberOrVariable,
            Operator,
            OpenParenthesis,
            CloseParenthesis
        }

        private State currentState = State.Start;
        private List<int> openParenthesis = new List<int>();

        public List<(string message, Range position)> Parse(List<(string, Range, TokenType)> tokens, List<(string message, Range position)> errors)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                var (token, position, type) = tokens[i];

                switch (currentState)
                {
                    case State.Start:
                        if (token == "-")
                            currentState = State.Operator;
                        else if (type == TokenType.OpenParenthesis)
                        {
                            currentState = State.OpenParenthesis;
                            openParenthesis.Add(position.Start.Value);
                        }
                        else if (type == TokenType.Number || type == TokenType.Variable)
                        {
                            currentState = State.NumberOrVariable;
                        }
                        else
                            errors.Add(($"Error: Expression cannot start with '{token}'", position));
                        break;

                    case State.NumberOrVariable:
                        if (type == TokenType.Operator)
                        {
                            currentState = State.Operator;
                        }
                        else if (type == TokenType.CloseParenthesis)
                        {
                            if (openParenthesis.Count > 0)
                            {
                                openParenthesis.RemoveAt(openParenthesis.Count - 1);
                                currentState = State.CloseParenthesis;
                            }
                            else
                                errors.Add(($"Error: Unmatched closing parenthesis at position {position}", position));
                        }
                        else if (type == TokenType.OpenParenthesis)
                        {
                            openParenthesis.Add(position.Start.Value);
                            currentState = State.OpenParenthesis;
                        }
                        else
                        {
                            errors.Add(($"Error: Invalid token '{token}' at position {position} after a number", position));
                        }
                        break;
                    case State.Operator:
                        if (type == TokenType.Number || type == TokenType.Variable)
                        {
                            currentState = State.NumberOrVariable;
                        }
                        else if (type == TokenType.OpenParenthesis)
                        {
                            currentState = State.OpenParenthesis;
                            openParenthesis.Add(position.Start.Value);
                        }
                        else if (type == TokenType.CloseParenthesis)
                        {
                            errors.Add(($"Error: Missed number of variable before close parenthesis' at position {position}", position));
                            if (openParenthesis.Count > 0)
                            {
                                openParenthesis.RemoveAt(openParenthesis.Count - 1);
                                currentState = State.CloseParenthesis;
                            }
                            else
                                errors.Add(($"Error: Unmatched closing parenthesis at position {position.Start.Value}", position));
                            currentState = State.CloseParenthesis;
                        }
                        else
                            errors.Add(($"Error: Invalid token '{token}' at position {position} after an operator", position));
                        break;

                    case State.OpenParenthesis:
                        if (type == TokenType.Number || type == TokenType.Variable)
                            currentState = State.NumberOrVariable;
                        else if (type == TokenType.OpenParenthesis)
                        {
                            openParenthesis.Add(position.Start.Value);
                        }
                        else if (type == TokenType.CloseParenthesis) {
                            errors.Add(($"Error: Missed variable or number inside parentheses at position {position.Start.Value}", position));
                            if (openParenthesis.Count > 0)
                            {
                                openParenthesis.RemoveAt(openParenthesis.Count - 1);
                                currentState = State.CloseParenthesis;
                            }
                            else
                                errors.Add(($"Error: Unmatched closing parenthesis at position {position.Start.Value}", position));
                            currentState = State.CloseParenthesis;
                        }
                        else if (token == "-")
                        {
                            currentState = State.Operator;
                        }
                        else
                            errors.Add(($"Error: Invalid token '{token}' inside parentheses at position {position}", position));
                        break;

                    case State.CloseParenthesis:
                        if (type == TokenType.Operator)
                            currentState = State.Operator;
                        else if (type == TokenType.CloseParenthesis)
                        {
                            if (openParenthesis.Count > 0)
                            {
                                openParenthesis.RemoveAt(openParenthesis.Count - 1);
                                currentState = State.CloseParenthesis;
                            }
                            else
                                errors.Add(($"Error: Unmatched closing parenthesis at position {position.Start.Value}", position));
                        }
                        else if (type == TokenType.OpenParenthesis)
                        {
                            errors.Add(($"Error: Missed operator before an open parenthesis at position {position.Start.Value}", position));
                            openParenthesis.Add(position.Start.Value);
                            currentState = State.OpenParenthesis;
                        }
                        else
                            errors.Add(($"Error: Invalid token '{token}' at position {position} after a closing parenthesis", position));
                        break;
                }
            }

            if (currentState == State.Operator)
                errors.Add(("Error: Expression cannot end with an operator", new Range(tokens[tokens.Count - 1].Item2.Start.Value, tokens[tokens.Count - 1].Item2.Start.Value)));

            if (openParenthesis.Count > 0)
                errors.Add(($"Error: Unmatched opening parenthesis at {openParenthesis[openParenthesis.Count - 1]}", new Range(openParenthesis[openParenthesis.Count - 1], openParenthesis[openParenthesis.Count - 1])));

            var sortedErrors = errors.OrderBy(err => err.position.Start.Value);

            return sortedErrors.ToList();
        }
    }
}
