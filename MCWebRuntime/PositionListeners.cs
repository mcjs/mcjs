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

namespace mwr
{
    public class PositionListenerOptions
    {
        PositionListenerOptions(int timeout)
        {
            this.timeout = timeout;
        }

        public int timeout;
    }
    public class PositionListener
    {
        public PositionListener(mdr.DFunction handler, mdr.DFunction errorHandler, PositionListenerOptions options)
        {
            this._handler = handler;
            // TODO: Unused.
            /*this._errorHandler = errorHandler;
            this._options = options;*/
            _callFrame = new mdr.CallFrame();
            _callFrame.Function = _handler;
        }

        public bool ApplyOption()
        {
            return true;
        }

        public void Dispatch(JSPosition position)
        {
            _callFrame.PassedArgsCount = 1;
            _callFrame.Arg0.Set(position);
            _callFrame.This = HTMLRuntime.Instance.GlobalContext;
            _callFrame.Signature = new mdr.DFunctionSignature(ref _callFrame, 1);
            _callFrame.Function.Call(ref _callFrame);
        }

        private mdr.DFunction _handler;
        /*private mdr.DFunction _errorHandler;*/ // TODO: Unused.
        private mdr.CallFrame _callFrame;
        /*private PositionListenerOptions _options;*/ // TODO: Unused.

    }
    public class PositionListeners
    {
        private Dictionary<int, PositionListener> _locationListeners = new Dictionary<int, PositionListener>();
        private static int _lastID = 0;
        private bool _watch = false;
        private List<int> _deleteIDList = new List<int>();
        private bool _dispatching = false;
        public int ListenerCount()
        {
            return _locationListeners.Count;
        }
        public PositionListeners(bool watchListener)
        {
            _watch = watchListener;
        }

        public void AddNewListener(PositionListener listener, out int id)
        {
            id = _lastID;
            _locationListeners.Add(id, listener);
            _lastID++;
        }

        public void RemoveListener(int id)
        {
            if (_locationListeners.ContainsKey(id))
            {
                if (!_dispatching)
                {
                    Debug.WriteLine("Removing position listener {0} directly!", id);
                    _locationListeners.Remove(id);
                }
                else
                {
                    Debug.WriteLine("Delaying removing position listener {0}!", id);
                    _deleteIDList.Add(id);
                }
            }
        }

        public void Dispatch(JSPosition position)
        {
            Debug.WriteLine("Calling Dispatch position with watch: {0} Count: {1}", _watch, _locationListeners.Count);
            if (_locationListeners == null || _locationListeners.Count == 0)
                return;
            _deleteIDList.Clear();
            _dispatching = true;
            foreach (KeyValuePair<int, PositionListener> pair in _locationListeners)
            {
                PositionListener listener = pair.Value;
                Debug.WriteLine("Calling listener ApplyOption!");
                if (listener.ApplyOption())
                {
                    Debug.WriteLine("Calling listener Dispatch!");
                    listener.Dispatch(position);
                    if (!_watch){
                        _deleteIDList.Add(pair.Key);
                    }
                }
            }
            foreach (int id in _deleteIDList)
            {
                Debug.WriteLine("Removing position listener {0}!", id);
                _locationListeners.Remove(id);
            }
            _dispatching = false;
        }


    }
}
