namespace CsMonkey.Object
{
  public class Integer : IObject, Hashable
  {
    public long value;

    public IObject.Type ObjectType => IObject.Type.INTEGER_OBJ;

    public string Inspect() => $"{value}";

    public HashKey HashKey() => new HashKey() { type = ObjectType, value = value };

    public override bool Equals(object obj)
    {
      if(!(obj is Integer integer))
      {
        return false;
      }
      return integer.value == value;
    }

    public override int GetHashCode() => base.GetHashCode();
  }
}
