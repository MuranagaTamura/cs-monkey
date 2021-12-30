using System.Text;
using CsMonkey.Ast;
using System.Collections.Generic;

namespace CsMonkey.Object
{
  public class Function : IObject
  {
    public IList<Identifier> parameters;
    public BlockStatement body;
    public Environment environment;

    public IObject.Type ObjectType => IObject.Type.FUNCTION_OBJ;

    public string Inspect()
    {
      StringBuilder builder = new StringBuilder();

      builder.AppendLine($"fn({string.Join(", ", parameters)}){{");
      builder.AppendLine(body.ToString());
      builder.Append("}");

      return builder.ToString();
    }
  }
}
