namespace CsMonkey.Object
{
  public class Integer : IObject, Hashable
  {
    public long value;

    public IObject.Type ObjectType => IObject.Type.INTEGER_OBJ;

    public string Inspect() => $"{value}";

    public HashKey HashKey() => new HashKey() { type = ObjectType, value = value };
  }
}
