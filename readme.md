# Fizil
A fuzzer. 

This is an experiment and doesn't do much yet. Interesting things are coming!

In the meantime, use [AFL](http://lcamtuf.coredump.cx/afl/) if you want to do real fuzzing.

| Feature                                | Fizil        | AFL             |
|----------------------------------------|--------------|-----------------|
| Runs on Windows                        | Yep!         | Nope            |
| Runs on Unix                           | Probably not | Yep!            |
| Fast                                   | No           | Yep!            |
| Instrumentation guided                 | Soon?        | Yep!            |
| Rich suite of fuzzing strategies       | Not yet!     | Yes!            |
| Rock solid                             | Not yet!     | Yes             |
| Automatically disables crash reporting | Yep!         | Nope            |
| Property based testing                 | In progress  | Don't think so? |
| Rich tooling                           | No           | Yes             |
| Proven track record                    | No           | Yes             |
| Stable                                 | No way       | Yes             |
| License                                | Apache 2.0   | Apache 2.0      |

## Getting Started
1. Clone repo, `cd` into root (solution) directory
2. Restore packages (only needed first time)
  1. `./.paket/paket.bootstrapper.exe` 
  2. `./.paket/paket.exe install`
3. Build (in VS or from the command line just type `msbuild` if it's in your path)
4. Init demo project (only needed first time). Use `--init`. Two ways you can do this:
  1. In VS, right click Fizil project, Properties, Debug, add `--init` to Command line arguments
  2. From command line, `Fizil\bin\Debug\Fizil.exe --init`
5. Run from VS or command line.
  1. In VS, press F5
  2. From command line, `Fizil\bin\Debug\Fizil.exe`
Instrumentation is manual now (see `TinyTest\Program.cs`). This is terrible and will change soon!

## Gratitude

This project is heavily inspired by [AFL](http://lcamtuf.coredump.cx/afl/) and [QuickCheck](http://www.cse.chalmers.se/~rjmh/QuickCheck/manual.html).
It probably wouldn't have been possible for me to write at all without the [AFL technical whitepaper](http://lcamtuf.coredump.cx/afl/technical_details.txt) and source code comments.

### Open Source License Information
* [FSharp.Configuration](http://fsprojects.github.io/FSharp.Configuration/) under terms of the [Apache License](https://github.com/fsprojects/FSharp.Configuration/blob/master/LICENSE.txt)