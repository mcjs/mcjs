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
using System.Reflection;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;



namespace mwr
{
    /// <summary>
    /// Record and Replay manager engine
    /// It stores the recorded or replayed session data structures
    /// Record/Replay functions are called for each binfing event during record/replay
    /// </summary>

  public class RecordReplayManager
  {
#if ENABLE_RR
#warning Record & Replay is enabled
        static public RecordReplayManager Instance = new RecordReplayManager();
        BindingSession RecordSession;
        BindingSession ReplaySession;
        private string _recordFilename = "binding_recordreplay_session.xml";
        private string _replayFilename = "binding_recordreplay_session.xml";
        HTMLRuntime _runtime = null;
        mwr.EventData _eventData;
        string[] _replayParams;
        
        //The table used for mapping wrapper id/wapper object during replay
        static Dictionary<long, DOM.WrappedObject> _recordReplayWrapperPtrMap = new Dictionary<long, DOM.WrappedObject>();
        //Converting a wrapped id to a wrapped object in the replay mode (wrapped ids remain the same between record replay)
        static public DOM.WrappedObject ConvertPtrIDToWrapper(long id)
        {
            DOM.WrappedObject value = null;
            if (_recordReplayWrapperPtrMap.TryGetValue(id, out value))
            {
                return value;
            }
            return null;
        }
        public bool RuntimeShutDown
        { get; set; }
        public bool ReplayStateConsistent
        { get; set; }
        public mwr.EventData EventData
        {
            get
            {
                return _eventData;
            }
        }
        public string[] ReplayParams
        {
            get
            {
                return _replayParams;
            }
        }


        public string RecordFilename
        {
            get
            {
                return _recordFilename;
            }
            set
            {               
                _recordFilename = value;
            }
        }
        public string ReplayFilename
        {
            get
            {
                return _replayFilename;
            }
            set
            {
                _replayFilename = value;
            }
        }
        public string Parameters { get; set; }
        int CurrentReplayInstanceIndex = 0;
        bool ReplayMode = false;
        bool RecordMode = false;

        public bool ReplayEnabled
        {
            get { return ReplayMode; }
        }
        public bool RecordEnabled
        {
            get { return RecordMode; }
        }

        /// <summary>
        /// Start record by creating recordSession
        /// </summary>
        /// 
        public void StartRecord()
        {
            RecordMode = true;
            RecordSession = new BindingSession();
        }
        
        /// <summary>
        /// Stop the recording by flushing the recordSession into an XML file
        /// </summary>
  
        public void StopRecord()
        {
            StreamWriter RecordWriter = new StreamWriter(RecordFilename);
            XmlSerializer serializer = new XmlSerializer(RecordSession.GetType());
            serializer.Serialize(RecordWriter, RecordSession);
            RecordWriter.Flush();
            RecordWriter.Close();
            RecordMode = false;
        }

        /// <summary>
        /// Start replaying by reading the input replay XML file and create the replaySession object
        /// </summary>
        public void StartReplay()
        {
            RuntimeShutDown = false;
            ReplayStateConsistent = true;
            ReplayMode = true;
            FileStream ReplayReader = new FileStream(ReplayFilename, FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(BindingSession));
            ReplaySession = serializer.Deserialize(ReplayReader) as BindingSession;
            ReplayReader.Close();

            Console.WriteLine("Binding session loaded successfully with {0} binding instances", ReplaySession.Instances.Count);
            Console.WriteLine("\n\nBinding session stats:\n\n");
            RecordReplayStats stats = new RecordReplayStats();
            for (int i = 0; i < ReplaySession.Instances.Count; i++)
            {
                stats.StatInstance(ReplaySession.Instances[i]);
            }
            Console.WriteLine(stats.ToString());

        }

        public void StopReplay()
        {
            ReplayMode = false;
        }

        /// <summary>
        /// When recording all IntPtr, EventData and WeappedObjects are converted IntPtrProxym EventDataProxy and WrappedObjectProxy objects.
        /// Otherwise the data cannot be converted to XML
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private object FilterRecordObject(object obj)
        {
            if (obj != null)
            {
                if (obj.GetType() == typeof(IntPtr))
                {
                    return new IntPtrRecordProxy((IntPtr)obj);
                } 
                if (obj.GetType() == typeof(EventData))
                {
                    return new EventDataRecordProxy((EventData)obj);
                }
                mwr.DOM.WrappedObject wobj = obj as mwr.DOM.WrappedObject;
                if (wobj != null)
                {
                    return new WrappedObjectRecordProxy(wobj);
                }
            }
            return obj;
        }

