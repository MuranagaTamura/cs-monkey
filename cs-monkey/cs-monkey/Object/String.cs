namespace CsMonkey.Object
{
  public class String : IObject, Hashable
  {
    public string value;

    public IObject.Type ObjectType => IObject.Type.STRING_OBJ;

    public string Inspect() => $"`{value}`";

    public HashKey HashKey() => new HashKey() { type = ObjectType, value = value.GetHashCode() };

    public override bool Equals(object obj)
    {
      if(!(obj is String @string))
      {
        return false;
      }
      return @string.value == value;
    }

    public override int GetHashCode() => base.GetHashCode();
  }
}
