namespace CsMonkey.Object
{
  public class Boolean : IObject
  {
    public bool value;

    public IObject.Type ObjectType => IObject.Type.BOOLEAN_OBJ;

    public string Inspect() => $"{value}";
  }
}
