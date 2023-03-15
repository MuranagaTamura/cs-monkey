using System.Collections.Generic;
using System.Linq;

namespace CsMonkey.Object
{
  public class BuiltinHelper
  {
    public struct Defininition
    {
      public string name;
      public Builtin builtin;

      public Defininition(string name, Builtin builtin)
      {
        this.name = name;
        this.builtin = builtin;
      }
    }

    public static IList<Defininition> builtins = new List<Defininition>
    {
       new Defininition("len", new Builtin(){ fn = (IList<IObject> args) =>
          {
            if(args.Count != 1)
            {
              return new Error() { message = $"wrong number of arguments. got={args.Count}, want=1"};
            }

            switch(args[0])
            {
              case String @string:
                {
                  return new Integer(){ value = @string.value.Length };
                }
                case Array array:
                {
                  return new Integer(){ value = array.elements.Count };
                }
              default:
                {
                  return new Error() { message = $"argument to `len` not supported, got {args[0].ObjectType}" };
                }
            }
          }
       } ),
       new Defininition("puts", new Builtin(){ fn = (IList<IObject> args) =>
          {
            foreach(IObject arg in args)
            {
              System.Console.WriteLine(arg.Inspect());
            }
            return null;
          } } ),
       new Defininition("first", new Builtin() { fn = (IList<IObject> args) =>
          {
            if(args.Count != 1)
            {
              return new Error() { message = $"wrong number of arguments. got={args.Count}, want=1"};
            }
            if(args[0].ObjectType != IObject.Type.ARRAY_OBJ)
            {
              return new Error() { message = $"argument to `first` must be ARRAY, got {args[0].ObjectType}" };
            }

            if(args[0] is Array array && array.elements.Count > 0)
            {
              return array.elements[0];
            }

            return null;
          } } ),
        new Defininition( "last", new Builtin() { fn = (IList<IObject> args) =>
          {
            if(args.Count != 1)
            {
              return new Error() { message = $"wrong number of arguments. got={args.Count}, want=1"};
            }
            if(args[0].ObjectType != IObject.Type.ARRAY_OBJ)
            {
              return new Error() { message = $"argument to `last` must be ARRAY, got {args[0].ObjectType}" };
            }

            if(args[0] is Array array && array.elements.Count > 0)
            {
              return array.elements.Last();
            }

            return null;
          } } ),
        new Defininition("rest", new Builtin() { fn = (IList<IObject> args) =>
          {
            if(args.Count != 1)
            {
              return new Error() { message = $"wrong number of arguments. got={args.Count}, want=1"};
            }
            if(args[0].ObjectType != IObject.Type.ARRAY_OBJ)
            {
              return new Error() { message = $"argument to `rest` must be ARRAY, got {args[0].ObjectType}" };
            }

            if(args[0] is Array array && array.elements.Count > 0)
            {
              IList<IObject> newEelements = new List<IObject>(array.elements);
              newEelements.RemoveAt(0);
              return new Array(){ elements = newEelements };
            }

            return null;
          } } ),
        new Defininition("push", new Builtin(){ fn = (IList<IObject> args) =>
          {
            if(args.Count != 2)
              {
                return new Error() { message = $"wrong number of arguments. got={args.Count}, want=2"};
              }
              if(args[0].ObjectType != IObject.Type.ARRAY_OBJ)
              {
                return new Error() { message = $"argument to `push` must be ARRAY, got {args[0].ObjectType}" };
              }

              Array array = args[0] as Array;
              long length = array.elements.Count;

              IList<IObject> newElements = new List<IObject>(array.elements) { args[1] };
              return new Array(){ elements = newElements };
          } } ),
    };

    public static Builtin GetBuiltinByName(string name)
    {
      foreach (Defininition define in builtins)
      {
        if (define.name == name)
        {
          return define.builtin;
        }
      }
      return null;
    }
  }
}
