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

using m.Util.Options;
using m.Util.Diagnose;

namespace m.Util
{
  public class Configuration
  {
    protected readonly OptionSet Options = new OptionSet();
    public string[] Arguments { get; private set; }
    public List<string> UnprocessedArguments { get; protected set; }
    bool IsParsed;

    public Configuration(params string[] args)
    {
      Arguments = args;
      IsParsed = false;

      Options
        //help
        .Add("h|?|help", "shows this help", v => ShowHelpAndExit())
      ;
    }

    public void ParseArgs()
    {
      if (!IsParsed)
      {
        Parse(Arguments);
        IsParsed = true;
      }
      else
      {
        Debug.Warning("Configuration arguments already are parsed!");
      }
    }

    protected virtual void Parse(params string[] args)
    {
      var AllArgs = AddDefaultArgs(args);
      Debug.WriteLine("Used arguments: {0}", string.Join(" ", AllArgs.ToArray()));

      try
      {
        UnprocessedArguments = Options.Parse(AllArgs);
      }
      catch (OptionException e)
      {
        ShowHelp(Options);
        throw new ArgumentException(e.Message);
      }
    }

    protected void ShowHelp(OptionSet options)
    {
      var cmdName = System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
      Debug.WriteLine("Usage: {0} [options]+", cmdName);
      Debug.WriteLine("Options:");
      options.WriteOptionDescriptions(Console.Out);
    }

    public void ShowHelpAndExit()
    {
      ShowHelp(Options);
      Environment.Exit(0);
    }

    private string GetAppSetting(System.Collections.Specialized.NameValueCollection appSettings, string key, params string[] optionalPrefixes)
    {
      var value = appSettings[key];
      if (string.IsNullOrEmpty(value))
      {
        foreach (var prefix in optionalPrefixes)
        {
          value = appSettings[prefix + key];
          if (value != null)
            break;
        }

      }
      return value;
    }
    private List<string> AddDefaultArgs(string[] args)
    {
      var argsSet = new List<string>();
      var appSettings = System.Configuration.ConfigurationManager.AppSettings;
      if (appSettings != null)
      {
        var osNamespace =
              (System.Environment.OSVersion.Platform == PlatformID.Unix)
              ? "linux:"
              : "win:";

        var defaultArgs = GetAppSetting(appSettings, "DefaultArgs", osNamespace);
        if (!string.IsNullOrEmpty(defaultArgs))
          argsSet.AddRange(defaultArgs.Split(' ')); //Technically, we need to parse this string

        var prefixes = new string[] { "", "-", "--", "/", osNamespace, "-" + osNamespace, "--" + osNamespace, "/" + osNamespace };

        foreach (var option in Options)
        {
          foreach (var name in option.Names)
          {
            var value = GetAppSetting(appSettings, name, prefixes);
            if (value != null)
            {
              if (value == "-" || value == "+")
                argsSet.Add(string.Format("--{0}{1}", name, value));
              else
                argsSet.Add(string.Format("--{0}={1}", name, value));
            }
          }
        }
      }
      argsSet.AddRange(args);
      return argsSet;
    }

  }
}
