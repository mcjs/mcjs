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
using System.Linq;
using System.Text;

using m.Util.Diagnose;

namespace mwr.DOM
{
    public partial class ContentWindow
    {
        partial void Initialize()
        {
            mjr.JSRuntime.Instance.InitGlobalContext(this);
            SetField("window", this);
        }

        // TODO: Remove this; we need instead to generate a binding based upon the NamedConstructor WebIDL attribute.
        // See http://dev.w3.org/html5/spec/the-img-element.html#the-img-element for how our IDL should look.
        public static void Image(ref mdr.CallFrame callFrame)
        {
            Debug.WriteLine("++$> calling Image constructor");
            var window = (ContentWindow) mjr.JSRuntime.Instance.GlobalContext;
            callFrame.Return.SetNullable(window.Document.CreateElement("img"));
        }

        #region Timeout
        private static void SetTimer(ref mdr.CallFrame callFrame, bool isInterval)
        {
          mdr.DFunction handler = null;
          //TODO: We might need to do it better using sth like Callable property
          if (callFrame.Arg0.ValueType == mdr.ValueTypes.Function)
          {
            handler = callFrame.Arg0.AsDFunction();
          }
          else if (callFrame.Arg0.ValueType == mdr.ValueTypes.String)
          {
            handler = HTMLRuntime.Instance.PrepareScript(callFrame.Arg0.AsString());
          }
          else
          {
            Debug.WriteLine("Invalid argument type {0} for setInterval", callFrame.Arg0.ValueType);
            //Trace.Fail("Invalid argument type {0} for setInterval", callFrame.Arg0.ValueType);
          }
          //TODO: Consider the case in which it's nor function or string

          long times = (long)callFrame.Arg1.AsInt32();
          int argCount = callFrame.PassedArgsCount;
          TimeoutEvent timeout = new TimeoutEvent(times, isInterval);
          timeout.CallFrame.Function = handler;

          Debug.WriteLineIf(
            handler == null
            , "The {0} handler function of timer {1} is null and arg0.ValueType={2} with value {3}"
            , (isInterval ? "setInterval" : "setTimeout")
            , timeout.Id
            , callFrame.Arg0.ValueType
            , mjr.Operations.Convert.ToString.Run(ref callFrame.Arg0)
          );
          //TODO: we need the following ASSERT, but for some reason, this will cause strange problems with WrappedObject.cpp:85 gcGetWrapper(), so we're going to catch this again later in TimerQueue.ProcessEvents
          //Debug.Assert(timeout.CallFrame.Function != null, "The setTimeout handler function must not be null");
          
          if (argCount > 2)
          {
            timeout.CallFrame.PassedArgsCount = argCount - 2;
            if (timeout.CallFrame.PassedArgsCount > 4)
            {
              timeout.CallFrame.Arguments = new mdr.DValue[timeout.CallFrame.PassedArgsCount - 4];
            }
            for (int i = 0; i < argCount - 2; i++)
            {
              var arg = callFrame.Arg(i + 2);
              timeout.CallFrame.SetArg(i, ref arg);
            }
          }
          HTMLRuntime.Instance.TimerQueue.SetTimeoutOrInterval(timeout);
          callFrame.Return.Set(timeout.Id);
        }
        public static void setTimeout(ref mdr.CallFrame callFrame)
        {
            Debug.WriteLine("Calling window.setTimeout");
            SetTimer(ref callFrame, false);
        }

        public static void setInterval(ref mdr.CallFrame callFrame)
        {
            Debug.WriteLine("Calling window.setInterval1");
            SetTimer(ref callFrame, true);
        }

        public static void clearTimeout(ref mdr.CallFrame callFrame)
        {
            Debug.WriteLine("Calling window.clearTimeout");
            int timer_id = callFrame.Arg0.AsInt32();
            HTMLRuntime.Instance.TimerQueue.ClearTimeout(timer_id);
        }

        public static void clearInterval(ref mdr.CallFrame callFrame)
        {
            Debug.WriteLine("Calling window.clearInterval");
            int timer_id = callFrame.Arg0.AsInt32();
            HTMLRuntime.Instance.TimerQueue.ClearInterval(timer_id);
        }
        #endregion
    }
}
