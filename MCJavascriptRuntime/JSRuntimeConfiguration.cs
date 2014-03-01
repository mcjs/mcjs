// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using m.Util.Diagnose;

namespace mjr
{
    public class JSRuntimeConfiguration : mdr.RuntimeConfiguration
    {
        //Input & Output
        public readonly List<string> ScriptFileNames;
        public readonly List<string> ScriptStrings;
        public string InstJSPrefix { get; private set; }
        public bool DumpJSOnly { get; private set; }

        //JIT & Optimizations
        public bool EnableTypeInference { get; private set; }
        public bool EnableMethodCallResolution { get; private set; }
        public bool EnableFunctionInlining { get; private set; }
        public bool EnableDirectCalls { get; private set; }
        public bool EnableInlineCache { get; private set; }
        public bool EnableArgumentPool { get; private set; }
        public bool EnableParallelLex { get; private set; }
        public bool EnableParallelAnalyze { get; private set; }
        public bool EnableParallelJit { get; private set; }
        public bool EnableInterpreter { get; private set; }
        public bool EnableJIT { get; private set; }
        public bool EnableLightCompiler { get; private set; }
        public bool EnableMonoOptimizations { get; private set; }
        public bool MathOptimization { get; private set; }
        public bool EnableProfiling { get; private set; }
        public bool EnableSpeculativeJIT { get; private set; }
        public bool EnableRecursiveInterpreter { get; private set; }
        public bool EnableGuardElimination { get; private set; }
        public bool EnableDeoptimization { get; private set; }

        //Diagnose            
        public bool EnableCodeDump { get; private set; }
        public bool EnableDiagIL { get; set; }
        public bool EnableDiagLocation { get; set; }
        public bool EnableDiagOperation { get; set; }
        public int RandomSeed { get; private set; }

        //Profiling
        public bool MeasureTime { get; private set; }
        public bool ProfileInitTime { get; private set; }
        public bool ProfileReadFileTime { get; private set; }
        public bool ProfileLexTime { get; private set; }
        public bool ProfileParseTime { get; private set; }
        public bool ProfileAnalyzeTime { get; private set; }
        public bool ProfileJitTime { get; private set; }
        public bool ProfileExecuteTime { get; private set; }
        public bool ProfileOpTime { get; private set; }
        public bool ProfileOpFrequency { get; private set; }
        public bool ProfileFunctionTime { get; private set; }
        public bool ProfileFunctionFrequency { get; private set; }
        public bool ProfilePropertyCache { get; private set; }
        public bool OnlyLex { get; private set; }
        public bool OnlyParse { get; private set; }
        public bool OnlyAnalyze { get; private set; }
        public bool OnlyJit { get; private set; }

