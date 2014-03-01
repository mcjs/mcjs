// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

using mwr;
using mjr;
using m.Util.Diagnose;

namespace MCJavascript
{
  public class Program
  {
    mjr.JSRuntime _runtime;
    LinkedList<KeyValuePair<string, string>> _scripts;

    void LoadRuntime(ProgramConfiguration config)
    {
      _runtime = new mjr.JSRuntime(config);

      var globalContext = new mdr.DObject();
      _runtime.InitGlobalContext(globalContext);
      _runtime.SetGlobalContext(globalContext);

      _scripts = new LinkedList<KeyValuePair<string, string>>();

      foreach (var s in config.ScriptFileNames)
        _scripts.AddLast(new KeyValuePair<string, string>(s, _runtime.ReadAllScript(s)));

      foreach (var s in config.ScriptStrings)
      {
        var MD5 = new MD5CryptoServiceProvider();
        var key = BitConverter.ToString(MD5.ComputeHash(Encoding.UTF8.GetBytes(s)));
        _scripts.AddLast(new KeyValuePair<string, string>(key, s));
      }
    }

    public delegate void Initializer(mjr.JSRuntime runtime);
    public int Run(Initializer initializer, ProgramConfiguration config)
    {
      var errorCount = 0;
      //var color = ConsoleColor.Blue;
      const int Repeats = 5;
      for (var i = 1; i < Repeats; ++i)
      {
        if (config.MeasureTime)
        {
          //var t = Util.Timers.Start("Write header");
          //if (++color == ConsoleColor.White)
          //    color = ConsoleColor.Blue;
          //Console.ForegroundColor = color;
          Debug.WriteLine("------- Running #{0} -------\n", i);
          //Console.ResetColor();
          //Timers.Stop(t);
        }
        else
          i = Repeats; //One iteration is enough!

        LoadRuntime(config);

        if (initializer != null)
        {
          //This means we are being called from the source code debugger. That will rely on the fact that all scripts are already parsed
          foreach (var pair in _scripts)
            _runtime.Scripts.GetOrAdd(pair.Value, pair.Key, _runtime);

          initializer(_runtime);
        }

        foreach (var pair in _scripts)
          errorCount += _runtime.RunScriptString(pair.Value, pair.Key);

        _runtime.ShutDown();
      }
      return errorCount;
    }

    void DoAll(mjr.JSFunctionMetadata func, ProgramConfiguration config)
    {
      if (config.DumpIRGraph)
      {
        func.Analyze();
        IRGraphWriter.Execute(func);
      }
      else
        if (config.OnlyJit)
        {
          func.JitSpeculatively(ref mdr.DFunctionSignature.EmptySignature);
        }
        else if (config.OnlyAnalyze)
        {
          func.Analyze();
        }
        else if (config.OnlyParse)
        {
          // Already parsed everything during load.
        }
        else if (config.OnlyLex)
        {
          // Already lexed everything during load.
        }
      foreach (var f in func.SubFunctions)
        DoAll(f, config);
    }
    public int Run(params string[] args)
    {
      var config = new ProgramConfiguration(args);
      var engine = new mwr.HTMLEngine(config);
      int result;
      if (config.EnableReplay)
      {
        result = mwr.RecordReplayManager.RunReplay(config.ReplayFilename, config.ReplayParams);
      }
      else
        if (config.DumpJSOnly || !string.IsNullOrEmpty(config.InstJSPrefix))
        {
          LoadRuntime(config);

          foreach (var pair in _scripts)
          {
            var Filename = System.IO.Path.GetFullPath(System.IO.Path.Combine(config.OutputDir, System.IO.Path.GetFileNameWithoutExtension(pair.Key) + "_inst.js"));
            var output = new System.IO.StreamWriter(Filename);
            //mjr.AstWriter writer;
            //if (config.DumpJSOnly)
            //    writer = new mjr.AstWriter(output);
            //else
            //    writer = new mjr.CodeGen.JSIntrumentor(output);
            //writer.Execute(_runtime.Scripts.GetOrAdd(pair.Value, pair.Key, _runtime));
            output.Close();
          }
          result = 0;
        }
        else if (config.OnlyLex || config.OnlyParse || config.OnlyAnalyze || config.OnlyJit || config.DumpIRGraph)
        {
          LoadRuntime(config);
          foreach (var pair in _scripts)
            DoAll(_runtime.Scripts.GetOrAdd(pair.Value, pair.Key, _runtime), config);
          _runtime.ShutDown();
          result = 0;
        }
        else
          result = Run(null, config);
      engine.ShutDown();
      return result;
    }

    static int Main(string[] args)
    {
      try
      {
        try
        {
          return new Program().Run(args);
        }
        catch (Exception e)
        {
          Diagnostics.WriteException(e);
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("Caught rethrown exception {0} that would passed to browser", e.Message);
      }
      return 1;
    }
  }
}
