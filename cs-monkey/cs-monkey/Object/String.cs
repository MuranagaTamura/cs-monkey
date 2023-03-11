namespace CsMonkey.Object
{
  public class String : IObject, Hashable
  {
    public string value;

    public IObject.Type ObjectType => IObject.Type.STRING_OBJ;

    public string Inspect() => $"`{value}`";

    public HashKey HashKey() => new HashKey() { type = ObjectType, value = value.GetHashCode() };
  }
}
