using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                var arg = args.First();
                if (!string.IsNullOrEmpty(arg))
                {
                    if (arg[0] < 'a')
                    {
                        A(arg.Substring(1));
                    }
                    else
                    {
                        B(arg.Substring(1));
                    }
                }
            }
        }

        private static void A(string arg)
        {
            if (!string.IsNullOrEmpty(arg))
            {
                if (arg[0] < 'a')
                {
                    C(arg.Substring(1));
                }
                else
                {
                    D(arg.Substring(1));
                }
            }
        }

        private static void B(string arg)
        {
            if (!string.IsNullOrEmpty(arg))
            {
                if (arg[0] < 'a')
                {
                    E(arg.Substring(1));
                }
                else
                {
                    F(arg.Substring(1));
                }
            }
        }

        private static void C(string arg)
        {
            Console.WriteLine("C");
            Environment.Exit(0);
        }

        private static void D(string arg)
        {
            Console.WriteLine("D");
            Console.WriteLine("Exiting!");
            Environment.Exit(0);
        }

        private static void E(string arg)
        {
            Console.WriteLine("E");
            Environment.Exit(1);
        }

        private static void F(string arg)
        {
            Console.WriteLine("F");
            Console.Error.WriteLine("Error!");
            Environment.Exit(1);
        }
    }
}
