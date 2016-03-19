using System;
using System.Collections.Generic;
using System.Linq;
using Fizil.Instrumentation;

namespace TinyTest
{
    class Program
    {
#if MANUAL_INSTRUMENTATION
        private static Instrument instrument;
#endif

        static void Main(string[] args)
        {
#if MANUAL_INSTRUMENTATION
            using (instrument = new Instrument())
            {
                instrument.Trace(50460);
#endif
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
#if MANUAL_INSTRUMENTATION
            }
#endif
        }

        private static void A(string arg)
        {
#if MANUAL_INSTRUMENTATION
            instrument.Trace(7880);
#endif
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
#if MANUAL_INSTRUMENTATION
            instrument.Trace(44666);
#endif
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
#if MANUAL_INSTRUMENTATION
            instrument.Trace(61360);
#endif
            Console.WriteLine("c");
            Environment.Exit(0);
        }

        private static void D(string arg)
        {
#if MANUAL_INSTRUMENTATION
            instrument.Trace(516);
#endif
            Console.WriteLine("d");
            Console.WriteLine("Exiting!");
            Environment.Exit(0);
        }

        private static void E(string arg)
        {
#if MANUAL_INSTRUMENTATION
            instrument.Trace(37587);
#endif
            Console.WriteLine("e");
            throw new InvalidOperationException("E just failed!");
        }

        private static void F(string arg)
        {
#if MANUAL_INSTRUMENTATION
            instrument.Trace(29875);
#endif
            Console.WriteLine("f");
            Console.Error.WriteLine("Error!");
            Environment.Exit(1);
        }
    }
}
