namespace CsMonkey.Ast
{
  public class IndexExpression : IExpression
  {
    public Token token;
    public IExpression left;
    public IExpression index;

    public string TokenLiteral => token.Literal;

    public override string ToString() => $"({left}[{index}])";
  }
}
