namespace CsMonkey.Object
{
  public interface IObject
  {
    public enum Type
    {
      BOOLEAN_OBJ,
      ERROR_OBJ,
      FUNCTION_OBJ,
      INTEGER_OBJ,
      NULL_OBJ,
      RETURN_VALUE_OBJ,
      STRING_OBJ,
      BULTIN_OBJ,
      ARRAY_OBJ,
      HASH_OBJ,
    }

    Type ObjectType { get; }
    string Inspect();
  }
}
