using System.Collections.Generic;
using System.Text;

namespace CsMonkey.Ast
{
  public class Program : INode
  {
    public IList<IStatement> statements = new List<IStatement>();

    public string TokenLiteral
    {
      get => statements.Count > 0 ? statements[0].TokenLiteral : "";
    }

    public override string ToString()
    {
      StringBuilder builder = new StringBuilder();

      foreach (IStatement statement in statements)
        builder.AppendLine(statement.ToString());

      return builder.ToString();
    }
  }
}
