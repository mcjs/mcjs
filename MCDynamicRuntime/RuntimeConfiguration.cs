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
using m.Util.Options;

namespace mdr
{
  public class RuntimeConfiguration : m.Util.Configuration
  {
    //Diagnose            
    public bool EnableExceptionDump { get; private set; }
    public bool EnableStackDump { get; private set; }
    public bool FailOnException { get; private set; }
    public bool RedirectAllExceptions { get; set; }

    public string OutputDir { get; protected set; }
    public string ProfilerOutput { get; protected set; }
    public bool EnableTimers { get; protected set; }
    public bool EnableCounters { get; protected set; }
    public bool ProfileStats { get; private set; }

    public RuntimeConfiguration(params string[] args)
      : base(args)
    {
      //All boolean switches are falst by default, here we can set the default if they should be true
#if DEBUG
      EnableExceptionDump = true;
      EnableStackDump = true;
#else
      RedirectAllExceptions = true;      
#endif

      OutputDir = ".";

      Options
        //Diagnose
        .Add("ed|exception-dump", "enable/disable reporting of exceptions (default is +)", v => EnableExceptionDump = v != null)
        .Add("sd|stack-dump", "enable/disable stack dump in case of an exception (default is +)", v => EnableStackDump = v != null)
        .Add("fe|fail-exceptions", "enable/disable fail on exception mode (default is -)", v => FailOnException = v != null)
        .Add("re|redirect-all-exceptions", "enable/disable capturing and masking all engine exceptions (default is -)", v => RedirectAllExceptions = v != null)
        .Add("odir=", "the name of the output directory where output results of this run will be written to. The defult is current directory", v => OutputDir = v)
        .Add("profiler-output:", "name of the file to write the pofiling results.", v => ProfilerOutput = v ?? "stats.xml")
        .Add("profile-stats", "enable/disable collecting stats on different features", v => ProfileStats = v != null)
      ;
    }
  }
}
