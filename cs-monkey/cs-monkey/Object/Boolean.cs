namespace CsMonkey.Object
{
  public class Boolean : IObject, Hashable
  {
    public bool value;

    public IObject.Type ObjectType => IObject.Type.BOOLEAN_OBJ;

    public string Inspect() => $"{value}";

    public HashKey HashKey() => new HashKey() { type = ObjectType, value = value ? 1 : 0 };
  }
}