        public JSRuntimeConfiguration(params string[] args)
            : base(args)
        {
            //All boolean switches are falst by default, here we can set the default if they should be true
#pragma warning disable 414, 219
            var EnabledSwichesByDefault //Dont remove this unused variable indicate what we are doing here! :)
                = EnableTypeInference
                //= EnableMethodCallResolution
                //= EnableASTFunctionInlining
                //= EnableArgumentPool
                //= EnableCodeDump
                //= EnableDirectCalls
                = EnableMonoOptimizations
                = EnableInterpreter
                = EnableRecursiveInterpreter
                = EnableJIT
                = EnableGuardElimination
                = true;
#pragma warning restore

            RandomSeed = -1;

            ScriptFileNames = new List<string>();
            ScriptStrings = new List<string>();

            Options
                //Input & Output
                .Add("e=", @"runs the following ""script_string"" that is provided in the command line", v => ScriptStrings.Add(v))
                .Add("inst-js=", "only instrument the JS code from the AST without running the code", v => InstJSPrefix = v)
                .Add("dump-js", "only generate formatted JS code from the AST without running the code", v => DumpJSOnly = v != null)
                //JIT & Optimizations
                .Add("ti|type-inference", "enable/disable type inference (default is +)", v => EnableTypeInference = v != null)
                .Add("mr|method-resolution", "enable/disable target method resolution in MethodCalls (default is +)", v => EnableMethodCallResolution = v != null)
                .Add("fi|func-inlining", "enable/disable function inlining (default is +)", v => EnableFunctionInlining = v != null)
                .Add("dc|direct-call", "enable/disable generating direct calls (default is +)", v => EnableDirectCalls = v != null)
                .Add("ic|inline-cache", "enable/disable inline property cache (default is +)", v => EnableInlineCache = v != null)
                .Add("ap|argument-pool", "enable/disable argument pool for function arguments (default is +)", v => EnableArgumentPool = v != null)
                .Add("pl|parallel-lex", "enable/disable parallel lex (default is -)", v => EnableParallelLex = v != null)
                .Add("pa|parallel-analyze", "enable/disable parallel analyze (default is -)", v => EnableParallelAnalyze = v != null)
                .Add("pj|parallel-jit", "enable/disable parallel JIT (default is -)", v => EnableParallelJit = v != null)
                .Add("lc|light-compiler", "enable/disable light compiler (default is -)", v => EnableLightCompiler = v != null)
                .Add("i|interpreter", "enable/disable interpreter (default is +)", v => EnableInterpreter = v != null)
                .Add("ri|recursive-interpreter", "enable/disable recursive interpreter (default is +)", v => EnableRecursiveInterpreter = v != null)
                .Add("j|jit", "enable/disable JIT (default is +)", v => EnableJIT = v != null)
                .Add("pr|profiler", "enable/disable profiler (default is -)", v => EnableProfiling = v != null)
                .Add("ge|gurad-elimination", "enable/disable Guard Elimination (default is +)", v => EnableGuardElimination = v != null)
                .Add("dz|deoptimization", "enable/disable deoptimization (default is -)", v =>
                {
                    EnableDeoptimization = v != null;
                    if (EnableDeoptimization) { EnableSpeculativeJIT = EnableProfiling = true; }
                })
                .Add("sj|speculative-jit", "enable/disable speculative JIT (default is -)", v =>
                {
                    EnableSpeculativeJIT = v != null;
                    if (EnableSpeculativeJIT) EnableProfiling = true;
                })
                .Add("mono-opt", "enable/disable Mono JIT optimizations (default is +)", v => EnableMonoOptimizations = v != null)
                .Add("mt|math-opt", "enable/disable math array-based sin and cos functions (default is -)", v => MathOptimization = v != null)

                //Diagnose
                .Add("cd|code-dump", "enable/disable debug code dump (default is +)", v => EnableCodeDump = v != null)
                .Add("di|diag-il", "enable/disable IL diag dump (default is -)", v => EnableDiagIL = v != null)
                .Add("dl|diag-location", "enable/disable code diag dump (default is -)", v => EnableDiagLocation = v != null)
                .Add("do|diag-operations", "enable/disable code diag dump (default is -)", v => EnableDiagOperation = v != null)
                .Add("rs|random-seed=", "use random seed", (int v) =>
                {
                    RandomSeed = v;
                    Debug.WriteLine("Using random seed {0}", RandomSeed);
                }
                )
                //Profiling
                .Add("t", "runs several times and measure the timing", v => MeasureTime = v != null)
                .Add("profile-init-time", "enable/disable the profiling of the init time (default is -)", v => ProfileInitTime = v != null)
                .Add("profile-read-file-time", "enable/disable the profiling of the file read time (default is -)", v => ProfileReadFileTime = v != null)
                .Add("profile-lex-time", "enable/disable the profiling of the lex time (default is -)", v => ProfileLexTime = v != null)
                .Add("profile-parse-time", "enable/disable the profiling of the parse time (default is -)", v => ProfileParseTime = v != null)
                .Add("profile-analyze-time", "enable/disable the profiling of the analyze time (default is -)", v => ProfileAnalyzeTime = v != null)
                .Add("profile-jit-time", "enable/disable the profiling of the jit time (default is -)", v => ProfileJitTime = v != null)
                .Add("profile-execute-time", "enable/disable the profiling of the js execution time (default is -)", v => ProfileExecuteTime = v != null)
                .Add("profile-func-time", "enable/disable the profiling of the each js function execution time (default is -)", v => ProfileFunctionTime = v != null)
                .Add("profile-func-freq", "enable/disable the profiling of the each js function execution frequency (default is -)", v => ProfileFunctionFrequency = v != null)
                .Add("profile-op-time", "enable/disable the profiling of the each operation execution time (default is -)", v => ProfileOpTime = v != null)
                .Add("profile-op-freq", "enable/disable the profiling of the each operation execution frequency (default is -)", v => ProfileOpFrequency = v != null)
                .Add("profile-phases", "enable/disable the profiling of all compilation pahses' time (default is -)", v =>
                  ProfileInitTime
                  = ProfileReadFileTime
                  = ProfileLexTime
                  = ProfileParseTime
                  = ProfileAnalyzeTime
                  = ProfileJitTime
                  = ProfileExecuteTime
                  = v != null
                )
                .Add("profile-all", "enable/disable all profiling (default is -)", v =>
                  ProfileInitTime
                  = ProfileReadFileTime
                  = ProfileLexTime
                  = ProfileParseTime
                  = ProfileAnalyzeTime
                  = ProfileJitTime
                  = ProfileExecuteTime
                  = ProfileOpFrequency
                  = ProfileFunctionTime
                  = ProfileFunctionFrequency
                  = ProfileOpTime
                  = ProfileOpFrequency
                  = ProfilePropertyCache
                  = v != null
                )
                //The following is deprecated, don't use it anymore
                //.Add("profile=", "specifies list of things to be profiled (InitTime,ReadFileTime,LexTime,ParseTime,AnalyzeTime,JitTime,ExecuteTime,FuncTime,FuncFreq,OpTime,OpFreq,PropCache,Phases,All,None)", v =>
                //    {
                //        var items = v.Split(',');
                //        foreach (var item in items)
                //            switch (item)
                //            {
                //                case "InitTime":
                //                    ProfileInitTime = true;
                //                    MeasureTime = true;
                //                    break;
                //                case "ReadFileTime":
                //                    ProfileReadFileTime = true;
                //                    MeasureTime = true;
                //                    break;
                //                case "LexTime":
                //                    ProfileLexTime = true;
                //                    MeasureTime = true;
                //                    break;
                //                case "ParseTime":
                //                    ProfileParseTime = true;
                //                    MeasureTime = true;
                //                    break;
                //                case "AnalyzeTime":
                //                    ProfileAnalyzeTime = true;
                //                    MeasureTime = true;
                //                    break;
                //                case "JitTime":
                //                    ProfileJitTime = true;
                //                    MeasureTime = true;
                //                    break;
                //                case "ExecuteTime":
                //                    ProfileExecuteTime = true;
                //                    MeasureTime = true;
                //                    break;
                //                case "FuncTime":
                //                    ProfileFunctionTime = true;
                //                    break;
                //                case "FunctFreq":
                //                    ProfileFunctionFrequency = true;
                //                    break;
                //                case "OpTime":
                //                    ProfileOpTime = true;
                //                    MeasureTime = true;
                //                    break;
                //                case "OpFreq":
                //                    ProfileOpFrequency = true;
                //                    MeasureTime = true;
                //                    break;
                //                case "PropCache":
                //                    ProfilePropertyCache = true;
                //                    break;
                //                case "Phases":
                //                    ProfileInitTime = true;
                //                    ProfileReadFileTime = true;
                //                    ProfileLexTime = true;
                //                    ProfileParseTime = true;
                //                    ProfileAnalyzeTime = true;
                //                    ProfileJitTime = true;
                //                    ProfileExecuteTime = true;
                //                    MeasureTime = true;
                //                    break;
                //                case "All":
                //                    ProfileFunctionTime = true;
                //                    ProfileFunctionFrequency = true;
                //                    ProfileOpTime = true;
                //                    ProfileOpFrequency = true;
                //                    ProfilePropertyCache = true;
                //                    goto case "Phases";
                //                case "None":
                //                    ProfileOpFrequency = false;
                //                    ProfileOpFrequency = false;
                //                    ProfileParseTime = false;
                //                    ProfileAnalyzeTime = false;
                //                    ProfileJitTime = false;
                //                    ProfileExecuteTime = false;
                //                    ProfilePropertyCache = false;
                //                    MeasureTime = false;
                //                    break;

                //            }
                //    }
                //)
                .Add("only-lex", "only lexe the input", v => OnlyLex = v != null)
                .Add("only-parse", "only lex and parse the input", v => OnlyParse = v != null)
                .Add("only-analyze", "only lex, parse, analyze, and jit the input", v => OnlyAnalyze = v != null)
                .Add("only-jit", "only lex, parse, analyze, and jit the input", v => OnlyJit = v != null)
                ;
        }
        protected override void Parse(params string[] args)
        {
            base.Parse(args);
            ScriptFileNames.AddRange(UnprocessedArguments);
            UnprocessedArguments = null;

            Debug.WriteLine("Mono optimizations enabled: {0}", EnableMonoOptimizations);
            Trace.Assert(EnableInterpreter || EnableJIT, "At least interpreter or jit must be enabled");

            const string AndroidLogPath = "/data/data/manticore.zoomm/cache/logs";
            if (System.IO.Directory.Exists(AndroidLogPath))
            {
                OutputDir = AndroidLogPath; //In this case, Android logs path is our only possible choice!
                Trace.WriteLine("Forced the OutputDir to '{0}'", OutputDir);
            }

            //TODO: fix these later!
            if (EnableParallelJit)
                EnableDirectCalls = false;

            EnableCounters = false
              || ProfileStats
              || ProfileOpTime
              || ProfileOpFrequency
              || ProfilePropertyCache
              || ProfileFunctionFrequency
              || ProfileOpFrequency
              ;

            EnableTimers = false
              || MeasureTime
              || ProfileInitTime
              || ProfileReadFileTime
              || ProfileLexTime
              || ProfileParseTime
              || ProfileAnalyzeTime
              || ProfileJitTime
              || ProfileExecuteTime
              || ProfileFunctionTime
              || ProfileOpTime
              ;

            if ((EnableTimers || EnableCounters) && ProfilerOutput != null)
                ProfilerOutput = System.IO.Path.Combine(OutputDir, ProfilerOutput);
        }
    }
}
