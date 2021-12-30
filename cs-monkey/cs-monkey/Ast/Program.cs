using System.Collections.Generic;
using System.Text;

namespace CsMonkey.Ast
{
  public class Program : INode
  {
    public IList<IStatement> Statements = new List<IStatement>();

    public string TokenLiteral
    {
      get => Statements.Count > 0 ? Statements[0].TokenLiteral : "";
    }

    public override string ToString()
    {
      StringBuilder builder = new StringBuilder();

      foreach (IStatement statement in Statements)
        builder.AppendLine(statement.ToString());

      return builder.ToString();
    }
  }
}
