// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

using mwr.DOM;
using m.Util.Diagnose;

namespace mwr
{
    /// <summary>
    /// We store all event data in this struct so that we can easily transfer data between C++ & C#
    /// http://dev.w3.org/2006/webapi/DOM-Level-3-Events/html/DOM3-Events.html#events-Event
    /// </summary>

    // Remember to update the C++ version of this enumeration if you make any changes.
    public enum EventTypes
    {
        ZoommInvalid,

        Abort,
        Blur,
        CanPlay,
        CanPlayThrough,
        Change,
        Click,
        CompositionStart,
        CompositionUpdate,
        CompositionEnd,
        ContextMenu,
        CueChange,
        DblClick,
        DOMActivate,
        DOMAttributeNameChanged,
        DOMAttrModified,
        DOMCharacterDataModified,
        DOMElementNameChanged,
        DOMFocusIn,
        DOMFocusOut,
        DOMNodeInserted,
        DOMNodeInsertedIntoDocument,
        DOMNodeRemoved,
        DOMNodeRemovedFromDocument,
        DOMSubtreeModified,
        DOMContentLoaded,
        Drag,
        DragEnd,
        DragEnter,
        DragLeave,
        DragOver,
        DragStart,
        Drop,
        DurationChange,
        Emptied,
        Ended,
        Error,
        Focus,
        FocusIn,
        FocusOut,
        Input,
        Invalid,
        KeyDown,
        KeyPress,
        KeyUp,
        Load,
        LoadedData,
        LoadedMetadata,
        LoadStart,
        MouseDown,
        MouseEnter,
        MouseLeave,
        MouseMove,
        MouseOut,
        MouseOver,
        MouseUp,
        MouseWheel,
        Pause,
        Play,
        Playing,
        Progress,
        RateChange,
        Reset,
        Resize,
        Scroll,
        Seeked,
        Seeking,
        Select,
        Show,
        Stalled,
        Submit,
        Suspend,
        TextInput,
        TimeUpdate,
        Unload,
        VolumeChange,
        Waiting,
        Wheel,

        ZoommEmpty,    // Work item was successfully processed, there was no DOM event returned
        ZoommTimeout,  // Timeout expired before work item arrived
        ZoommUnpaused, // The dispatcher was unpaused, so caller should recalculate timeouts
        ZoommStop,     // The DOM dispatcher is stopped, so shut everything down
        ZoommYield,    // Exit event loop to yield back to caller of Page::processEvents
    }


    public enum EventClasses
    {
        Event,
        UIEvent,
        MouseEvent,

        TheLastIndex //This should always be the last one!
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EventData
    {
        public EventClasses EventClass;
        #region Event
        public EventTypes Type;
        public WrappedObject Target;
        public UInt64 TimeStamp;
        #endregion

        #region UIEvent
        //add UIevent data here

        public Int32 detail;
        #endregion

        #region MouseEvent
        public Int32 screenX;
        public Int32 screenY;
        public Int32 clientX;
        public Int32 clientY;
        public Int32 pageX;
        public Int32 pageY;
        public bool ctrlKey;
        public bool shiftKey;
        public bool altKey;
        public bool metaKey;
        public UInt32 button;
        public UInt32 buttons;
        public WrappedObject relatedTarget;

        #endregion
    }


    public class JSEvent : mdr.DObject
    {
        static Dictionary<string, EventTypes> eventTypesToString = new Dictionary<string, EventTypes>();

        public static EventTypes GetEventType(string eventName)
        {
            EventTypes type;

            if (eventTypesToString.TryGetValue(eventName, out type))
                return type;
            else
                return EventTypes.ZoommInvalid;
        }

        public static EventTypes GetPropertyEventType(string propertyName)
        {
            if (propertyName.Length < 3 || propertyName[0] != 'o' || propertyName[1] != 'n')
                return EventTypes.ZoommInvalid;
            else
                return GetEventType(propertyName.Substring(2));
        }

