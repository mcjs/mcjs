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

namespace mdr
{
  /// <summary>
  /// This is a singleton class that hold the global information accross all instances of runtime. 
  /// </summary>
  public class Engine
  {
    public static Engine Instance { get; private set; }

    public readonly RuntimeConfiguration Configuration;
    
    public Engine(params string[] args)
      : this(new RuntimeConfiguration(args))
    {}
    
    public Engine(RuntimeConfiguration configuration)
    {
      Trace.Assert(Instance == null, "Cannot have more than one instance of Engine");
      Engine.Instance = this;
      Configuration = configuration;
      configuration.ParseArgs(); // Do this now before anyone tries to read any configuration value.

      // Select Diagnose settings based on configuration.
      Diagnostics.EnableExceptionDump = configuration.EnableExceptionDump;
      Diagnostics.EnableStackDump = configuration.EnableStackDump;
      Diagnostics.FailOnException = configuration.FailOnException;
      Diagnostics.RedirectAllExceptions = configuration.RedirectAllExceptions;
    }
    
    public virtual void ShutDown()
    {
      Debug.WriteLine("Shutting down the engine");
      Instance = null;
    }
  }
}