        /// <summary>
        /// When replay all Proxy objects are converted back to IntPtr EventData and WrappedObject objects.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private object FilterReplayObject(object obj)
        {
            if (obj != null)
            {
                if (obj.GetType() == typeof(IntPtrRecordProxy))
                {
                    return null;
/*                    IntPtrRecordProxy intPtrProxy = obj as IntPtrRecordProxy;
                    if (intPtrProxy != null){
                        IntPtr ipt = new IntPtr(intPtrProxy.ID);
 
                    }   
                    else
                        return null;*/
                }
                if (obj.GetType() == typeof(WrappedObjectRecordProxy))
                {
                    WrappedObjectRecordProxy wobj = obj as WrappedObjectRecordProxy;
                    if (wobj == null)
                    {
                        return null;
                    }
                    return ConvertPtrIDToWrapper(wobj.DomID);
                }
                if (obj.GetType() == typeof(EventDataRecordProxy))
                {
                    EventDataRecordProxy edata = obj as EventDataRecordProxy;
                    _eventData.altKey = edata.altKey;
                    _eventData.button = edata.button;
                    _eventData.buttons = edata.buttons;
                    _eventData.clientX = edata.clientY;
                    _eventData.clientY = edata.clientY;
                    _eventData.ctrlKey = edata.ctrlKey;
                    _eventData.detail = edata.detail;
                    _eventData.EventClass = edata.EventClass;
                    _eventData.metaKey = edata.metaKey;
                    _eventData.pageX = edata.pageX;
                    _eventData.pageY = edata.pageY;
                    _eventData.screenX = edata.screenX;
                    _eventData.screenY = edata.screenY;
                    _eventData.Type = edata.Type;
                    _eventData.shiftKey = edata.shiftKey;
                    _eventData.TimeStamp = edata.TimeStamp;
                    _eventData.Target = (DOM.WrappedObject)FilterReplayObject(edata.Target);
                    _eventData.relatedTarget = (DOM.WrappedObject)FilterReplayObject(edata.relatedTarget);
                }
 
            }
            return obj;
        }
        /// <summary>
        /// Record the current binding (including its returned value, function name all input params)
        /// </summary>
        /// <param name="classInfo"></param>
        /// <param name="val"></param>
        /// <param name="funcName"></param>
        /// <param name="outgoing"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public object Record(string classInfo, object val, string funcName, bool outgoing, params object[] parameters)
        {
 
            val = FilterRecordObject(val);
            for (int i = 0; i < parameters.Length;i++)
            {
                object obj = parameters[i];
                if (obj != null)
                {
                    parameters[i] = FilterRecordObject(obj);
                }
            }
            Console.WriteLine("Recording instance {0}", RecordSession.Instances.Count);
            RecordSession.AddBindingInstance(new BindingInstance(classInfo, val, funcName, outgoing, parameters));
            return val;
        }

        private void FilterReplayReturnParams(BindingInstance instance)
        {
            instance.RetVal = FilterReplayObject(instance.RetVal);
            if (instance.Parameters != null)
            {
                for (int j = 0; j < instance.Parameters.Length; j++)
                {
                    object obj = instance.Parameters[j];
                    if (obj != null)
                    {
                        instance.Parameters[j] = FilterReplayObject(obj);
                    }
                }
            }
        
        }

        /// <summary>
        /// Specialized replay for Shutdown
        /// </summary>
        private void ReplayHTMLRuntimeShutdown()
        {
            _runtime.ShutDown();
            _runtime = null;
            RuntimeShutDown = true;
        }

        /// <summary>
        /// Specialized replay for Runtime ctor
        /// </summary>
        /// <param name="currentInstance"></param>
        private void ReplayHTMLRuntimeCtor(BindingInstance currentInstance)
        {
            _replayParams = new string[currentInstance.Parameters.Length];
            for (int i = 0; i < currentInstance.Parameters.Length; i++)
            {
                if (currentInstance.Parameters[i].Equals("--record"))
                {
                    _replayParams[i] = "--replay";
                }
                else if (currentInstance.Parameters[i].Equals("--record+"))
                {
                    _replayParams[i] = "--replay+";
                }
                else
                {
                    _replayParams[i] = (string)currentInstance.Parameters[i];
                }
            }
            HTMLRuntimeConfiguration htmlconfig = new HTMLRuntimeConfiguration(ReplayParams);
            //Loading the runtime
            _runtime = new mwr.HTMLRuntime(htmlconfig);
            if (_runtime != null)
            {
                _runtime.Configuration.EnableReplay = true;
            }
        }

