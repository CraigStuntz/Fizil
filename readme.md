# Fizil
A fuzzer. 

This is an experiment and doesn't do much yet. Interesting things are coming!

In the meantime, use [AFL](http://lcamtuf.coredump.cx/afl/) if you want to do real fuzzing.

| Feature                                | Fizil             | AFL                |
|----------------------------------------|-------------------|--------------------|
| Runs on Windows                        | Yep!              | No, but [there's a fork](https://github.com/ivanfratric/winafl) |
| Runs on Unix                           | Probably not      | Yep!               |
| Fast                                   | Not so much       | Yep!               |
| Process models                         | In/Out of process | Out of process, fork server |
| Instrumentation guided                 | Soon?             | Yep!               |
| Automatic instrumentation              | .NET assemblies   | Clang, GCC, Python |
| Rich suite of fuzzing strategies       | Getting there!    | Yes!               |
| Automatically disables crash reporting | Yep!              | Nope               |
| Rich tooling                           | No                | Yes                |
| Proven track record                    | No                | Yes                |
| Stable                                 | No way            | Yes                |
| License                                | Apache 2.0        | Apache 2.0         |

## Getting Started
1. Clone repo, `cd` into root (solution) directory
2. Restore packages (only needed first time)
  1. `./.paket/paket.bootstrapper.exe` 
  2. `./.paket/paket.exe install --redirects`
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
Fizil is released under the Apache license 2.0. See `license.txt`

Fizil depends on:

* [Cecil](https://github.com/jbevain/cecil) under the terms of the [MIT/X11 license](https://github.com/jbevain/cecil/blob/master/LICENSE.txt)
* [FSharp.Collections.ParallelSeq](http://fsprojects.github.io/FSharp.Collections.ParallelSeq/) under terms of the [Apache license 2.0](https://github.com/fsprojects/FSharp.Collections.ParallelSeq/blob/master/LICENSE.txt)
* [FSharp.Configuration](http://fsprojects.github.io/FSharp.Configuration/) under terms of the [Apache License](https://github.com/fsprojects/FSharp.Configuration/blob/master/LICENSE.txt)
* [FsUnit](http://fsprojects.github.io/FsUnit/) under terms of the [MIT license](https://github.com/fsprojects/FsUnit/blob/master/license.txt)
* Data from [FuzzDB](https://github.com/fuzzdb-project/fuzzdb) under terms of [CC-BY](https://github.com/fuzzdb-project/fuzzdb)
* Data from [JSONTestSute](https://github.com/nst/JSONTestSuite) under terms of the [MIT license](https://github.com/nst/JSONTestSuite/blob/master/LICENSE)
* [NUnit](http://www.nunit.org/) under terms of the [MIT license](http://www.nunit.org/nuget/nunit3-license.txt)
* [STJSON](https://github.com/nst/STJSON) under terms of the [MIT license](https://github.com/nst/STJSON/blob/master/LICENSE)
