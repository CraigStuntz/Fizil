using System;
using System.Collections.Generic;
using System.Linq;
using Fizil.Instrumentation;

namespace TinyTest
{
    class Program
    {
        private static Instrument instrument; 

        static void Main(string[] args)
        {
            using (instrument = new Instrument())
            {
                instrument.Trace(50460);
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
        }

        private static void A(string arg)
        {
            instrument.Trace(7880);
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
            instrument.Trace(44666);
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
            instrument.Trace(61360);
            Console.WriteLine("C");
            Environment.Exit(0);
        }

        private static void D(string arg)
        {
            instrument.Trace(516);
            Console.WriteLine("D");
            Console.WriteLine("Exiting!");
            Environment.Exit(0);
        }

        private static void E(string arg)
        {
            instrument.Trace(37587);
            Console.WriteLine("E");
            throw new InvalidOperationException("E just failed!");
        }

        private static void F(string arg)
        {
            instrument.Trace(29875);
            Console.WriteLine("F");
            Console.Error.WriteLine("Error!");
            Environment.Exit(1);
        }
    }
}
