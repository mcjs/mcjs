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
using System.Reflection;
using System.IO;



using IDLCodeGen.Targets;
using IDLCodeGen.Util;

namespace IDLCodeGen
{
  class ProgramConfiguration : m.Util.Configuration
  {
    public string OutputDir { get; private set; }
    public string IdlXmlFilepath { get; private set; }
    public Dictionary<Type, string> SelectedTargets { get; private set; }

    public ProgramConfiguration(string[] args)
      : base(args)
    {
      OutputDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      SelectedTargets = new Dictionary<Type, string>();

      var targets = from t in Assembly.GetExecutingAssembly().GetTypes()
                    where t.FullName.StartsWith("IDLCodeGen.Targets") && !t.IsAbstract
                    select t;

      Options
        .Add("outdir=", "default output directory", v => OutputDir = v)
        .Add("idl=", "path to idl xml file", v => IdlXmlFilepath = Path.GetFullPath(v))
      ;

      foreach (var t in targets)
      {
        var type = t; // to make sure delegate closes on the correct value
        Options.Add(
          string.Format("{0}:", t.Name),
          string.Format("Generate {0} to default outdir or the specified path", t.Name),
          v => SelectedTargets[type] = v
        );
      }

    }
  }

  class Program
  {
    internal static ProgramConfiguration Configuration { get; private set; }
    static void Main(string[] args)
    {
      try
      {
        Configuration = new ProgramConfiguration(args);
        Configuration.ParseArgs();

        if (Configuration.SelectedTargets.Count == 0)
          Configuration.ShowHelpAndExit();

        foreach (var t in Configuration.SelectedTargets)
        {
          var ctor = t.Key.GetConstructor(new Type[] { });
          var obj = ctor.Invoke(new object[] { });
          var target = obj as Target;

          string outputFilepath;
          if (string.IsNullOrEmpty(t.Value))
            outputFilepath = Path.GetFullPath(Path.Combine(Configuration.OutputDir, target.Filename));
          else
            outputFilepath = Path.GetFullPath(t.Value);

          using (var outFile = new StreamWriter(outputFilepath))
          {
            System.Console.WriteLine("Generating {0}...".Formatted(outputFilepath));

            target.AddMetadata("Generator class: {0}".Formatted(t.Key.FullName));
            target.AddMetadata("Output File: {0}".Formatted(outputFilepath));

            target.Generate(outFile);
          }
        }

        //Generate(args);
      }
      catch (Exception e)
      {
        Console.WriteLine("ERROR: {0}", e);
      }
    }

    //private static List<string> AddDefaultArgs(string[] args)
    //{
    //  var argsSet = new List<string>();
    //  var appSettings = System.Configuration.ConfigurationManager.AppSettings;
    //  if (appSettings != null)
    //  {
    //    var defaultArgs = appSettings["DefaultArgs"];
    //    if (string.IsNullOrEmpty(defaultArgs))
    //    {
    //      var osNamespace =
    //        (System.Environment.OSVersion.Platform == PlatformID.Unix)
    //        ? "unix:"
    //        : "win:";
    //      defaultArgs = appSettings[osNamespace + "DefaultArgs"];
    //    }
    //    if (!string.IsNullOrEmpty(defaultArgs))
    //      argsSet.AddRange(defaultArgs.Split(' '));
    //  }
    //  argsSet.AddRange(args);
    //  return argsSet;
    //}

    //private static void Generate(string[] args)
    //{
    //  string IdlXmlFilepath = null;
    //  string OutputDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    //  bool ShowHelp = false;
    //  var options = new NDesk.Options.OptionSet()
    //  {
    //    {"outdir=", "default output directory", v => OutputDir = v},
    //    {"idl=", "path to idl xml file", v => IdlXmlFilepath = Path.GetFullPath(v)},
    //    {"h|help", "", v => ShowHelp = v!=null}
    //  };


    //  var targets = from t in Assembly.GetExecutingAssembly().GetTypes()
    //                where t.FullName.StartsWith("IDLCodeGen.Targets") && !t.IsAbstract
    //                select t;

    //  var selectedTargets = new Dictionary<Type, string>();
    //  foreach (var t in targets)
    //  {
    //    var type = t; // to make sure delegate closes on the correct value
    //    options.Add(
    //      string.Format("{0}:", t.Name),
    //      string.Format("Generate {0} to default outdir or the specified path", t.Name),
    //      v => selectedTargets[type] = v
    //    );
    //  }

    //  options.Parse(AddDefaultArgs(args));

    //  if (ShowHelp || string.IsNullOrEmpty(IdlXmlFilepath) || selectedTargets.Count == 0)
    //    options.WriteOptionDescriptions(Console.Out);
    //  else if (!System.IO.File.Exists(IdlXmlFilepath))
    //    throw new Exception(string.Format("Invalid IDL xml file '{0}'", IdlXmlFilepath));
    //  else
    //  {
    //    foreach (var t in selectedTargets)
    //    {
    //      var ctor = t.Key.GetConstructor(new Type[] {});
    //      var obj = ctor.Invoke(new object[] {});
    //      var target = obj as Target;

    //      string outputFilepath;
    //      if (string.IsNullOrEmpty(t.Value))
    //        outputFilepath = Path.GetFullPath(Path.Combine(OutputDir, target.Filename));
    //      else
    //        outputFilepath = Path.GetFullPath(t.Value);

    //      using (var outFile = new StreamWriter(outputFilepath))
    //      {
    //        System.Console.WriteLine("Generating {0}...".Formatted(outputFilepath));

    //        target.AddMetadata("File: {0}".Formatted(outputFilepath));
    //        target.AddMetadata("IDLXML: {0}".Formatted(IdlXmlFilepath));
    //        target.AddMetadata("Generator class: {0}".Formatted(t.Key.FullName));

    //        using (var idlFile = new StreamReader(IdlXmlFilepath))
    //          target.Generate(outFile, idlFile);
    //      }
    //    }
    //  }
    //}
  }
}