        /// <summary>
        /// Specialized replay for WrappedObject ctor
        /// </summary>
        /// <param name="currentInstance"></param>
        /// <param name="currentReplayInstanceIndex"></param>
        private void ReplayWrappedObjectCtor(BindingInstance currentInstance, int currentReplayInstanceIndex)
        {
            Type type = System.Reflection.Assembly.GetExecutingAssembly().GetType((string)currentInstance.Parameters[1]);
            if (type != null)
            {
                //Using reflection to call the correct DOMWrapper object
                Type[] cArgTypes = { typeof(IntPtr) };
                ConstructorInfo cInfo = type.GetConstructor(cArgTypes);
                DOM.WrappedObject wobj = null;
                if (cInfo != null)
                {
                    IntPtr intPtr = new IntPtr(0);
                    object[] cArgs = { intPtr };
                    wobj = cInfo.Invoke(cArgs) as DOM.WrappedObject;
                    if (wobj != null)
                        _recordReplayWrapperPtrMap.Add((long)currentInstance.Parameters[0], wobj);
                }

            }
            else
            {
                string exceptionString = "Replaying binding instance " + currentReplayInstanceIndex + " error : type of recorded WrapperObject ctor is unknown";
                ReplayStateConsistent = false;

                throw new RRException(exceptionString);
            }
        }

        /// <summary>
        /// Specialized replay for SetParent
        /// </summary>
        /// <param name="currentInstance"></param>
        /// <param name="currentReplayInstanceIndex"></param>
        private void ReplayWrappedObjectSetParent(BindingInstance currentInstance, int currentReplayInstanceIndex)
        {
            if (currentInstance.Parameters.Length >= 1)
            {
                DOM.WrappedObject wobj = currentInstance.Parameters[0] as DOM.WrappedObject;
                DOM.WrappedObject parentWobj;
                if (currentInstance.Parameters.Length == 2)
                    parentWobj = currentInstance.Parameters[1] as DOM.WrappedObject;
                else
                    parentWobj = null;
                if (parentWobj != null)
                    wobj.SetParent(parentWobj);
            }
            else
            {
                string exceptionString = "Replaying binding instance " + currentReplayInstanceIndex + " error : SetParent requies at least two arguments!";
                ReplayStateConsistent = false;
                throw new RRException(exceptionString);
            }
        }
        /// <summary>
        /// The loop for handling incoming bindings
        /// </summary>
        public void InputBindingsReplayLoop()
        {
            Console.WriteLine("Starting the input binding replay loop");
            while (CurrentReplayInstanceIndex < ReplaySession.Instances.Count && !ReplaySession.Instances[CurrentReplayInstanceIndex].Outgoing)
            {
                BindingInstance currentInstance = ReplaySession.Instances[CurrentReplayInstanceIndex];
                string classInfo = currentInstance.ClassInfo;
                string funcName = currentInstance.FuncName;
                int currentReplayInstanceIndex = CurrentReplayInstanceIndex;
                Console.WriteLine("Replaying instance {0}", currentReplayInstanceIndex);

                FilterReplayReturnParams(currentInstance);
                CurrentReplayInstanceIndex++;

                if (classInfo == "HTMLRuntime" && funcName == "ctor")
                {
                    ReplayHTMLRuntimeCtor(currentInstance);
                }
                else
                    if (classInfo == "WrappedObject" && funcName == "ctor")
                    {
                        ReplayWrappedObjectCtor(currentInstance, currentReplayInstanceIndex);
                    }
                    else
                        if (classInfo == "WrappedObject" && funcName == "SetParent")
                        {
                            ReplayWrappedObjectSetParent(currentInstance, currentReplayInstanceIndex);
                        }
                        else
                            if (classInfo == "HTMLRuntime" && funcName == "SetTopGlobalContext")
                            {
                                mwr.HTMLRuntime.Instance.SetTopGlobalContext(currentInstance.Parameters[0] as mdr.DObject);
                            }
                            else
                                if (classInfo == "HTMLRuntime" && funcName == "ProcessEvents")
                                {
                                    HTMLRuntime.Instance.ProcessEvents(new IntPtr(0));
                                }
                                else
                                    if (classInfo == "Element" && funcName == "SetEventHandlerAttr")
                                    {
                                        DOM.Element.SetEventHandlerAttr((mdr.DObject)currentInstance.Parameters[0],
                                            (string)currentInstance.Parameters[1], (string)currentInstance.Parameters[2]);
                                    }
                                    else
                                        if (classInfo == "Element" && funcName == "GetEventHandlerAttr")
                                        {
                                            DOM.Element.GetEventHandlerAttr((mdr.DObject)currentInstance.Parameters[0],
                                                (EventTypes)currentInstance.Parameters[1], (string)currentInstance.Parameters[2]);
                                        }
                                        else if (classInfo == "HTMLRuntime" && funcName == "RunHTMLScriptString")
                                        {
                                            HTMLRuntime.Instance.RunHTMLScriptString((mdr.DObject)currentInstance.Parameters[0],
                                                (string)currentInstance.Parameters[1], (string)currentInstance.Parameters[2]);
                                        }
                                        else if (classInfo == "HTMLRuntime" && funcName == "ShutDown")
                                        {
                                            ReplayHTMLRuntimeShutdown();
                                        }
                                        else
                                        {
                                            string exceptionString = "Replaying binding instance " + currentReplayInstanceIndex + " error : unknown binding: " + classInfo + " : " + funcName;
                                            ReplayStateConsistent = false;
                                            throw new RRException(exceptionString);
                                        }
            }
        }

