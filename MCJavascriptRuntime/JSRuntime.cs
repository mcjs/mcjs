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
using System.Text;
using System.Collections.Generic;
using System.IO;

using m.Util.Diagnose;

namespace mjr
{
    public class JSRuntime : mdr.Runtime
    {
        public static new JSRuntime Instance { get { return (JSRuntime)mdr.Runtime.Instance; } set { mdr.Runtime.Instance = value; } }
        public new JSRuntimeConfiguration Configuration { get { return (JSRuntimeConfiguration)base.Configuration; } }

        internal ILGen.BaseAsmGenerator AsmGenerator;

        public class ScriptCollection
        {
            class ScriptInfo
            {
                public readonly string Key;
                public readonly string Text;
                public JSFunctionMetadata Metadata;
                public System.Threading.Tasks.Task LexTask;

                public ScriptInfo(string key, string text)
                {
                    Key = key;
                    Text = text;
                }
            }
            readonly System.Collections.Concurrent.ConcurrentDictionary<string, ScriptInfo> Scripts = new System.Collections.Concurrent.ConcurrentDictionary<string, ScriptInfo>();

            /// <summary>
            /// Use this method for asynchronous preparation of the script
            /// </summary>
            public void Add(string scriptString, string scriptKey, JSRuntime runtime)
            {
                if (!runtime.Configuration.EnableParallelLex)
                  return;
                JSRuntime.Instance = runtime;
                var addTimer = StartTimer(true, "JS/Script/Add/" + scriptKey);
                InternalAdd(scriptString, scriptKey, runtime);
                StopTimer(addTimer);
            }
            private ScriptInfo InternalAdd(string scriptString, string scriptKey, JSRuntime runtime)
            {
                Debug.Assert(scriptKey != null, "Invalid scriptKey, should be string");
                ScriptInfo script = null;
                if (!Scripts.TryGetValue(scriptKey, out script))
                {
                    script = Scripts.GetOrAdd(scriptKey, key =>
                    {
                        Debug.WriteLine("Script precompilation cache {0} [miss]", key);
                        JSRuntime.Instance = runtime;
                        var scriptInfo = new ScriptInfo(key, scriptString);
                        scriptInfo.LexTask = System.Threading.Tasks.Task.Factory.StartNew(state =>
                        {
                            var taskState = state as JSTaskState<ScriptInfo>;
                            var info = taskState.GetState();
                            var timer = JSRuntime.StartTimer(true, "JS/PreLoad/" + key);
                            info.Metadata = taskState.RuntimeInstance.LoadScriptStringMetadata(info.Text);
                            if (Instance.Configuration.EnableParallelJit)
                            {
                                info.Metadata.JitSpeculatively(ref mdr.DFunctionSignature.EmptySignature);
                            }
                            else if (Instance.Configuration.EnableParallelAnalyze)
                                info.Metadata.Analyze(); //Will launch the rest itself.
                            JSRuntime.StopTimer(timer);

                        }, new JSTaskState<ScriptInfo>(scriptInfo));
                        return scriptInfo;
                    });
                }
                else
                {
                    Debug.WriteLine("Script precompilation cache {0} [hit]", scriptKey);
                }
                return script;
            }

            public JSFunctionMetadata GetOrAdd(string scriptString, string scriptKey, JSRuntime runtime)
            {
              ScriptInfo script;
              if (runtime.Configuration.EnableParallelLex)
              {
                script = InternalAdd(scriptString, scriptKey, runtime);
                var timer = StartTimer(Instance.Configuration.MeasureTime, "WaitForLex");
                script.LexTask.Wait();
                StopTimer(timer);
              }
              else
              {
                if (!Scripts.TryGetValue(scriptKey, out script))
                {
                  script = new ScriptInfo(scriptKey, scriptString);
                  script.Metadata = runtime.LoadScriptStringMetadata(scriptString);
                  Scripts.TryAdd(scriptKey, script);
                }
              }
              return script.Metadata;
            }

            private ScriptInfo GetScriptInfo(JSFunctionMetadata funcMetadata)
            {
              var f = funcMetadata;
              while (f.ParentFunction != null)
                f = f.ParentFunction;
              foreach (var v in Scripts.Values)
                if (v.Metadata == f)
                  return v;

              return null;
            }
            public string GetScriptString(JSFunctionMetadata funcMetadata)
            {
              var info = GetScriptInfo(funcMetadata);
              return (info != null) ? info.Text : null;
            }
            public string GetScriptKey(JSFunctionMetadata funcMetadata)
            {
              var info = GetScriptInfo(funcMetadata);
              return (info != null) ? info.Key : null;
            }
            public JSFunctionMetadata GetMetadata(string scriptKey)
            {
              ScriptInfo info;
              if (Scripts.TryGetValue(scriptKey, out info))
                return info.Metadata;
              else
                return null;
            }
            public List<JSFunctionMetadata> GetAllMetaData()
            {
              List<JSFunctionMetadata> retList = new List<JSFunctionMetadata>();
              foreach (var key in Scripts.Keys)
                retList.Add(Scripts[key].Metadata);
              return retList;
            }
        }

