# Fizil
A fuzzer. 

This is an experiment and doesn't do much yet. Interesting things are coming!

In the meantime, use [AFL](http://lcamtuf.coredump.cx/afl/) if you want to do real fuzzing.

| Feature                                | Fizil           | AFL                |
|----------------------------------------|-----------------|--------------------|
| Runs on Windows                        | Yep!            | Nope               |
| Runs on Unix                           | Probably not    | Yep!               |
| Fast                                   | No              | Yep!               |
| Instrumentation guided                 | Soon?           | Yep!               |
| Automatic instrumentation              | .NET assemblies | Clang, GCC, Python |
| Rich suite of fuzzing strategies       | Not yet!        | Yes!               |
| Automatically disables crash reporting | Yep!            | Nope               |
| Property based testing                 | In progress     | Don't think so?    |
| Rich tooling                           | No              | Yes                |
| Proven track record                    | No              | Yes                |
| Stable                                 | No way          | Yes                |
| License                                | Apache 2.0      | Apache 2.0         |

## Getting Started
1. Clone repo, `cd` into root (solution) directory
2. Restore packages (only needed first time)
  1. `./.paket/paket.bootstrapper.exe` 
  2. `./.paket/paket.exe install`
3. Build (in VS or from the command line just type `msbuild` if it's in your path)
4. Init demo project (only needed first time). Use `--init`. Two ways you can do this:
  1. In VS, right click Fizil project, Properties, Debug, add `--init` to Command line arguments
  2. From command line, change to project folder and then `Fizil\bin\Debug\Fizil.exe --init`. Copy appropriate files into `system-under-test` and `examples` folders
5. Instrument: 
  1. In VS, right click Fizil project, Properties, Debug, add `--instrument` to Command line arguments
  2. From command line, change to project folder and then `Fizil\bin\Debug\Fizil.exe --instrument`
6. Run from VS or command line.
  1. In VS, press F5
  2. From command line, change to project folder and then`Fizil\bin\Debug\Fizil.exe`

## Gratitude

This project is heavily inspired by [AFL](http://lcamtuf.coredump.cx/afl/) and [QuickCheck](http://www.cse.chalmers.se/~rjmh/QuickCheck/manual.html).
It probably wouldn't have been possible for me to write at all without the [AFL technical whitepaper](http://lcamtuf.coredump.cx/afl/technical_details.txt) and source code comments.

Shout-out to the folks at Microsoft who wrote and maintain [peverify](https://msdn.microsoft.com/en-us/library/62bwd2yd.aspx) and [ildasm](https://msdn.microsoft.com/en-us/library/f7dy01k1.aspx).

Thank you the authors of and contributors to the fine open source libraries listed below.

### Open Source License Information
* [FSharp.Configuration](http://fsprojects.github.io/FSharp.Configuration/) under terms of the [Apache License](https://github.com/fsprojects/FSharp.Configuration/blob/master/LICENSE.txt)
* [Cecil](https://github.com/jbevain/cecil) under the terms of the [MIT/X11 license](https://github.com/jbevain/cecil/blob/master/LICENSE.txt)