        /// <summary>
        /// Function for Replaying all outgoing bindings, gets classInfo, function name and params for checking the consisency if the caller state
        /// It returns the recorded return value of the binding if the state is consistence (the func/class name and param count match the recorded ones)
        /// </summary>
        /// <param name="classInfo"></param>
        /// <param name="funcName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public object OutputBindingReplay(string classInfo, string funcName, params object[] parameters)
        {
            //Going through all the incoming messages firsts
            InputBindingsReplayLoop();
            if (CurrentReplayInstanceIndex >= ReplaySession.Instances.Count)
            {
                Console.WriteLine("Warning: Replaying {0} in class {1} after replay file ended!", funcName, classInfo);
                CurrentReplayInstanceIndex++;
                return null;
            }
            BindingInstance currentInstance = ReplaySession.Instances[CurrentReplayInstanceIndex];

            Console.WriteLine("Replaying instance {0}", CurrentReplayInstanceIndex);
            FilterReplayReturnParams(currentInstance);
            if (!currentInstance.FuncName.Equals(funcName))
            {
                if (funcName == "gcDestroy" && classInfo == "WrappedObject")
                {
                    Console.WriteLine("Warning: By passing unmatching gcDestroy calls!");
                    return null;
                }
                else if (currentInstance.FuncName == "gcDestroy" && currentInstance.ClassInfo == "WrappedObject")
                {
                    while (currentInstance.FuncName == "gcDestroy" && currentInstance.ClassInfo == "WrappedObject")
                    {
                        Console.WriteLine("Warning: By passing unmatching gcDestroy replay calls!");
                        CurrentReplayInstanceIndex++;
                        InputBindingsReplayLoop();
                        currentInstance = ReplaySession.Instances[CurrentReplayInstanceIndex];
                        FilterReplayReturnParams(currentInstance);
                    }
                }
                else
                {
                    string exceptionString = "Replaying binding instance " + CurrentReplayInstanceIndex + " error : function name mismtach: CALLED " + funcName +
                        " while REPLAYED " + currentInstance.FuncName;
                    throw new RRException(exceptionString);
                }
            }
            if (!currentInstance.FuncName.Equals(funcName))
            {
                string exceptionString = "Replaying binding instance " + CurrentReplayInstanceIndex + " error : function name mismtach: CALLED " + funcName +
                    " while REPLAYED " + currentInstance.FuncName;
                throw new RRException(exceptionString);
            }
            if (parameters.Length != currentInstance.Parameters.Length)
            {
                string exceptionString = "Replaying binding instance " + CurrentReplayInstanceIndex + " error : function " + funcName + 
                    " param length mismtach: CALLED " + parameters.Length + " while REPLAYED " + currentInstance.Parameters.Length;
                throw new RRException(exceptionString);
            }
            if (classInfo == "HTMLDocument" && funcName == "write")
            {
                Console.WriteLine("---> Document.write {0}", parameters[1]);                
            }
            //Commented for now
            /*
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!currentEvent.Parameters[i].Equals(parameters[i]))
                {
                    string exceptionString = "Replaying binding event " + CurrentReplayEvent + " error : function " + funcName +
                        " param " + i + " value mismtach: CALLED " + parameters[i].ToString() + " while REPLAYED " + currentEvent.Parameters[i].ToString();
                    throw new Exception(exceptionString);
                }
            }
             */
            CurrentReplayInstanceIndex++;
            return currentInstance.RetVal;
        }

