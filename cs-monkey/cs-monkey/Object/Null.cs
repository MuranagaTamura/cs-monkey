namespace CsMonkey.Object
{
  public class Null : IObject
  {
    public IObject.Type ObjectType => IObject.Type.NULL_OBJ;

    public string Inspect() => "null";
  }
}
