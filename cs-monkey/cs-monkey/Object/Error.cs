namespace CsMonkey.Object
{
  public class Error : IObject
  {
    public string message;

    public IObject.Type ObjectType => IObject.Type.ERROR_OBJ;

    public string Inspect() => $"ERROR: {message}";
  }
}
