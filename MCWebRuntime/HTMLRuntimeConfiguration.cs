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

namespace mwr
{
  public class HTMLRuntimeConfiguration : mjr.JSRuntimeConfiguration
  {
    //Record and Replay
    public bool EnableRecord { get; private set; }
    public bool EnableReplay { get; set; }

    public string RecordFilename { get; private set; }
    public string RecordParams { get; private set; }
    public string ReplayFilename { get; private set; }
    public string ReplayParams { get; private set; }

    //Profiling
    public bool ProfileTimerTime { get; private set; }
    public bool ProfileEventTime { get; private set; }
    public bool ProfileBindingTime { get; private set; }

    public HTMLRuntimeConfiguration(params string[] args)
      : base(args)
    {
      Options
        //Record and Replay
        .Add("record", "enable/disable record mode (default is -)", v => EnableRecord = v != null)
        .Add("record-filename=", "sets record/replay filename (default is 'session.xml')", (string v) => RecordFilename = v)
        .Add("record-params=", "sets record/replay parameters", (string v) => RecordParams = v)
        .Add("replay", "enable/disable replay mode (default is -)", v => EnableReplay = v != null)
        .Add("replay-filename=", "sets record/replay filename", v => ReplayFilename = v)
        .Add("replay-params=", "sets record/replay parameters", v => ReplayParams = v)

        .Add("profile-event-time", "enable/disable profiling of events (default is -)", v => ProfileEventTime = v != null)
        .Add("profile-timer-time", "enable/disable profiling of timers (default is -)", v => ProfileTimerTime = v != null)
        .Add("profile-binding-time", "enable/disable profiling of binding calls (default is +)", v => ProfileBindingTime = v != null)
      ;
    }

    protected override void Parse(params string[] args)
    {
      base.Parse(args);
#if !ENABLE_RR
      m.Util.Diagnose.Trace.Assert(!EnableRecord && !EnableReplay, "Code is not compiled for Record & Replay!");
#endif
    }
  }
}