        //TODO: we can make the followings static and avoid re-compiling a script if it is already there!
        // Note that scripts are currently cleared in ShutDown() due to a GC issue
        public readonly ScriptCollection Scripts = new ScriptCollection();

        public class SpeculationMap
        {
            System.Collections.Concurrent.ConcurrentDictionary<string, LinkedList<JSFunctionMetadata>> _map = new System.Collections.Concurrent.ConcurrentDictionary<string, LinkedList<JSFunctionMetadata>>();
            public void Add(string key, JSFunctionMetadata func)
            {
                //TODO: can we make this call async?
                Debug.Assert(func != null, "Cannot add null function for {0}", key);
                LinkedList<JSFunctionMetadata> funcs = null;
                if (!_map.TryGetValue(key, out funcs))
                {
                    funcs = _map.GetOrAdd(key, k => new LinkedList<JSFunctionMetadata>());
                }
                funcs.AddLast(func);
            }
            public LinkedList<JSFunctionMetadata> Get(string key)
            {
                LinkedList<JSFunctionMetadata> funcs = null;
                _map.TryGetValue(key, out funcs);
                return funcs;
            }
        }
        public readonly SpeculationMap Speculator = new SpeculationMap();

#if DIAGNOSE || DEBUG
        /// <summary>
        /// The code generator can emit code to update these variables as each statement is executed, making it possible
        /// to produce a JSSourceLocation at runtime, which can be used with the Diagnose module for more precise error messages.
        /// </summary>
        LinkedList<JSSourceLocation> _locations = new LinkedList<JSSourceLocation>();

        public override m.Util.ISourceLocation Location
        {
            get 
            {
              lock (_locations)
                return (_locations.Count > 0) ? _locations.Last.Value : null; 
            }
        }
        public static void PushLocation(JSFunctionMetadata funcMetadata, int sourceOffset)
        {
          lock (Instance._locations)
            Instance._locations.AddLast(new JSSourceLocation(funcMetadata, sourceOffset));
        }
        public static void PopLocation()
        {
          lock (Instance._locations)
            if (Instance._locations.Count > 0)
                Instance._locations.RemoveLast();
        }
#else
        public string GetScriptString(JSFunctionMetadata funcMetadata)
        {
            return null;
        }
        public override m.Util.ISourceLocation Location
        {
            get { return null; }
        }
        public static void PushLocation(JSFunctionMetadata funcMetadata, int sourceOffset)
        {
        }
        public static void PopLocation()
        {
        }
#endif

