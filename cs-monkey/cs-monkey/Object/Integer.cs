namespace CsMonkey.Object
{
  public class Integer : IObject
  {
    public long value;

    public IObject.Type ObjectType => IObject.Type.INTEGER_OBJ;

    public string Inspect() => $"{value}";
  }
}