        static JSEvent()
        {
            // From HTMLElement / ContentWindow IDL
            eventTypesToString.Add("abort", EventTypes.Abort);
            eventTypesToString.Add("blur", EventTypes.Blur);
            eventTypesToString.Add("canplay", EventTypes.CanPlay);
            eventTypesToString.Add("canplaythrough", EventTypes.CanPlayThrough);
            eventTypesToString.Add("change", EventTypes.Change);
            eventTypesToString.Add("click", EventTypes.Click);
            eventTypesToString.Add("contextmenu", EventTypes.ContextMenu);
            eventTypesToString.Add("cuechange", EventTypes.CueChange);
            eventTypesToString.Add("dblclick", EventTypes.DblClick);
            eventTypesToString.Add("drag", EventTypes.Drag);
            eventTypesToString.Add("dragend", EventTypes.DragEnd);
            eventTypesToString.Add("dragenter", EventTypes.DragEnter);
            eventTypesToString.Add("dragleave", EventTypes.DragLeave);
            eventTypesToString.Add("dragover", EventTypes.DragOver);
            eventTypesToString.Add("dragstart", EventTypes.DragStart);
            eventTypesToString.Add("drop", EventTypes.Drop);
            eventTypesToString.Add("durationchange", EventTypes.DurationChange);
            eventTypesToString.Add("emptied", EventTypes.Emptied);
            eventTypesToString.Add("ended", EventTypes.Ended);
            eventTypesToString.Add("error", EventTypes.Error);
            eventTypesToString.Add("focus", EventTypes.Focus);
            eventTypesToString.Add("input", EventTypes.Input);
            eventTypesToString.Add("invalid", EventTypes.Invalid);
            eventTypesToString.Add("keydown", EventTypes.KeyDown);
            eventTypesToString.Add("keypress", EventTypes.KeyPress);
            eventTypesToString.Add("keyup", EventTypes.KeyUp);
            eventTypesToString.Add("load", EventTypes.Load);
            eventTypesToString.Add("loadeddata", EventTypes.LoadedData);
            eventTypesToString.Add("loadedmetadata", EventTypes.LoadedMetadata);
            eventTypesToString.Add("loadstart", EventTypes.LoadStart);
            eventTypesToString.Add("mousedown", EventTypes.MouseDown);
            eventTypesToString.Add("mousemove", EventTypes.MouseMove);
            eventTypesToString.Add("mouseout", EventTypes.MouseOut);
            eventTypesToString.Add("mouseover", EventTypes.MouseOver);
            eventTypesToString.Add("mouseup", EventTypes.MouseUp);
            eventTypesToString.Add("mousewheel", EventTypes.MouseWheel);
            eventTypesToString.Add("pause", EventTypes.Pause);
            eventTypesToString.Add("play", EventTypes.Play);
            eventTypesToString.Add("playing", EventTypes.Playing);
            eventTypesToString.Add("progress", EventTypes.Progress);
            eventTypesToString.Add("ratechange", EventTypes.RateChange);
            eventTypesToString.Add("reset", EventTypes.Reset);
            eventTypesToString.Add("scroll", EventTypes.Scroll);
            eventTypesToString.Add("seeked", EventTypes.Seeked);
            eventTypesToString.Add("seeking", EventTypes.Seeking);
            eventTypesToString.Add("select", EventTypes.Select);
            eventTypesToString.Add("show", EventTypes.Show);
            eventTypesToString.Add("stalled", EventTypes.Stalled);
            eventTypesToString.Add("submit", EventTypes.Submit);
            eventTypesToString.Add("suspend", EventTypes.Suspend);
            eventTypesToString.Add("timeupdate", EventTypes.TimeUpdate);
            eventTypesToString.Add("volumechange", EventTypes.VolumeChange);
            eventTypesToString.Add("waiting", EventTypes.Waiting);

            // Others
            eventTypesToString.Add("unload", EventTypes.Unload);
            eventTypesToString.Add("focusin", EventTypes.FocusIn);
            eventTypesToString.Add("focusout", EventTypes.FocusOut);
            eventTypesToString.Add("mouseenter", EventTypes.MouseEnter);
            eventTypesToString.Add("resize", EventTypes.Resize);
            eventTypesToString.Add("textinput", EventTypes.TextInput);
            eventTypesToString.Add("compositionstart", EventTypes.CompositionStart);
            eventTypesToString.Add("compositionupdate", EventTypes.CompositionUpdate);
            eventTypesToString.Add("compositionend", EventTypes.CompositionEnd);

            // DOM events
            eventTypesToString.Add("DOMActivate", EventTypes.DOMActivate);
            eventTypesToString.Add("DOMAttributeNameChanged", EventTypes.DOMAttributeNameChanged);
            eventTypesToString.Add("DOMAttrModified", EventTypes.DOMAttrModified);
            eventTypesToString.Add("DOMCharacterDataModified", EventTypes.DOMCharacterDataModified);
            eventTypesToString.Add("DOMElementNameChanged", EventTypes.DOMElementNameChanged);
            eventTypesToString.Add("DOMFocusIn", EventTypes.DOMFocusIn);
            eventTypesToString.Add("DOMFocusOut", EventTypes.DOMFocusOut);
            eventTypesToString.Add("DOMNodeInserted", EventTypes.DOMNodeInserted);
            eventTypesToString.Add("DOMNodeInsertedIntoDocument", EventTypes.DOMNodeInsertedIntoDocument);
            eventTypesToString.Add("DOMNodeRemoved", EventTypes.DOMNodeRemoved);
            eventTypesToString.Add("DOMNodeRemovedFromDocument", EventTypes.DOMNodeRemovedFromDocument);
            eventTypesToString.Add("DOMSubtreeModified", EventTypes.DOMSubtreeModified);
            eventTypesToString.Add("DOMContentLoaded", EventTypes.DOMContentLoaded);

        }

