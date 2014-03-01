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
    public partial class EventTarget
    {
        List<EventListeners> _listenersCollection;
        protected Dictionary<EventTypes, string> _handlerAttrs;

        public EventListeners GetEventListeners(EventTypes eventType, bool createIfNull)
        {
            var index = (int)eventType;
            if (_listenersCollection == null)
                _listenersCollection = new List<EventListeners>();

            while (index >= _listenersCollection.Count)
                _listenersCollection.Add(null);
            var eventListeners = _listenersCollection[index];
            if (eventListeners == null && createIfNull)
            {
                eventListeners = new EventListeners();
                _listenersCollection[index] = eventListeners;
            }
            return eventListeners;
        }

        public static EventListeners GetEventListeners(mdr.DObject obj, string eventName)
        {
            Debug.Assert(obj != null, "invalid null object for adding event '{0}' listener", eventName);
            var pd = obj.GetPropertyDescriptor(eventName);
            Debug.Assert(pd != null , "cannot find property descriptor for event {0}", eventName);
            Debug.Assert(pd.IsAccessorDescriptor && pd.IsInherited, "invalid property descriptor type {0} for event {1}", pd.GetAttributes(), eventName);
            // Debug.Assert(pd.IsAccessorDescriptor, "invalid property discriptor type {0} for event {1}", pd.GetAttributes(), eventName);
            var eventHandler = pd.GetProperty() as EventHandlerProperty;
            Debug.Assert(eventHandler != null, "invalid event handler at index {0}", pd.Index);
            var eventTarget = obj as EventTarget;
            Debug.Assert(eventTarget != null, "Invalid event target object type {0}", obj.GetType().FullName);
            if (eventHandler == null || eventTarget == null)
              return null;
            var eventListeners = eventTarget.GetEventListeners(eventHandler.EventType, true);
            return eventListeners;
        }

				public static void addEventListener(ref mdr.CallFrame callFrame)
				{
					var eventName = "on" + callFrame.Arg0.AsString();
					if (callFrame.Arg1.ValueType == mdr.ValueTypes.Undefined)
					{
							//This is a special case! We just cannot continue! Need to drop the handler!
							return;
					}
					var listenerFunction = callFrame.Arg1.AsDFunction();
					var eventListeners = GetEventListeners(callFrame.This, eventName);
          if (eventListeners == null)
            return;
					Debug.WriteLine("Adding a new event listener for " + eventName + " of type " + callFrame.Arg1.ValueType.ToString());
					var useCapture = mjr.Operations.Convert.ToBoolean.Run(ref callFrame.Arg2);
					eventListeners.Add(listenerFunction, useCapture);
				}

				public static void removeEventListener(ref mdr.CallFrame callFrame)
				{
					var eventName = "on" + callFrame.Arg0.AsString();
					var listenerFunction = callFrame.Arg1.AsDFunction();
					var eventListeners = GetEventListeners(callFrame.This, eventName);
          if (eventListeners == null)
            return;
          eventListeners.Remove(listenerFunction);
				}

				public static void dispatchEvent(ref mdr.CallFrame callFrame)
				{
					var eventObj = callFrame.Arg0.AsDObject() as JSEvent;
					Debug.Assert(eventObj != null, "Invalid event object type {0}", callFrame.Arg0.AsDObject().GetType().FullName);
					var eventTarget = callFrame.This as EventTarget;
					Debug.Assert(eventTarget != null, "Invalid event target object type {0}", callFrame.This.GetType().FullName);
					eventObj.Target = eventTarget;
					eventObj.CurrentTarget = eventTarget;
					bool cancelled = eventObj.Dispatch();
					callFrame.Return.Set(cancelled);
				}
    }
}
