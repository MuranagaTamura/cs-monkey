namespace CsMonkey.Ast
{
  public class InfixExpression : IExpression
  {
    public Token token;
    public IExpression left;
    public string op;
    public IExpression right;

    public string TokenLiteral => token.Literal;

    public override string ToString() => $"({left} {op} {right})";
  }
}