        #region Timers & Counters
        //long AccumulateTimers(string timerName, Util.Timers source, Util.Counters destination)
        //{
        //    var timers = source.Find(timerName);
        //    long totalTime = 0;
        //    foreach (var timer in timers)
        //        totalTime += timer.ElapsedTicks;
        //    destination.Add(timerName, "ns").Count = totalTime * Util.Timers.NanoSecondsPerTick;
        //    return totalTime;
        //}
        //long AccumulateTimers(bool isEnabled, string threadName, string timerName, long totalTime, Util.Timers source, Util.Counters destination)
        //{
        //    if (isEnabled)
        //    {
        //        var fullTimerName = string.Format("{0}{1}{2}", threadName, Util.Timers.Timer.NameSeparator, timerName);
        //        var timerTotal = AccumulateTimers(fullTimerName, Timers, destination);
        //        destination.Add(fullTimerName, "%").Count = 100 * timerTotal / totalTime;
        //        return timerTotal;
        //    }
        //    return 0;
        //}
        //long AccumulateCounters(string counterName, Util.Counters source, Util.Counters destination)
        //{
        //    var counters = source.Find(counterName);
        //    long totalCount = 0;
        //    foreach (var counter in counters)
        //        totalCount += counter.Count;
        //    // Debug.WriteLine("{0}: {1}", counterName, string.Join(", ", counters.Select(s => s.Count.ToString()).ToArray()));
        //    destination.Add(counterName, "ns").Count = totalCount * Util.Timers.NanoSecondsPerTick;
        //    return totalCount;
        //}
        void GenerateSummary(TextWriter output)
        {
            //var totalTime = _timer.ElapsedTicks;
            //var summary = new Util.Counters("Summary");
            //foreach (var threadName in Timers.ThreadNames)
            //{
            //    long sum = 0;
            //    sum += AccumulateTimers(Config.ProfileParseTime, threadName, "GlobalInit", totalTime, Timers, summary);
            //    sum += AccumulateTimers(Config.ProfileParseTime, threadName, "ReadFile", totalTime, Timers, summary);
            //    sum += AccumulateTimers(Config.ProfileParseTime, threadName, "LexScript", totalTime, Timers, summary);
            //    sum += AccumulateTimers(Config.ProfileParseTime, threadName, "Parse", totalTime, Timers, summary);
            //    sum += AccumulateTimers(Config.ProfileAnalyzeTime, threadName, "Analyze", totalTime, Timers, summary);
            //    sum += AccumulateTimers(Config.ProfileJitTime, threadName, "TI", totalTime, Timers, summary);
            //    sum += AccumulateTimers(Config.ProfileJitTime, threadName, "Jit", totalTime, Timers, summary);
            //    AccumulateTimers(Config.ProfileJitTime, threadName, "Jit/Full", totalTime, Timers, summary);
            //    AccumulateTimers(Config.ProfileJitTime, threadName, "Jit/Clr", totalTime, Timers, summary);
            //    sum += AccumulateTimers(Config.ProfileJitTime, threadName, "Execute", totalTime, Timers, summary);
            //    summary.Add(Util.Timers.TimersOverhead.Name, "ns").Count = Util.Timers.TimersOverhead.ElapsedNanoseconds;
            //    summary.Add(Util.Timers.TimersOverhead.Name, "%").Count = 100 * Util.Timers.TimersOverhead.ElapsedTicks / totalTime;

            //    if (sum > 0)
            //    {
            //        summary.Add(threadName + "-util", "ns").Count = sum * Util.Timers.NanoSecondsPerTick;
            //        summary.Add(threadName + "-util", "%").Count = 100 * sum / totalTime;
            //    }
            //}
            //summary.Add("Total", "ns").Count = totalTime * Util.Timers.NanoSecondsPerTick;
            //summary.PrintCounters(Console.Out);
        }

        #endregion

        public JSRuntime() :
          this(JSEngine.Configuration)
        {}

        public JSRuntime(JSRuntimeConfiguration configuration)
            : base(configuration)
        {
            //We need to first set the Instance since the rest may use it!
            JSRuntime.Instance = this;

#if DEBUG
            if (configuration.EnableCodeDump)
            {
                var scriptFileName = "__Debug.js";
                if (configuration.ScriptFileNames.Count > 0)
                    scriptFileName = configuration.ScriptFileNames[0];

                AsmGenerator = new ILGen.CppAsmGenerator(scriptFileName, configuration.OutputDir);
            }
            else if (configuration.EnableDiagIL)
                AsmGenerator = new ILGen.CppAsmGenerator();// CodeAsmGenerator();
            else
#endif
                AsmGenerator = new ILGen.DynamicAsmGenerator();

            AsmGenerator.BeginAssembly();
        }


        private void PrintProfileInfo(JSFunctionMetadata metaData)
        {
          Console.WriteLine("###########################################");
          Console.WriteLine("Profile Info of function: {0}", metaData.Declaration);
          var cachedCodes = metaData.Cache.Items;
          foreach (var code in cachedCodes)
          {
            Console.WriteLine("Signature {0}", code.Signature);
            Console.Write("{0}", code.Profiler);
          }
          Console.WriteLine("###########################################\n");
          foreach (var subFunc in metaData.SubFunctions)
            if (subFunc != null)
              PrintProfileInfo(subFunc);
        }
        public override void ShutDown()
        {

            AsmGenerator.EndAssembly();
            GlobalContext = null;

            base.ShutDown();
            Instance = null;
        }



