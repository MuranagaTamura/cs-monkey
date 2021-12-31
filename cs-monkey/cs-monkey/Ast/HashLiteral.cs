using System.Linq;
using System.Collections.Generic;

namespace CsMonkey.Ast
{
  public class HashLiteral : IExpression
  {
    public Token token;
    public IDictionary<IExpression, IExpression> pairs
      = new Dictionary<IExpression, IExpression>();

    public string TokenLiteral => token.Literal;

    public override string ToString()
      => $"{{{string.Join(", ", pairs.Select(pair => $"{pair.Key}:{pair.Value}"))}}}";
  }
}