        public enum Phases
        {
            Captureing = 1,
            AtTarget = 2,
            Bubbling = 3,
        }
        public WrappedObject CurrentTarget;
        public WrappedObject Target;
        public Phases Phase;

        public bool Bubbles = false;
        public bool Cancelable = false;
        public bool DefaultPrevented = false;
        public bool PropagationStopped = false;
        public bool ImmediatePropagationStopped = false;
        public bool hasDefaultAction = false;
        public bool IsTrusted;

        /// <summary>
        /// We separate data from the rest so that we can get it directly from C++ side
        /// </summary>
        public EventData Data = new EventData();

        //public Event(Types type, bool bubbles, bool cancelable)
        //    : base(HTMLRuntime.Instance.EventMap)
        //{ }

        public void InitEventState()
        {
            DefaultPrevented = false;
            Cancelable = false;
            PropagationStopped = false;
            ImmediatePropagationStopped = false;
        }

        public void StopPropagation()
        {
            PropagationStopped = true;
        }

        public void StopImmediatePropagation()
        {
            ImmediatePropagationStopped = true;
        }

        public void PreventDefault()
        {
            DefaultPrevented = true;
        }

        /// <summary>
        /// Usually, we first need to create the Event object, have it data filled by C++,
        /// then assign proper prototype to this object
        /// </summary>
        public void UpdateMap()
        {
            Map = HTMLRuntime.Instance.GetPropertyMapOfEventPrototype(Data.EventClass);
            //            Map = mjr.JSRuntime.Instance.GetPropertyMapOfEventPrototype(Data.EventClass);
        }

        public void InitializeCreatedEvent(EventClasses eventClass, WrappedObject target)
        {
            Data.EventClass = eventClass;
            Data.Type = EventTypes.ZoommInvalid;
            Data.Target = target;
            CurrentTarget = target;
            Bubbles = false;
            Cancelable = true;
            DefaultPrevented = false;
            IsTrusted = false;
            Phase = Phases.AtTarget;
            InitializeEventFlags();
            UpdateMap();
        }

        public void InitializeEventFlags()
        {
            switch (Data.Type)
            {
                case EventTypes.Click:
                case EventTypes.MouseDown:
                case EventTypes.MouseUp:
                case EventTypes.MouseEnter:
                case EventTypes.MouseOut:
                case EventTypes.MouseOver:
                case EventTypes.MouseMove:
                case EventTypes.MouseWheel:
                    {
                    Bubbles = true;
                    hasDefaultAction = true;
                    if (Data.Type == EventTypes.MouseMove)
                        Cancelable = false;
                    else
                        Cancelable = true;
                    return;
                }
            }
        }

        public void InitEvent(EventTypes type, bool bubbles, bool cancelable)
        {
            this.Data.Type = type;
            Data.Target = null;
            Bubbles = bubbles;
            Cancelable = cancelable;
        }



        public static JSEvent createEvent(string eventClassString, WrappedObject target)
        {
            JSEvent evt = null;
            EventClasses eventClass = EventClasses.Event;
            if (eventClassString == "Event")
            {
                eventClass = EventClasses.Event;
            }
            else if (eventClassString == "UIEvent")
            {
                eventClass = EventClasses.UIEvent;
            }
            else if (eventClassString == "MouseEvent")
            {
                eventClass = EventClasses.MouseEvent;
            }
            else
            {
                //Throw Unsupported error exception according to W3C createEvent documentation
            }
            evt = new JSEvent();
            evt.InitializeCreatedEvent(eventClass, target);

            return evt;
        }

