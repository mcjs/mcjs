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
using System.IO;

namespace mjr
{
  public class JSEngine : mdr.Engine
  {
    public new static JSRuntimeConfiguration Configuration { get { return (JSRuntimeConfiguration)mdr.Engine.Instance.Configuration; } }

    public JSEngine(params string[] args)
      : this(new JSRuntimeConfiguration(args))
    { }

    public JSEngine(JSRuntimeConfiguration configuration)
      : base(configuration)
    {
      if ((configuration.EnableCounters || configuration.EnableTimers) && configuration.ProfilerOutput != null)
      {
        using (var output = File.CreateText(configuration.ProfilerOutput))
        {
          output.WriteLine("<Results>");
          output.WriteLine("  <JSArgs>{0}</JSArgs>", string.Join(" ", configuration.Arguments));
        }
      }
      int minWorker, minIOC;
      System.Threading.ThreadPool.GetMinThreads(out minWorker, out minIOC);
      System.Threading.ThreadPool.SetMinThreads(4, minIOC);
    }

    public override void ShutDown()
    {
      if ((Configuration.EnableCounters || Configuration.EnableTimers) && Configuration.ProfilerOutput != null)
      {
        using (var output = File.AppendText(Configuration.ProfilerOutput))
        {
          output.WriteLine("</Results>");
        }
      }
      base.ShutDown();

    }
  }
}
