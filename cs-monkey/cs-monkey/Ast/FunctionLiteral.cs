using System.Collections.Generic;

namespace CsMonkey.Ast
{
  public class FunctionLiteral : IExpression
  {
    public Token token;
    public IList<Identifier> parameters = new List<Identifier>();
    public BlockStatement body;
    public string name;

    public string TokenLiteral => token.Literal;

    public override string ToString() => $"{TokenLiteral}{(name != "" ? $"<{name}>" : "")}({string.Join(", ", parameters)}){body}";
  }
}