        public static int RunReplay(string ReplayFilename, string ReplayParams)
        {
          //Initializing RR Manager
          RecordReplayManager.Instance.ReplayFilename = ReplayFilename;
          RecordReplayManager.Instance.Parameters = ReplayParams;
          //Replaying the first binding, must be HTMLRuntime ctor
          RecordReplayManager.Instance.StartReplay();
          RecordReplayManager.Instance.InputBindingsReplayLoop();
          //            rrManager.Replay("HTMLRuntime", "ctor", false, null);


          //Running the replay session after loading the engine
          //            rrManager.Replay("HTMLRuntime", "SetTopGlobalContext", false);
          if (RecordReplayManager.Instance.RuntimeShutDown)
          {
            Console.WriteLine("Replaying finished with engine shutdown properly.");
          }
          else
          {
            Console.WriteLine("Replaying finished but the engine did not shutdown correctly!");
          }
          if (!RecordReplayManager.Instance.ReplayStateConsistent)
          {
            Console.WriteLine("Replay state inconsistency detected!");
          }
          return 0;
        }

        /// <summary>
        /// This class represents each binding calls in the recorded/replayed session
        /// The binding info includes the return value for the binding call, the corresponding function name and input parameters
        /// This info can be easily extended
        /// </summary>
        public class BindingInstance
        {
          [XmlElement("BID")]
          public long BindingID;
          [XmlElement("CLASS")]
          public string ClassInfo;
          [XmlElement("FuncName")]
          public string FuncName;
          [XmlElement("OutDirection")]
          public bool Outgoing;
          [XmlElement("RetVal")]
          public object RetVal;
          [XmlElement("Parameters")]
          public object[] Parameters;
          private static long _currentID = 0;
          public BindingInstance(string classInfo, object retVal, string funcName, bool outgoing, params object[] parameters)
          {
            RetVal = retVal;
            FuncName = funcName;
            Parameters = parameters;
            ClassInfo = classInfo;
            Outgoing = outgoing;
            BindingID = _currentID;
            _currentID++;
          }
          public BindingInstance()
          {
          }

        }

    /// <summary>
        /// Represents the entire recorded/replayed binding session
        /// Includes the list of the reorded/replayed events
        /// </summary>
        [XmlInclude(typeof(IntPtrRecordProxy))]
        [XmlInclude(typeof(WrappedObjectRecordProxy))]
        [XmlInclude(typeof(EventDataRecordProxy))]
        //    [XmlInclude(typeof(EventData))]
        [XmlRoot("BindingSession")]
        public class BindingSession
        {
          [XmlElement("Info")]
          public string Info;

          [XmlArray("Instances")]
          public List<BindingInstance> Instances;

          public BindingSession()
          {
            Instances = new List<BindingInstance>();
          }
          public void AddBindingInstance(BindingInstance sessionEvent)
          {
            Instances.Add(sessionEvent);
          }

        }

        /// <summary>
        /// Represents the objects recorded and replayed as a proxy for IntPtr
        /// </summary>
        public class IntPtrRecordProxy
        {
          public IntPtrRecordProxy(IntPtr ptr)
          {
            if (ptr != IntPtr.Zero)
            {
              ID = ptr.ToInt64();
            }
          }
          public IntPtrRecordProxy()
          {
          }
          [XmlElement("ID")]
          public long ID;

        }
        /// <summary>
        /// Represents the objects recorded and replayed as a proxy for WrappedObjects 
        /// </summary>
        public class WrappedObjectRecordProxy
        {
          public WrappedObjectRecordProxy(DOM.WrappedObject wobj)
          {
            if (wobj != null)
            {
              WrappedTypeString = wobj.GetType().ToString();
              DomID = wobj.Domptr.ToInt64();
            }
          }
          public WrappedObjectRecordProxy()
          {
          }
          [XmlElement("Type")]
          public string WrappedTypeString;
          [XmlElement("DOMID")]
          public long DomID;

        }

