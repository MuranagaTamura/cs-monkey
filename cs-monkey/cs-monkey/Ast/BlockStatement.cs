using System.Collections.Generic;
using System.Text;

namespace CsMonkey.Ast
{
  public class BlockStatement : IStatement
  {
    public Token token;
    public IList<IStatement> statements = new List<IStatement>();

    public string TokenLiteral => token.Literal;
    public override string ToString()
    {
      StringBuilder builder = new StringBuilder();

      foreach (IStatement statement in statements)
        builder.Append(statement.ToString());

      return builder.ToString();
    }
  }
}
