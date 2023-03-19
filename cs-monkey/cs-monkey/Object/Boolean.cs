namespace CsMonkey.Object
{
  public class Boolean : IObject, Hashable
  {
    public bool value;

    public IObject.Type ObjectType => IObject.Type.BOOLEAN_OBJ;

    public string Inspect() => $"{value}";

    public HashKey HashKey() => new HashKey() { type = ObjectType, value = value ? 1 : 0 };

    public override bool Equals(object obj)
    {
      if(!(obj is Boolean boolean))
      {
        return false;
      }
      return boolean.value == value;
    }

    public override int GetHashCode() => base.GetHashCode();
  }
}
