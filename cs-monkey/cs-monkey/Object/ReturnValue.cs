namespace CsMonkey.Object
{
  public class ReturnValue : IObject
  {
    public IObject value;

    public IObject.Type ObjectType => IObject.Type.RETURN_VALUE_OBJ;

    public string Inspect() => value.Inspect();

    public override bool Equals(object obj)
    {
      if(!(obj is ReturnValue))
      {
        return false;
      }
      return value.Equals(obj);
    }

    public override int GetHashCode() => base.GetHashCode();
  }
}
