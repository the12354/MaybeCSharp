using System;
using System.Diagnostics;
using MaybeCSharp;
namespace MaybeDemo
{
    class Program
    {
        class B
        {
            public int Value;
            public B(int value)
            {
                Value = value;
            }
        }
        class A
        {
            public B MaybeNull;
            public A(B obj)
            {
                MaybeNull = obj;
            }
        }
        static void Main()
        {
            var notNull = new A(new B(42));
            var Null = new A(null);

            var resultA = notNull.Maybe(n => n.MaybeNull.Value);
            var resultB = Null.Maybe(n => n.MaybeNull.Value);
            
            Console.WriteLine(resultA);
            Console.WriteLine(resultB);

            Debug.Assert(resultA.HasValue);
            Debug.Assert(!resultB.HasValue);

            Console.ReadLine();
        }
    }
}
