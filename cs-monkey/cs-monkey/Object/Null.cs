namespace CsMonkey.Object
{
  public class Null : IObject
  {
    public IObject.Type ObjectType => IObject.Type.NULL_OBJ;

    public string Inspect() => "null";

    public override bool Equals(object obj)
    {
      return obj is Null;
    }

    public override int GetHashCode() => base.GetHashCode();
  }
}
