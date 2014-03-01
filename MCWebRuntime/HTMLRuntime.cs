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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using mjr;
using m.Util.Diagnose;

namespace mwr
{
   public class HTMLRuntime : mjr.JSRuntime
    {
        //[ThreadStatic]
        public new static HTMLRuntime Instance { get { return (HTMLRuntime)mdr.Runtime.Instance; } }
        public new HTMLRuntimeConfiguration Configuration { get { return (HTMLRuntimeConfiguration)base.Configuration; } }

        /// <summary>
        /// This is equivalent to top.window in HTML, we need a reference to prevent collection.
        /// </summary>
        #pragma warning disable 414             // Disable the 'assigned but never used' warning.
        private mdr.DObject TopGlobalContext;
        #pragma warning restore 414

        #region PrototypeMapCache
        /// <summary>
        /// All the prototypes for this runtime
        /// We should actually use the following instead to get much better performance
        /// public mdr.ProperyMap[] DomPropertyMapOfPrototypes;
        /// </summary>
        public mdr.DObject[] domPrototypes;
        private bool _pageLoaded = false;


        private mdr.PropertyMap[] _maps = new mdr.PropertyMap[/*(int)WrapperType.LastWrapperType +*/(int)EventClasses.TheLastIndex];
        /*
        // This isn't compiling since we've moved away from using WrapperType.
        // I'd update it, but apparently nobody is actually calling it. - SF
        internal mdr.PropertyMap GetPropertyMapOfDomPrototype(WrapperType domClass)
        {
            var mapIndex = (int)domClass;
            var map = _maps[mapIndex];
            if (map == null)
            {
                var prototype = DOMBinder.GetPrototype(mapIndex);
                map = GetRootMapOfPrototype(prototype);
                _maps[mapIndex] = map;
            }
            return map;
        }
        */
        public mdr.PropertyMap GetPropertyMapOfEventPrototype(EventClasses eventClass)
        {
            var mapIndex = /*(int) WrapperType.LastWrapperType + */(int)eventClass;
            var map = _maps[mapIndex];
            if (map == null)
            {
                var prototype = JSEvent.GetPrototype(eventClass);
                map = GetRootMapOfPrototype(prototype);
                _maps[mapIndex] = map;
            }
            return map;
        }

        private Dictionary<Type, mdr.PropertyMap> _propertyMaps = new Dictionary<Type, mdr.PropertyMap>();
        public mdr.PropertyMap GetPropertyMapOfWrappedObjectType(Type wrappedObjectType)
        {
            mdr.PropertyMap pmap = null;
            if (!_propertyMaps.TryGetValue(wrappedObjectType, out pmap))
            {
                if (wrappedObjectType == typeof(DOM.WrappedObject))
                    return mdr.Runtime.Instance.DObjectMap;

                var baseMap = GetPropertyMapOfWrappedObjectType(wrappedObjectType.BaseType);
                var prototype = new mdr.DObject(baseMap);

                Debug.WriteLine("Creating PropertyMap for {0}", wrappedObjectType.FullName);

                var preparePrototypeMethod = wrappedObjectType.GetMethod("PreparePrototype");
                Debug.Assert(preparePrototypeMethod != null, "Cannot find method {0}.PreparePrototype()", wrappedObjectType.FullName);

                preparePrototypeMethod.Invoke(null, new object[] { prototype });
                pmap = GetRootMapOfPrototype(prototype);
                _propertyMaps[wrappedObjectType] = pmap;
            }
            return pmap;
        }
        #endregion

        /// <summary>
        /// We need this to comunicate with C++ side
        /// </summary>
        private DOM.Page _page;

        public TimerQueue TimerQueue;

        public HTMLRuntime(IntPtr pagePtr)
          : this(HTMLEngine.Configuration)
        {
            _page = new DOM.Page(pagePtr);
        }

        public HTMLRuntime(HTMLRuntimeConfiguration configuration)
            : base(configuration)
        {
            Debug.WriteLine("Creating a new HTMLRuntime Instance in Appdomain {0}", AppDomain.CurrentDomain.FriendlyName);
            //mdr.Diagnose.WriteLineIf(Instance != null, "Instance already has a value");
            //Instance = this;
            //domPrototypes = new mdr.DObject[(int) WrapperType.LastWrapperType];
            TimerQueue = new TimerQueue();
            Debug.WriteLine("method call resolution is {0}", Configuration.EnableMethodCallResolution ? "enabled" : "disabled");
            Debug.WriteLine("light code gen is {0}", Configuration.EnableLightCompiler ? "enabled" : "disabled");

#if ENABLE_RR
            if (configuration.EnableRecord)
            {
              //                RecordReplayManager.Instance = new RecordReplayManager();
              RecordReplayManager.Instance.RecordFilename = configuration.RecordFilename;
              RecordReplayManager.Instance.Parameters = configuration.RecordParams;
              Debug.WriteLine("Start recording the session: filename {0} params {1}", RecordReplayManager.Instance.RecordFilename, RecordReplayManager.Instance.Parameters);
              RecordReplayManager.Instance.StartRecord();
              RecordReplayManager.Instance.Record("HTMLRuntime", null, "ctor", false, configuration.Arguments);
            }
            else
              if (configuration.EnableReplay)
              {
                _page = new DOM.Page(new IntPtr(0));
              }

#endif

        }

