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

## Gratitude

This project is heavily inspired by [AFL](http://lcamtuf.coredump.cx/afl/) and [QuickCheck](http://www.cse.chalmers.se/~rjmh/QuickCheck/manual.html).
It probably wouldn't have been possible for me to write at all without the [AFL technical whitepaper](http://lcamtuf.coredump.cx/afl/technical_details.txt) and source code comments.

### Open Source License Information
* [FSharp.Configuration](http://fsprojects.github.io/FSharp.Configuration/) under terms of the [Apache License](https://github.com/fsprojects/FSharp.Configuration/blob/master/LICENSE.txt)