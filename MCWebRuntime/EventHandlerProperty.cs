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
    public class EventHandlerProperty : mdr.DProperty
    {
        public readonly EventTypes EventType;

        static EventListeners GetEventListeners(mdr.DObject obj, EventTypes eventType)
        {
            var targetElement = obj.FirstInPrototypeChainAs<EventTarget>();
            var eventListeners = targetElement.GetEventListeners(eventType, true);
            return eventListeners;
        }

        public EventHandlerProperty(EventTypes eventType)
            : base()
        {
            EventType = eventType;
            TargetValueType = mdr.ValueTypes.Function;
            //We should static or constants in the following methods to avoid expensive delegates!
            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
            {
                var eventListeners = GetEventListeners(This, eventType);
                var idlListener = eventListeners.IdlListener;
                if (idlListener != null)
                    v.Set(idlListener);
                else
                    v.Set(mdr.Runtime.Instance.DefaultDNull);
            };
            OnSetDValue = (mdr.DObject This, ref mdr.DValue v) =>
            {
                var eventListeners = GetEventListeners(This, eventType);
                Debug.WriteLine("Setting the IDL listener for event type " + eventType.ToString() + " : type " + v.ValueType.ToString());
                var idlListener = (v.ValueType == mdr.ValueTypes.Function) ? v.AsDFunction() : null;
                eventListeners.IdlListener = idlListener;
            };
        }

        public static mdr.DProperty CreateProperty(string propertyName)
        {
          EventTypes type = JSEvent.GetPropertyEventType(propertyName);

          if (type == EventTypes.ZoommInvalid)
            throw new Exception(String.Format("Unimplemented EventHandlerProperty {0}", propertyName));
          else
            return new EventHandlerProperty(type);
        }
    }
}
