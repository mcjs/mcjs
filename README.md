MuscalietJS
===========

MuscalietJS: An Extensible Portable Layered JavaScript Engine

MuscalietJS is a layered architecture that splits responsibilities
across two levels: a JavaScript-specific engine and a language-agnostic
low-level VM. In principle, any managed language VM can serve as the lowlevel
engine. Our current implementation uses the Common Language Runtime
(CLR), as implemented by Mono. The low-level VM provides traditional
compiler optimizations: instruction scheduling, register allocation,
constant propagation, common subexpression elimination, etc., as well as
code generation and machine specific optimizations. In addition it provides
managed language services such as garbage collection, allowing us to focus
on the JavaScript-specifc aspects of the engine. 
Running the JavaScript engine inside another VM has performance implications.
Our split design relies on JavaScript specific optimizations at
the high-level to help mitigate the overhead of running on the CLR. The
JavaScript engine code generator exploits advanced high-level techniques
combined with type analysis and special hints to lead the low-level engine to
generate high-quality optimized code. There are performance advantages to
running on top of the CLR.

===========
System Requirement:

-- If using Mono, make sure to install Mono v3.0 or higher first and make sure mono executable is in the path

-- If using Visual Studio, make sure it supports .NET 4.5 (VS12 for instance).

===========
Building MCJS:

After cloning the git repo for MCJS, change directory to the root MCJavascript
folder. Then run the following:

git submodule update --init --recursive

For the rest of the commands here we assume you are in the root directory.

-- Linux Builds:

You must have mono version 3.0 or higher already installed and in your
PATH. You can use xbuild or make to build. The following command builds
./MCJavascript/bin/Release/MCJavascript.exe:

make

or

make JS BIILD TYPE=Release

The following command builds ./MCJavascript/bin/Debug/MCJavascript.exe:

make JS BIILD TYPE=Debug

-- Windows Builds:

You can use Visual Studio or Xamarin Studio to open and build MCJavascript.
In the root MCJavascript directory open MCJavascript-VS.sln solution and build.
NOTE: If building for the first time, please first build in RELASE mode once and 
      then you can build in DEBUG mode.

For more documentation, please refer to docs/MCJS.pdf in the repo