        public void SetTopGlobalContext(mdr.DObject topWindow)
        {
#if ENABLE_RR
            if (RecordReplayManager.Instance.RecordEnabled)
            {
                RecordReplayManager.Instance.Record("HTMLRuntime", null, "SetTopGlobalContext", false, topWindow);
            }
#endif
            Debug.WriteLine("Setting global context");
            TopGlobalContext = topWindow;
            GlobalContext = topWindow;      // Just to be sure GlobalContext also has current value
            // RunScriptString("if(undefined);","xyzyyz"); // This might JIT most of the compiler code early
        }

        /// <summary>
        /// This is an asynchronous method that quickly finishes. It launches task to peform parallel analyze, jit, ... (depending on the switches)
        /// this method can be called from any thread
        /// </summary>
        /// <param name="scriptString"></param>
        /// <param name="scriptKey"></param>
        public void PrepareScriptString(string scriptString, string scriptKey)
        {
#if ENABLE_RR
            if (RecordReplayManager.Instance.RecordEnabled)
            {
                RecordReplayManager.Instance.Record("HTMLRuntime", null, "PrepareScriptString", false, scriptString, scriptKey);
            }
#endif
            base.Scripts.Add(scriptString, scriptKey, this);
        }

        /// <summary>
        /// Takes a script from the browser and runs it 
        /// </summary>
        public int RunHTMLScriptString(mdr.DObject currWindow, string scriptString, string scriptKey)
        {
#if ENABLE_RR
            if (RecordReplayManager.Instance.RecordEnabled)
            {
                RecordReplayManager.Instance.Record("HTMLRuntime", null, "RunHTMLScriptString", false, currWindow, scriptString, scriptKey);
            }
#endif
            int returnValue = -1;

            if (Instance != this)
                throw new Exception("JS Runtime instance has changed! JS Runtime instance must remain the same for a page as long as the pages is alive.");

            Debug.Assert(TopGlobalContext != null, "Top Global Context must be initialized before calling RunScript");

            try
            {
                if (GlobalContext != currWindow)
                    SetGlobalContext(currWindow);

                Debug.WriteLine("=====************ RUNNING JS CODE *************=====\n");
                if (string.IsNullOrEmpty(scriptString))
                {
                    Debug.WriteLine("Empty JS code. Returning ...");
                    return 0;
                }
                returnValue = RunScriptString(scriptString, scriptKey);
#if DEBUG
                // TODO: Write code to print the wrapper tree
                //(currWindow as WrappedObject).PrintTheList();
#endif
            }
            catch (Exception e)
            {
                Diagnostics.WriteException(e);
            }

            return returnValue;
        }