        /// <summary>
        /// Represents the objects recorded and replayed as a proxy for EventData 
        /// </summary>
        public class EventDataRecordProxy
        {
          public EventDataRecordProxy(EventData data)
          {
            EventClass = data.EventClass;
            Type = data.Type;
            bool realEvent = true;
            bool realEventRelTarget = false;
            if (Type == EventTypes.ZoommTimeout || Type == EventTypes.ZoommUnpaused || Type == EventTypes.ZoommStop)
              realEvent = false;
            if (Type == EventTypes.MouseOut || Type == EventTypes.MouseOver)
              realEventRelTarget = true;
            realEvent = true;
            realEventRelTarget = true;
            TimeStamp = data.TimeStamp;
            if (data.Target != null && realEvent)
              Target = new WrappedObjectRecordProxy(data.Target);
            else
              Target = null;
            detail = data.detail;
            screenX = data.screenX;
            screenY = data.screenY;
            pageX = data.pageX;
            pageY = data.pageY;
            ctrlKey = data.ctrlKey;
            shiftKey = data.shiftKey;
            altKey = data.altKey;
            metaKey = data.metaKey;
            button = data.button;
            buttons = data.buttons;
            if (data.relatedTarget != null && realEvent && realEventRelTarget)
              relatedTarget = new WrappedObjectRecordProxy(data.relatedTarget);
            else
              relatedTarget = null;
          }
          public EventDataRecordProxy()
          {

          }
          [XmlElement("EventClass")]
          public EventClasses EventClass;
          [XmlElement("EventType")]
          public EventTypes Type;
          [XmlElement("Target")]
          public WrappedObjectRecordProxy Target;
          [XmlElement("TimeStamp")]
          public UInt64 TimeStamp;

          [XmlElement("Detail")]
          public Int32 detail;

          [XmlElement("ScreenX")]
          public Int32 screenX;
          [XmlElement("ScreenY")]
          public Int32 screenY;
          [XmlElement("ClientX")]
          public Int32 clientX;
          [XmlElement("ClientY")]
          public Int32 clientY;
          [XmlElement("PageX")]
          public Int32 pageX;
          [XmlElement("PageY")]
          public Int32 pageY;
          [XmlElement("CtrlKey")]
          public bool ctrlKey;
          [XmlElement("ShiftKey")]
          public bool shiftKey;
          [XmlElement("AltKey")]
          public bool altKey;
          [XmlElement("MetaKey")]
          public bool metaKey;
          [XmlElement("Button")]
          public UInt32 button;
          [XmlElement("Buttons")]
          public UInt32 buttons;
          [XmlElement("RelatedTarget")]
          public WrappedObjectRecordProxy relatedTarget;
        }

        /// <summary>
        /// Record & Replay stats
        /// </summary>
        public class RecordReplayStats
        {
          private int _bindingInstances = 0;
          private int _outgoingInstances = 0;
          private int _incomingInstances = 0;
          private Dictionary<string, int> _bindingCallsStats = new Dictionary<string, int>();
          public void StatInstance(BindingInstance instance)
          {
            _bindingInstances++;
            if (instance.Outgoing)
              _outgoingInstances++;
            else
              _incomingInstances++;
            string key = instance.ClassInfo + ":" + instance.FuncName + " [" + ((instance.Outgoing) ? "out" : "in") + "]";
            int count;
            if (_bindingCallsStats.TryGetValue(key, out count))
            {
              count++;
              _bindingCallsStats[key] = count;
            }
            else
              _bindingCallsStats.Add(key, 1);

          }
          override public string ToString()
          {

            string output = "Total bindings " + _bindingInstances + "\n";
            output += "Total outging bindings " + _outgoingInstances + "\n";
            output += "Total incoming bindings " + _incomingInstances + "\n";
            int count;
            var items = from k in _bindingCallsStats.Keys
                        orderby _bindingCallsStats[k] descending
                        select k;


            foreach (string key in items)
            {
              if (_bindingCallsStats.TryGetValue(key, out count))
              {
                output += key + ":" + count + "\n";
              }

            }
            return output;
          }
        }

        /// <summary>
        /// Record replay exception, thrown when the replay state is different from the record state
        /// </summary>
        public class RRException : System.Exception
        {
          public RRException(string s)
            : base(s)
          {
          }
        }
#else
    public static int RunReplay(string ReplayFilename, string ReplayParams) { return 0; }
#endif
  }
}
