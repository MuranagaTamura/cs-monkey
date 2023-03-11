namespace CsMonkey.Ast
{
  public class ErrorNode : INode
  {
    public string message;

    public string TokenLiteral => message;

    public override string ToString() => $"ERROR({TokenLiteral})";
  }
}