        public void InitGlobalContext(mdr.DObject globalContext)
        {
            Builtins.JSGlobalObject.Init(globalContext);
        }
        public override void SetGlobalContext(mdr.DObject globalContext)
        {
            var timer = StartTimer(Configuration.ProfileInitTime, "JS/GlobalInit");
            try
            {
                base.SetGlobalContext(globalContext); //This will set the GlobalContext
                if (JSRuntime.Instance.Configuration.EnableRecursiveInterpreter && false)
                {
                  var result = new mdr.DValue();
                  Builtins.JSGlobalObject.EvalString(@"
Object.defineProperty(Array.prototype, 'pop', 
{
  enumerable: false,
  value: function() 
  {
    var n = (this.length);
    if (n == 0) {
      this.length = n;
      return;
    }
    n--;
    var value = this[n];
    delete this[n];
    this.length = n;
    return value;
  }
});

Object.defineProperty(Array.prototype, 'push', 
{
  enumerable: false,
  value: function() 
  {
    var n = this.length;
    var m = arguments.length;
    for (var i = 0; i < m; i++) {
      this[i+n] = arguments[i];
    }
    this.length = n + m;
    return this.length;
  }
});
", ref result);
                }
            }
            finally
            {
                StopTimer(timer);
            }
        }

        public int RunScriptProgram(JSFunctionMetadata prgMetadata)
        {
            try
            {
            //TODO: It should be checked later. This situation should not happen

            if (prgMetadata == null)
                return 0;
            var prgFunc = new mdr.DFunction(prgMetadata, null);

            mdr.CallFrame callFrame = new mdr.CallFrame();
            callFrame.Function = prgFunc;
            callFrame.Signature = mdr.DFunctionSignature.EmptySignature;
            callFrame.This = (GlobalContext);
            prgFunc.Call(ref callFrame);
            return 0;
            }
            catch (mjr.JSException e)
            {
              WriteJSException(e);
            }
            catch (Exception e)
            {
              Diagnostics.WriteException(e);
            }
            return 1;
        }

        protected void WriteJSException(JSException e)
        {
          if (Configuration.EnableExceptionDump)
          {
            Trace.WriteLine("\n\n");
            Trace.WriteLine("==========================================================");
            Trace.WriteLine("=============== script threw exception ===================");
            Trace.WriteLine("==========================================================");
            Trace.WriteLine(Builtins.JSGlobalObject.ToString(ref e.Value));

            Debug.WriteLine("\n" + e.StackTrace);
            Debug.WriteLine("");
          }
        }

        private JSFunctionMetadata LoadScriptStringMetadata(string scriptString)
        {
            JSFunctionMetadata program = null;
            var timer = StartTimer(Configuration.ProfileLexTime, "JS/Parse");
            try
            {
                program = JSParser.ParseScript(scriptString).Expression.Metadata;
            }
            catch (Exception e)
            {
                Diagnostics.WriteException(e);
            }
            finally
            {
              StopTimer(timer);
            }
            return program;
        }

        public int RunScriptString(string scriptString, string scriptKey)
        {
            Debug.WriteLine("calling runScriptString on {0} with key {1}", scriptString, scriptKey);
            var program = Scripts.GetOrAdd(scriptString, scriptKey, this);
            return RunScriptProgram(program);
        }

        public string ReadAllScript(string scriptFileName)
        {
            var timer = StartTimer(Configuration.ProfileReadFileTime, "JS/ReadFile/" + System.IO.Path.GetFileName(scriptFileName));
            string scriptString;
            try
            {
                using (var sr = new System.IO.StreamReader(scriptFileName, true))
                {
                    scriptString = sr.ReadToEnd();
                }
            }
            finally
            {
                StopTimer(timer);
            }
            return scriptString;
        }

        internal int RunScriptFile(string scriptFileName)
        {
            return RunScriptString(ReadAllScript(scriptFileName), scriptFileName);
        }

        public mdr.DFunction PrepareScript(string script, mdr.DObject ctx = null)
        {
            var scriptMetadata = LoadScriptStringMetadata(script);
            var prgFunc = new mdr.DFunction(scriptMetadata, null);
            return prgFunc;
        }


#if DEBUG
        static JSRuntime()
        {
            SanityCheck();
        }
        static bool _sanityCheckDone = false;
        static void SanityCheck()
        {
            if (_sanityCheckDone) return;

            if (CheckType(typeof(CodeGen.Types), null, null))
                throw new InvalidProgramException();

            _sanityCheckDone = true;
        }
        static bool CheckType(Type t, object instance, string instanceName)
        {
            bool foundError = false;
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            if (instance != null)
                flags |= System.Reflection.BindingFlags.Instance;
            foreach (var f in t.GetFields(flags))
            {
                object value = null;
                if (f.IsStatic)
                    value = f.GetValue(null);
                else if (instance != null)
                    value = f.GetValue(instance);

                var fullFieldName = string.Format("{0}.{1}", instanceName ?? f.DeclaringType.FullName, f.Name);

                if (value == null)
                {
                    Debug.WriteLine("Error! Null field {0}", fullFieldName);
                    foundError = true;
                }
                else
                {
                    var fieldType = f.FieldType;
                    if (fieldType.DeclaringType == t)
                        foundError = CheckType(fieldType, value, fullFieldName) || foundError;
                }
            }
            foreach (var nt in t.GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
                foundError = CheckType(nt, null, null) || foundError;
            return foundError;
        }
#else
        static void SanityCheck() { }
#endif
    }
}
