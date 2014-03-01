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

namespace mwr.DOM
{
    public class EventListener
    {
        EventListener(mdr.DFunction handler, bool useCapture)
        {
            // TODO: Unused
            /*this.eventHandler = handler;
            this.useCapture = useCapture;*/
        }

        // TODO: Unused.
        /*mdr.DFunction eventHandler;
        bool useCapture;*/
    }
    public class EventListeners
    {
        List<mdr.DFunction> _listeners;
        HashSet<mdr.DFunction> _useCaptures = new HashSet<mdr.DFunction>();
        /*mdr.DFunction _defaultListener;*/ // TODO: Unused.

        /// <summary>
        /// We use an index into the list to determine the idl onX handler
        /// we use the index because this one usually is set once, rarely read, and frequently executed
        /// therefore, this approach will be faster with less indirection
        /// </summary>
        int _idlListernerIndex = -1;

        public void Add(mdr.DFunction f, bool useCapture)
        {
            if (_listeners == null)
                _listeners = new List<mdr.DFunction>();
            _listeners.Add(f);
            if (useCapture)
                _useCaptures.Add(f);
        }
        public void Remove(mdr.DFunction f)
        {
            if (_listeners != null)
            {
                var index = _listeners.IndexOf(f);
                //For now we just set it to null, we should see what to do if it is the idlListener
                if (index >= 0)
                {
                    _listeners[index] = null;
                    _useCaptures.Remove(f);
                }
                //_listeners.RemoveAt(index);
            }
        }
        public mdr.DFunction IdlListener
        {
            get
            {
                if (_idlListernerIndex < 0)
                    return null;
                else
                    return _listeners[_idlListernerIndex];
            }
            set
            {
                if (_idlListernerIndex < 0)
                {
                    Add(value, false);
                    _idlListernerIndex = _listeners.Count - 1;
                }
                else
                    _listeners[_idlListernerIndex] = value;
            }
        }




        public bool HandleEvent(JSEvent e)
        {
            try
            {
                Debug.WriteLine("Listeners loop starts here");
                var callFrame = new mdr.CallFrame();
                callFrame.PassedArgsCount = 1;
                callFrame.Arg0.Set(e);
                callFrame.Signature = new mdr.DFunctionSignature(ref callFrame, 1);
                callFrame.This = e.CurrentTarget;
                mdr.DFunction handler;
                bool eventPropagationStopped = false;
                if (_listeners == null)
                {
                    return false;
                }
                Debug.WriteLine("Listeners loop with " + _listeners.Count + "  listeners");


                //The event listener loop (inner loop)
                for (var i = 0; i < _listeners.Count; ++i)
                {
                    handler = _listeners[i];
                    callFrame.Function = handler;

                    if (handler != null)
                    {
                        if (_useCaptures.Contains(handler))
                        {
                            e.Phase = JSEvent.Phases.Captureing;
                            //TODO: Capturing 
                        }
                        callFrame.Function.Call(ref callFrame);

                        if (e.PropagationStopped)
                        {
                            eventPropagationStopped = true;
                        }
                        if (e.ImmediatePropagationStopped)
                        {
                            eventPropagationStopped = true;
                            break;
                        }

                    }
                }

                return eventPropagationStopped;
            }
            catch (System.Exception ex)
            {
              Diagnostics.WriteException(ex, "when processing event");
              return false;
            }
        }
    }
}
