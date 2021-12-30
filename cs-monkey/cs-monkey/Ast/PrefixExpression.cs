namespace CsMonkey.Ast
{
  public class PrefixExpression : IExpression
  {
    public Token token;
    public string op;
    public IExpression right;

    public string TokenLiteral => token.Literal;

    public override string ToString() => $"({op}{right})";
  }
}
