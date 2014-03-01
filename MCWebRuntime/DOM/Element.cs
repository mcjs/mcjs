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
using System.Runtime.CompilerServices;

using m.Util.Diagnose;

namespace mwr.DOM
{
    public partial class Element
    {
        #region JavaScript Trampolines
        static void getAttribute (ref mdr.CallFrame callFrame)
        {
            var elem = (Element) callFrame.This;
            var name = callFrame.Arg0.AsString ();
            mdr.PropertyDescriptor pd = callFrame.This.GetPropertyDescriptor(name);
            EventHandlerProperty ehp =null;
            if (pd != null) { 
                ehp = pd.GetProperty() as EventHandlerProperty;
            }            
            string val;
            if (ehp != null) {
                val = GetEventHandlerAttr(callFrame.This, ehp.EventType, name);
                callFrame.Return.Set(val);
                return;
            }
            val = elem.GetContentAttribute(name);
            callFrame.Return.Set(val);
        }

        static void setAttribute (ref mdr.CallFrame callFrame)
        {
            var elem = (Element) callFrame.This;
            var name = callFrame.Arg0.AsString ();
            var val = callFrame.Arg1.AsString ();
            var ehp = callFrame.This.GetPropertyDescriptor(name).GetProperty()
                as EventHandlerProperty;
            if (ehp != null) {
                SetEventHandlerAttr(callFrame.This, ehp.EventType, name, val);
                return;
            }
            elem.SetContentAttribute(name, val);
        }
        #endregion

        #region Zoomm Trampolines
        public static string GetEventHandlerAttr(mdr.DObject obj, string name)
        {
            var ehp = obj.GetPropertyDescriptor(name).GetProperty() as EventHandlerProperty;
            if (ehp == null)
                throw new Exception("Invalid Event " + name);
            return GetEventHandlerAttr(obj, ehp.EventType, name);
        }

        public static void SetEventHandlerAttr(mdr.DObject obj, string name, string script)
        {
#if ENABLE_RR
            if (RecordReplayManager.Instance != null && RecordReplayManager.Instance.RecordEnabled)
            {
                RecordReplayManager.Instance.Record("Element", null, "SetEventHandlerAttr", false, obj, name, script);
            }
#endif
            var ehp = obj.GetPropertyDescriptor(name).GetProperty() as EventHandlerProperty;
            if (ehp == null)
                throw new Exception("Invalid Event " + name);
            SetEventHandlerAttr(obj, ehp.EventType, name, script);
        }
        #endregion

        #region Event Handler Attributes
        // all event handler attribute accessors should eventually call these
        public static string GetEventHandlerAttr(mdr.DObject obj, EventTypes type, string name)
        {
#if ENABLE_RR
            if (RecordReplayManager.Instance != null && RecordReplayManager.Instance.RecordEnabled)
            {
                mwr.RecordReplayManager.Instance.Record("Element", null, "GetEventHandlerAttr", false, obj, type, name);
            }
#endif
            var targetElement = obj.FirstInPrototypeChainAs<HTMLElement>();
            var s = targetElement.PrimGetEventHandlerAttr(type);
            Debug.WriteLine("GetEventHandlerAttr({0}) = '{1}'", name, s);
            return s;
        }

        string PrimGetEventHandlerAttr(EventTypes type)
        {
            if (_handlerAttrs == null)
                return "";
            string script = "";
            if (!_handlerAttrs.TryGetValue(type, out script))
                return "";
            return script;
        }

        public static void SetEventHandlerAttr(mdr.DObject obj, EventTypes type,
                                               string name, string script)
        {
            var targetElement = obj.FirstInPrototypeChainAs<HTMLElement>();
            targetElement.PrimSetEventHandlerAttr(type, script);
            var prgFunc = HTMLRuntime.Instance.PrepareScript(script);
            var pd = obj.GetPropertyDescriptor(name);
            pd.Set(obj,prgFunc);
        }

        void PrimSetEventHandlerAttr(EventTypes type, string script)
        {
            if (_handlerAttrs == null)
                _handlerAttrs = new Dictionary<EventTypes, string>();
            _handlerAttrs[type] = script;
        }
        #endregion
    }
}
