using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CsMonkey.Object
{
  public struct HashKey
  {
    public IObject.Type type;
    public long value;

    public override bool Equals([NotNullWhen(true)] object obj)
    {
      return obj is HashKey hashKey && hashKey.value == value;
    }

    public override int GetHashCode()
    {
      return (int)value;
    }
  }

  public struct HashPair
  {
    public IObject key;
    public IObject value;
  }

  public class Hash : IObject
  {
    public IDictionary<HashKey, HashPair> pairs; 

    public IObject.Type ObjectType => IObject.Type.HASH_OBJ;

    public string Inspect() => $"{{{string.Join(", ", pairs.Select((pair) => $"{pair.Value.key.Inspect()}: {pair.Value.value.Inspect()}"))}}}";
  }

  public interface Hashable
  {
    HashKey HashKey();
  }
}
