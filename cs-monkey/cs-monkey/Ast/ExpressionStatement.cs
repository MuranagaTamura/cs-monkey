namespace CsMonkey.Ast
{
  public class ExpressionStatement : IStatement
  {
    public Token token;
    public IExpression expression;

    public string TokenLiteral => token.Literal;

    public override string ToString() => expression?.ToString() ?? "";
  }
}
