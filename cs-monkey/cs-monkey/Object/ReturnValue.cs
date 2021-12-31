namespace CsMonkey.Object
{
  public class ReturnValue : IObject
  {
    public IObject value;

    public IObject.Type ObjectType => IObject.Type.RETURN_VALUE_OBJ;

    public string Inspect() => value.Inspect();
  }
}