        public static mdr.DObject GetPrototype(EventClasses eventClass)
        {
            mdr.DObject prototype = null;
            switch (eventClass)
            {
                case EventClasses.Event:
                    {
                        prototype = new mdr.DObject();
                        prototype.DefineOwnProperty("type", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set(ev.Data.Type.ToString().ToLower());
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("screenX", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set((int)ev.Data.screenX);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("screenY", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set((int)ev.Data.screenY);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("clientX", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set((int)ev.Data.clientX);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("clientY", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set((int)ev.Data.clientY);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("pageX", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set((int)ev.Data.pageX);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("pageY", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set((int)ev.Data.pageY);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("buttons", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set((int)ev.Data.buttons);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("button", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set((int)ev.Data.button);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("metaKey", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set(ev.Data.metaKey);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("ctrlKey", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set(ev.Data.ctrlKey);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("shiftKey", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set(ev.Data.shiftKey);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("altKey", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set(ev.Data.altKey);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("target", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set(ev.Target);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("currentTarget", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set(ev.CurrentTarget);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("eventPhase", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set((UInt32)ev.Phase);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("bubbles", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set(ev.Bubbles);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        prototype.DefineOwnProperty("cancelable", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set(ev.Cancelable);
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);


                        prototype.SetField("initEvent", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
                        {
                            string eventTypeString = callFrame.Arg0.AsString();
                            bool bubbles = mjr.Operations.Convert.ToBoolean.Run(ref callFrame.Arg1);
                            bool cancelable = mjr.Operations.Convert.ToBoolean.Run(ref callFrame.Arg2);

                            var ev = callFrame.This.FirstInPrototypeChainAs<JSEvent>();
                            EventTypes type;
                            eventTypesToString.TryGetValue(eventTypeString, out type);
                            ev.InitEvent(type, bubbles, cancelable);

                        }));
                        prototype.SetField("stopPropagation", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
                        {
                            var ev = callFrame.This.FirstInPrototypeChainAs<JSEvent>();
                            ev.StopPropagation();

                        }));
                        prototype.SetField("stopImmediatePropagation", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
                        {
                            var ev = callFrame.This.FirstInPrototypeChainAs<JSEvent>();
                            ev.StopImmediatePropagation();

                        }));
                        prototype.SetField("preventDefault", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
                        {
                            var ev = callFrame.This.FirstInPrototypeChainAs<JSEvent>();
                            ev.PreventDefault();

                        }));
                        //Fill the rest ...
                        break;
                    }
                case EventClasses.UIEvent:
                    {
                        prototype = new mdr.DObject(HTMLRuntime.Instance.GetPropertyMapOfEventPrototype(EventClasses.Event));
                        /*
                                                prototype.DefineOwnProperty("type", new mdr.DProperty()
                                                {
                                                    OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                                                    {
                                                        var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                                        v.Set(ev.Data.Type.ToString());
                                                    },
                                                }, mdr.PropertyDescriptor.Attributes.NotWritable);
                         * */
                        prototype.SetField("initUIEvent", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
                        {
                            string eventTypeString = callFrame.Arg0.AsString();
                            bool bubbles = mjr.Operations.Convert.ToBoolean.Run(ref callFrame.Arg1);
                            bool cancelable = mjr.Operations.Convert.ToBoolean.Run(ref callFrame.Arg2);
                            // TODO: Variables 'view' and 'detail' are never used; remove?
                            /*string view = callFrame.Arg3.ToString();
                            int detail = -1;
                            if (callFrame.ArgsCount > 4)
                            {
                                detail = callFrame.Arguments[0].ToInt32();
                            }*/

                            var ev = callFrame.This.FirstInPrototypeChainAs<JSEvent>();
                            EventTypes type;
                            eventTypesToString.TryGetValue(eventTypeString, out type);
                            ev.InitEvent(type, bubbles, cancelable);

                        }));
                        //Fill the rest ...
                        break;
                    }
                case EventClasses.MouseEvent:
                    {
                        prototype = new mdr.DObject(HTMLRuntime.Instance.GetPropertyMapOfEventPrototype(EventClasses.UIEvent));
                        prototype.DefineOwnProperty("type", new mdr.DProperty()
                        {
                            OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                            {
                                var ev = This.FirstInPrototypeChainAs<JSEvent>();
                                v.Set(ev.Data.Type.ToString());
                            },
                        }, mdr.PropertyDescriptor.Attributes.NotWritable);
                        //Fill the rest ...
                        break;
                    }
                default:
                    Trace.Fail("Invalid event class type {0}", eventClass);
                    break;
            }
            return prototype;
        }

        public void DefaultAction()
        {
            if (Data.Type == EventTypes.Click)
            {
                WrappedObject tempTarget = Target;
                HTMLAnchorElement anchorElement = tempTarget as HTMLAnchorElement;
                tempTarget = tempTarget.Parent;
                while (anchorElement == null && tempTarget != null)
                {
                    anchorElement = tempTarget as HTMLAnchorElement;
                    tempTarget = tempTarget.Parent;
                }
                if (anchorElement != null)
                {
                    string newHref = anchorElement.Href;
                    if (newHref != null)
                    {
                        ContentWindow window = mdr.Runtime.Instance.GlobalContext as ContentWindow;
                        Debug.Assert(window != null, "In handling the default action for Click Content Window cannot be retrived!");
                        if (window != null)
                        {
                            Location loc = window.Location;
                            Debug.Assert(loc != null, "In handling the default action for Click Content Window location cannot be retrived!");
                            if (loc != null)
                            {
                                loc.Href = newHref;
                                Debug.WriteLine("Setting href of window location to {0}", newHref);
                            }
                        }
                    }
                }

            }

            
        }

        public bool Dispatch()
        {
            //Initializing event progress flags
            PropagationStopped = false;
            ImmediatePropagationStopped = false;
            DefaultPrevented = false;
            var eventTarget = CurrentTarget as DOM.EventTarget;
             if (eventTarget != null)
            {
                Debug.WriteLine("TARGET = " + eventTarget.ToString());
            }
            else
            {
                Debug.WriteLine("TARGET IS NULL");
            }

            Phase = JSEvent.Phases.AtTarget;
            bool eventBubblingCancelled = false;
            if (eventTarget != null)
            {
                var eventListeners = eventTarget.GetEventListeners(Data.Type, false);
                if (eventListeners != null)
                    eventBubblingCancelled = eventListeners.HandleEvent(this);
                if (eventListeners == null)
                {
                    Debug.WriteLine("Listener list is null!");
                }

                CurrentTarget = eventTarget.Parent as DOM.EventTarget;
            }
            //Implementing bubbling accroding to HTML Spec (Document Object Model Events section 1.2.3). 
            if (this.Bubbles && eventTarget != null)
            {
                Debug.WriteLine("EVENT BUBBLING");
                //Bubbling loop (outer loop)
                Phase = JSEvent.Phases.Bubbling;

                var bubllingTargetList = new List<DOM.EventTarget>();
                var tempTarget = eventTarget.Parent as DOM.EventTarget;
                while (tempTarget != null)
                {
                    bubllingTargetList.Add(tempTarget);
                    tempTarget = tempTarget.Parent as DOM.EventTarget;
                }
                for (int i = 0; (i < bubllingTargetList.Count) && (!eventBubblingCancelled); i++)
                {
                    CurrentTarget = bubllingTargetList[i];
                    tempTarget = CurrentTarget as DOM.EventTarget;
                    if (tempTarget != null)
                    {
                        Debug.WriteLine("curr TARGET = " + CurrentTarget.ToString());
                    }
                    var eventListeners = tempTarget.GetEventListeners(Data.Type, false);
                    if (eventListeners != null)
                        eventBubblingCancelled = eventListeners.HandleEvent(this);

                    CurrentTarget = CurrentTarget.Parent;
                }

            }

            bool cancelled = (DefaultPrevented && Cancelable);
            if (hasDefaultAction)
            {
                if (!cancelled)
                {
                    Debug.WriteLine("Performing default action!");
                    DefaultAction();
                }
                else
                {
                    Debug.WriteLine("Ignoring the default action as event Cancelable is {0} and cancelled flag is {1}", Cancelable, cancelled);
                }
            
            }

            //According to SPEC the return value is false if the default action is cancelled
            return cancelled;
        }
    }
}