        public bool ProcessEvents(IntPtr filterPtr)
        {
            //Debug.Assert(filterPtr != IntPtr.Zero, "filterPtr is not valid");
#if ENABLE_RR
            if (RecordReplayManager.Instance.RecordEnabled)
            {
                RecordReplayManager.Instance.Record("HTMLRuntime", null, "ProcessEvents", false, filterPtr.ToInt64());
            }
#endif
            var filter = new DOM.EventFilter(filterPtr);
            while (true)
            {
                try
                {
                  Debug.WriteLine("**** MEM USAGE = {0} bytes, GC collection counts: G0={1}, G1={2}, G2={3}", System.GC.GetTotalMemory(false), System.GC.CollectionCount(0), System.GC.CollectionCount(1), System.GC.CollectionCount(2));
                //Process timer queue here and find the nextActiveTimer
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    var nextActiveTimer = TimerQueue.NextActivationTime();
                    var span = nextActiveTimer - epoch;
                    var curr = nextActiveTimer - DateTime.UtcNow;
                    Debug.WriteLine("Requested next wakeup time " + curr.Ticks + " ticks which is " + curr.Ticks / (10000) + " miliSec ");

                    var nextWakeuptime = (UInt64) span.Ticks; //C++ will convert ticks (1 / 100 ns) to (sec, ns)
                    var ev = new JSEvent();
                    Debug.WriteLine("Asking for next event");
#if ENABLE_RR
                    if (RecordReplayManager.Instance.ReplayEnabled)
                    {
                        RecordReplayManager.Instance.OutputBindingReplay("Page", "GetNextEvent", ev.Data, nextWakeuptime);
                        ev.Data = RecordReplayManager.Instance.EventData;
                    }
                    else
                        if (RecordReplayManager.Instance.RecordEnabled)
                        {
                            _page.GetNextEvent(filter, ref ev.Data, nextWakeuptime);
                            RecordReplayManager.Instance.Record("Page", null, "GetNextEvent", true, ev.Data, nextWakeuptime);
                        }
                        else
                        {
                            _page.GetNextEvent(filter, ref ev.Data, nextWakeuptime);
                        }
#else
                    _page.GetNextEvent(filter, ref ev.Data, nextWakeuptime);
#endif


                    Debug.WriteLine("Event type={0} event screenX/Y ({1} {2}) clientX/Y ({3} {4})", ev.Data.Type,
                        ev.Data.screenX, ev.Data.screenY, ev.Data.clientX, ev.Data.clientY);
                    switch (ev.Data.Type)
                    {//we handle special cases right here!
                        case EventTypes.ZoommTimeout:
                        case EventTypes.ZoommUnpaused:
                          {
                            var timer = StartTimer(Configuration.ProfileTimerTime, "JS/Event/Timer");
                            
                            TimerQueue.ProcessEvents();
                            
                            StopTimer(timer);
                            break;
                          }
                        case EventTypes.ZoommStop:
                            Debug.WriteLine("Exiting ProcessEvents loop!");
                            return true;
                        case EventTypes.ZoommYield:
                            Debug.WriteLine("ProcessEvents yielding back to caller");
                            return false;
                        default:
                          {
                            var timer = StartTimer(Configuration.ProfileTimerTime, "JS/Event/" + ev.Data.Type.ToString());

                            //Now we have a valid DOMEvent
                            Debug.WriteLine("Start processing the event!");
                            ev.CurrentTarget = ev.Data.Target;
                            ev.Target = ev.Data.Target;
                            ev.InitializeEventFlags();
                            if (ev.Data.Type == EventTypes.Load && ev.CurrentTarget == GlobalContext)
                            {
                                if (_pageLoaded)
                                {
                                    Console.Out.WriteLine("Recieving multiple onload events!\n Aborting!");
                                    throw (new InvalidProgramException("Recieving multiple onload events!\n Aborting!"));
                                }
                                _pageLoaded = true;
                            }
                            Debug.Assert(ev.Data.Type < EventTypes.ZoommEmpty, "Invalid dome event type {0}", ev.Data.Type);
                            Debug.Assert(ev.Data.Target != null, "DOM event has no target!");
                            ev.UpdateMap(); //To make sure we have the right prototype chain and event property setup
                            ev.Dispatch();
                            Debug.WriteLine("Dispatch completed!");

                            StopTimer(timer);
                            break;
                          }
                    }
                    _page.CleanupAfterScript();
                }
#if ENABLE_RR
                catch (mwr.RecordReplayManager.RRException ex)
                {
                    throw (ex);
                }
#endif
                catch (mjr.JSException e)
                {
                  WriteJSException(e);
                }
                catch (Exception ex)
                {
                  Diagnostics.WriteException(ex, "when processing event");
                }
            }
        }
        public void StartLocationTracking()
        {
            if (_page != null)
            {
                Debug.WriteLine("Calling page startLocationTracking");
                _page.StartLocationTracking();
            }
        }
        public void StopLocationTracking()
        {
            if (_page != null)
            {
                Debug.WriteLine("Calling page stopLocationTracking");
                _page.StopLocationTracking();
            }

        }


        public override void ShutDown()
        {
            Debug.WriteLine("HTMLRuntime Shutdown method was called");
#if ENABLE_RR            
            if (RecordReplayManager.Instance.RecordEnabled)
            {
                RecordReplayManager.Instance.Record("HTMLRuntime", null, "ShutDown", false);
            }
            if (RecordReplayManager.Instance.RecordEnabled)
            {
                RecordReplayManager.Instance.StopRecord();
                Debug.WriteLine("Stop recording the session.");
            }
#endif
            TopGlobalContext = null;
            GlobalContext = null;
            base.ShutDown();
            //Instance = null;
        }

        ~HTMLRuntime()
        {
            Debug.WriteLine("HTMLRuntime was collected!");
#if ENABLE_RR

#endif
        }

        /// <summary>
        /// This is use test the overhead of C++ --> C# calls
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public int Dummy(int i)
        {
            return i + 2;
        }
    }
}
