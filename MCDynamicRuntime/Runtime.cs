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

using m.Util.Diagnose;
using System.Runtime.CompilerServices;

namespace mdr
{
  /// <summary>
  /// It is important to remember that static and const values will be used accross all instances of the Runtime
  /// If that is not the case, we should use instance members
  /// </summary>

  public abstract class Runtime
  {
    [ThreadStatic]
    //public static Runtime Instance;
    public static Runtime _Instance;
    public static Runtime Instance { get { return _Instance; } set { _Instance = value; } }

    //private readonly RuntimeConfiguration Configuration;
    public RuntimeConfiguration Configuration { get; private set; } //Potentially we want to allow different configurations for different instances of Runtime

    #region Constants
    public const int InvalidIndex = -1;
    public const int InvalidFieldId = -1;
    public const int InvalidFieldIndex = -1;
    public const int InvalidOffset = -1;
    #endregion

    #region DTypeManager

    /// <summary>
    /// The root node for and empty DType with null prototype
    /// </summary>
    public readonly PropertyMapMetadata EmptyPropertyMapMetadata;
    public PropertyMapMetadata GetMapMetadataOfPrototype(DObject prototype)
    {
      if (prototype == null)
        return EmptyPropertyMapMetadata;
      return prototype.Map.Metadata.GetMapMetadataOfPrototype(prototype);
    }
    public PropertyMap GetRootMapOfPrototype(DObject prototype)
    {
      return GetMapMetadataOfPrototype(prototype).Root;
    }
    #endregion

    #region Prototypes
    /// <summary>
    /// We use the DUndefinedPrototype only internally to represent undefined stuff!
    /// </summary>
    //public readonly DObject DUndefinedPrototype;

    public readonly DObject DObjectPrototype;
    public readonly DObject DFunctionPrototype;
    public readonly DObject DNumberPrototype;
    public readonly DObject DStringPrototype;
    public readonly DObject DBooleanPrototype;
    public readonly DObject DArrayPrototype;
    public readonly DObject DRegExpPrototype;
    #endregion

    #region Maps
    public readonly PropertyMap DObjectMap;
    public readonly PropertyMap DFunctionMap;
    public readonly PropertyMap DNumberMap;
    public readonly PropertyMap DStringMap;
    public readonly PropertyMap DBooleanMap;
    public readonly PropertyMap DArrayMap;
    public readonly PropertyMap DRegExpMap;
    #endregion

    #region Defaults
    public readonly DUndefined DefaultDUndefined;
    public readonly DNull DefaultDNull;

    //public readonly DObject DefaultDObject;
    //public readonly DDouble DefaultDDouble;
    //public readonly DString DefaultDString;
    //public readonly DInt DefaultDInt;
    //public readonly DBoolean DefaultDBoolean;
    //public readonly DFunction DefaultDFunction;
    //public readonly DArray DefaultDArray;
    //public readonly DProperty DefaultDProperty;
    #endregion


    #region FieldIds
    public readonly int ValueOfFieldId;
    public readonly int ToStringFieldId;
    public readonly int PrototypeFieldId;
    public readonly int LengthFieldId;

    readonly List<string> _fieldId2NameMap = new List<string>();
    readonly Dictionary<string, int> _fieldName2IdMap = new Dictionary<string, int>();
    public List<string> FieldId2NameMap { get { return _fieldId2NameMap; } } //used for source code generation for debuggin. Otherwise, there is no reason to expose this

    public struct LastAccessPropertyCache
    {
      public PropertyMap Map;
      public PropertyDescriptor PropDesc;
    }
    //        static public List<LastAccessPropertyCache> _lastAccessedPropertyCache = new List<LastAccessPropertyCache>(10000);
    static private LastAccessPropertyCache[] _lastAccessedPropertyCache = new LastAccessPropertyCache[1000];

    static public int _inheritPropertyObjectCacheMaxIndex = 0;
    static public DObject[] _inheritPropertyObjectCache = new DObject[1000];

    static public int UpdateInheritPropertyObjectCache(PropertyDescriptor pd)
    {
      if (pd.ObjectCacheIndex != -1)
      {
        return pd.ObjectCacheIndex;
      }
      if (_inheritPropertyObjectCacheMaxIndex < _inheritPropertyObjectCache.Length)
      {
        _inheritPropertyObjectCache[_inheritPropertyObjectCacheMaxIndex] = pd.Container;
        pd.ObjectCacheIndex = _inheritPropertyObjectCacheMaxIndex;
        _inheritPropertyObjectCacheMaxIndex++;
//        Trace.WriteLine("inherit pobject index = {0}", _inheritPropertyObjectCacheMaxIndex);
        return _inheritPropertyObjectCacheMaxIndex - 1;
      }
      else
      {
        return -1;
      }
    }
//#if __MonoCS__
//    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
//#endif
    static public PropertyDescriptor LookupLastAcessedPropertyCacheForRead(int index, PropertyMap map)
    {
#if __STAT__PD 
      if (Runtime.Instance.Configuration.ProfileStats)
      {
        Runtime.Instance.Counters.GetCounter("Last PDCache read_accesses").Count++;
      }
#endif
      if (_lastAccessedPropertyCache[index].Map == map)
      {
#if __STAT__PD 
        if (Runtime.Instance.Configuration.ProfileStats)
        {
          Runtime.Instance.Counters.GetCounter("Last PDCache read_hits").Count++;
          if (_lastAccessedPropertyCache[index].PropDesc.IsInherited)
          {
            Runtime.Instance.Counters.GetCounter("Last PDCache inherited read_hits").Count++;
          }
        }
#endif
        return _lastAccessedPropertyCache[index].PropDesc;
      }
      return null;
    }
    static public PropertyDescriptor LookupLastAcessedPropertyCacheForWrite(int index, PropertyMap map, out PropertyMap newMap)
    {
#if __STAT__PD 
      if (Runtime.Instance.Configuration.ProfileStats)
      {
        Runtime.Instance.Counters.GetCounter("Last PDCache write_accesses").Count++;
      }
#endif
      newMap = null;
      var line = _lastAccessedPropertyCache[index]; //We are going to use this many times, so we can pay the overhead of copy

      if (line.Map == map)
      {
#if __STAT__PD 
        if (Runtime.Instance.Configuration.ProfileStats)
        {
          Runtime.Instance.Counters.GetCounter("Last PDCache write_hits").Count++;
          if (line.PropDesc.IsInherited)
          {
            Runtime.Instance.Counters.GetCounter("Last PDCache inherited write_hits").Count++;
          }
        } 
#endif
        return line.PropDesc;
      }
      else if (line.Map != null)
      {
        if (line.Map.Parent == map)
        {
#if __STAT__PD 
          if (Runtime.Instance.Configuration.ProfileStats)
          {
            Runtime.Instance.Counters.GetCounter("Last PDCache write_hits").Count++;
            if (line.PropDesc.IsInherited)
            {
              Runtime.Instance.Counters.GetCounter("Last PDCache inherited write_hits").Count++;
            }
          } 
#endif
          newMap = line.Map;
          return line.PropDesc;
        }
      }
      return null;
    }
    static public void UpdateLastAcessedPropertyCache(int index, PropertyMap map, PropertyDescriptor pd)
    {
      //We need to check the index and extent or map if needed. 
      //index &=0xFFFF            _lastAccessedPropertyCache[index].Map = map;
      _lastAccessedPropertyCache[index].Map = map;
      _lastAccessedPropertyCache[index].PropDesc = pd;
    }
    public int GetFieldId(string field)
    {
      if (field == null)
        return InvalidFieldId;

      if (Runtime.Instance.Configuration.ProfileStats)
      {              
        Runtime.Instance.Counters.GetCounter("GetFieldId").Count++;
      }
      int fieldId;
      lock (_fieldName2IdMap) //Does not matter which one we lock!
      {
        if (!_fieldName2IdMap.TryGetValue(field, out fieldId))
        {
          ///Technically we should lock here to prevent data corruption when parallelism is enabled
          ///But, we never execute in parallel, only paralle analyze may call this function
          ///To prevent runtime overhead, we do the locking the Analyzer algorithm, rather than here. 
          ///
          fieldId = _fieldName2IdMap.Count;
          _fieldName2IdMap[field] = fieldId;
          _fieldId2NameMap.Add(field);
          Debug.Assert(_fieldId2NameMap[fieldId] == field, "Invalid situation!");

          if (fieldId >= _lastAccessedPropertyCache.Length)
          {
            Array.Resize(ref _lastAccessedPropertyCache, 5 * _lastAccessedPropertyCache.Length);
          }

          Debug.Assert(fieldId == _fieldId2NameMap.Count - 1);
        }
      }
      return fieldId;
    }
    public string GetFieldName(int fieldId)
    {
      if (Runtime.Instance.Configuration.ProfileStats)
      {
        Runtime.Instance.Counters.GetCounter("GetFieldName").Count++;
      }
      if (fieldId != InvalidFieldId && fieldId < _fieldId2NameMap.Count)
        return _fieldId2NameMap[fieldId];
      return null;
    }
    #endregion

    //TODO:
    /// <summary>
    /// For now we use this hack! Assuming that we don't have arrays as prototype of others, the following will work
    /// later we will have a static list of PDs for accessing 'own' elements of arrays and use (pd,dvalue) pair for array elements
    /// </summary>

    internal PropertyDescriptor ArrayItemAccessor;



    internal PropertyDescriptor StringItemAccessor;

    public PropertyDescriptor GetArrayItemAccessor() {return ArrayItemAccessor;}
    public PropertyDescriptor GetStringItemAccessor() {return StringItemAccessor;}
    /// <summary>
    /// Holds the global variables of the program
    /// </summary>
    public DObject GlobalContext;

    public abstract m.Util.ISourceLocation Location { get; }

    #region Timers & Counters
    public readonly m.Util.Timers Timers;
    m.Util.Timers.SimpleTimer _timer;
    public readonly m.Util.Counters Counters;

    public static m.Util.Timers.Timer StartTimer(bool isEnabled, string name)
    {
      if (isEnabled && Instance.Timers != null)
      {
        var timer = Instance.Timers.GetTimer(name);
        timer.Start();
        return timer;
      }
      else
        return null;
    }
    public static void StartTimer(m.Util.Timers.Timer timer)
    {
      if (timer != null)
        timer.Start();
    }
    public static void StopTimer(m.Util.Timers.Timer timer)
    {
      if (timer != null)
        timer.Stop();
    }

    public static m.Util.Timers.SimpleTimer StartSimpleTimer(bool isEnabled, string name)
    {
      if (isEnabled && Instance.Timers != null)
      {
        var timer = Instance.Timers.GetSimpleTimer(name);
        timer.Start();
        return timer;
      }
      else
        return null;
    }
    public static void StartTimer(m.Util.Timers.SimpleTimer timer)
    {
      if (timer != null)
        timer.Start();
    }
    public static void StopTimer(m.Util.Timers.SimpleTimer timer)
    {
      if (timer != null)
        timer.Stop();
    }
    #endregion


    protected Runtime(RuntimeConfiguration configuration)
    {
      _inheritPropertyObjectCacheMaxIndex = 0;

      //We need to first set the Instance since the following constructors may use it!
      Runtime.Instance = this;

      Configuration = configuration;
      configuration.ParseArgs();//Do this now before anyone tries to read any configuration value.

      if (configuration.EnableTimers)
      {
        Timers = new m.Util.Timers();
        _timer = StartSimpleTimer(true, "MCJS");
      }

      if (configuration.EnableCounters)
        Counters = new m.Util.Counters();

      EmptyPropertyMapMetadata = new PropertyMapMetadata(null);

      ///We can initialize commong field Ids to save lookups later
      ValueOfFieldId = GetFieldId("valueOf");
      ToStringFieldId = GetFieldId("toString");
      PrototypeFieldId = GetFieldId("prototype");
      LengthFieldId = GetFieldId("length");

      ///In each instance of Runtime we need to first reset prototypes in case a program has changed them

      //DUndefinedPrototype = new DObject(root.Root);

      DObjectPrototype = new DObject(EmptyPropertyMapMetadata.Root);
      DObjectMap = GetRootMapOfPrototype(DObjectPrototype);

      DFunctionPrototype = new DObject(DObjectMap);
      DFunctionMap = GetRootMapOfPrototype(DFunctionPrototype);

      DNumberPrototype = new DObject(DObjectMap);
      DNumberMap = GetRootMapOfPrototype(DNumberPrototype);

      DStringPrototype = new DObject(DObjectMap);
      DStringMap = GetRootMapOfPrototype(DStringPrototype);

      DBooleanPrototype = new DObject(DObjectMap);
      DBooleanMap = GetRootMapOfPrototype(DBooleanPrototype);

      DArrayPrototype = new DObject(DObjectMap);
      DArrayMap = GetRootMapOfPrototype(DArrayPrototype);

      DRegExpPrototype = new DObject(DObjectMap);
      DRegExpMap = GetRootMapOfPrototype(DRegExpPrototype);


      //Now need to recreate default values based on fresh prototypes.
      DefaultDUndefined = new DUndefined();
      DefaultDNull = new DNull();

      //DefaultDObject = new DObject();
      //DefaultDDouble = new DDouble(default(double));
      //DefaultDString = new DString(default(string));
      //DefaultDInt = new DInt(default(int));
      //DefaultDBoolean = new DBoolean(default(bool));
      //DefaultDFunction = new DFunction(null);
      //DefaultDArray = new DArray();
      //DefaultDProperty = new DProperty();

      ArrayItemAccessor = new PropertyDescriptor(null)
      {
        Getter = (mdr.PropertyDescriptor pd, mdr.DObject obj, ref mdr.DValue value) =>
        {
          value = (obj as DArray).Elements[pd.Index];
          /*if (mdr.Runtime.Instance.Configuration.ProfileStats)
          {
            mdr.Runtime.Instance.Counters.GetCounter("ArrayItemAccessor").Count++;
          }*/
        },
        Setter = (mdr.PropertyDescriptor pd, mdr.DObject obj, ref mdr.DValue value) =>
        {
          (obj as DArray).Elements[pd.Index] = value;
        },
      };

      StringItemAccessor = new PropertyDescriptor(null)
      {
        Getter = (mdr.PropertyDescriptor pd, mdr.DObject obj, ref mdr.DValue value) =>
        {
          var strObj = obj.FirstInPrototypeChainAs<DString>();
          value.Set(strObj.PrimitiveValue.AsString()[pd.Index]);
        },
        Setter = (mdr.PropertyDescriptor pd, mdr.DObject obj, ref mdr.DValue value) =>
        {
          var strObj = obj.FirstInPrototypeChainAs<DString>();
          var chars = strObj.PrimitiveValue.AsString().ToCharArray();
          chars[pd.Index] = value.AsChar();
          strObj.PrimitiveValue.Set(new String(chars));
        },
      };

      var lengthFieldName = GetFieldName(LengthFieldId);
      ProtoInitializer.InitDArrayPrototype(DArrayPrototype, lengthFieldName);
      ProtoInitializer.InitDStringPrototype(DStringPrototype, lengthFieldName);

      var protoFieldName = GetFieldName(PrototypeFieldId);
      ProtoInitializer.InitDFunctionPrototype(DFunctionPrototype,
                                              protoFieldName,
                                              PrototypeFieldId);

    }

    public virtual void SetGlobalContext(mdr.DObject globalContext)
    {
      Debug.Assert(globalContext != null, "Global Context cannot be null");
      GlobalContext = globalContext;
      //if (globalContext == null)
      //    GlobalContext = new mdr.DObject();
      //GlobalContext = new mdr.DObject(GlobalDObject);
    }

    public virtual void ShutDown()
    {
      Debug.WriteLine("Shutting down the runtime");
      if (_timer != null)
      {
        StopTimer(_timer);
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine();
        Console.WriteLine("{0} ===> {1} ms", _timer.Name, _timer.ElapsedMilliseconds);
        Console.ResetColor();
        Console.WriteLine();
      }
      System.IO.TextWriter profileOut = Console.Out;
      if ((Timers != null || Counters != null) && Configuration.ProfilerOutput != null)
      {
        var profileFilename = Configuration.ProfilerOutput;
        profileOut = 
          System.IO.File.Exists(profileFilename) 
          ? System.IO.File.AppendText(profileFilename) 
          : System.IO.File.CreateText(profileFilename);
      }

      if (Timers != null)
      {
        Timers.ToXml(profileOut);
        //GenerateSummary(profileOut);
      }
      if (Counters != null)
      {
        Counters.PrintCounters(profileOut);
      }
      
      if (profileOut != Console.Out)
      {
        profileOut.Flush();
        profileOut.Close();
      }

      Instance = null;
    }



    /// <summary>
    /// Mono's implementation strategy for lambdas causes them
    /// to hold onto the parent class, if they contain free variables.
    /// Hence, we factor them out into a separate class.
    /// </summary>
    internal static class ProtoInitializer
    {
      internal static void InitDArrayPrototype(mdr.DObject dArrayProto,
                                               string protoFieldName)
      {
        dArrayProto.DefineOwnProperty(
            protoFieldName
            , new DProperty()
            {
              TargetValueType = ValueTypes.Int32,
              OnGetInt = (This) => { return (This as DArray).Length; },
              OnSetInt = (This, v) => { (This as DArray).Length = v; },
              OnGetDValue = (DObject This, ref DValue v) =>
              {
                var array = This.FirstInPrototypeChainAs<DArray>(false);
                if (array != null)
                  v.Set(array.Length);
                else
                  v.Set(0);
              },
              OnSetDValue = (DObject This, ref DValue v) =>
              {
                var array = This.FirstInPrototypeChainAs<DArray>(false);
                if (array != null)
                  array.Length = v.AsInt32();
              },
            }
            , PropertyDescriptor.Attributes.Accessor
            | PropertyDescriptor.Attributes.NotEnumerable
            | PropertyDescriptor.Attributes.NotConfigurable
        );
      }

      internal static void InitDStringPrototype(mdr.DObject dStringProto,
                                                string protoFieldName)
      {
        dStringProto.DefineOwnProperty(
            protoFieldName
            , new DProperty()
            {
              TargetValueType = ValueTypes.Int32,
              OnGetInt = (This) =>
              {
                var str = This as DString;
                if (str.PrimitiveValue.AsString() != null)
                  return str.PrimitiveValue.AsString().Length;
                else
                  return 0;
              },
              OnGetDValue = (DObject This, ref DValue v) =>
              {
                var str = This as DString;
                if (str != null && str.PrimitiveValue.AsString() != null)
                  v.Set(str.PrimitiveValue.AsString().Length);
                else
                  v.Set(0);
              },
              OnSetDValue = (DObject This, ref DValue v) => { },
            }
            , PropertyDescriptor.Attributes.Accessor
            | PropertyDescriptor.Attributes.NotEnumerable
            | PropertyDescriptor.Attributes.NotConfigurable
            | PropertyDescriptor.Attributes.NotWritable
        );
      }

      internal static void InitDFunctionPrototype(mdr.DObject dFunctionProto,
                                                  string protoFieldName,
                                                  int prototypeFieldId)
      {
        dFunctionProto.DefineOwnProperty(
            protoFieldName
            , new DProperty()
            {
              TargetValueType = ValueTypes.Object,
              OnGetDValue = (DObject This, ref DValue v) =>
              {
                //This is the first time This["prototype"] is accessed
                //So, we create the object, and add it to the object with a new PropertyDescriptor so that the current code is not executed again later
                var func = This.ToDFunction();
                var prototype = func.Map.AddOwnProperty(func, protoFieldName, prototypeFieldId, PropertyDescriptor.Attributes.Data | PropertyDescriptor.Attributes.NotEnumerable);
                func.PrototypePropertyDescriptor = prototype;
                func.Fields[prototype.Index].Set(new DObject());
                v.Set(ref func.Fields[prototype.Index]);
              },
              OnSetDValue = (DObject This, ref DValue v) =>
              {
                //This is the first time This["prototype"] is accessed
                //So, we add a new PropertyDescriptor so that the current code is not executed again later
                var func = This.ToDFunction();
                var prototype = func.Map.AddOwnProperty(func, protoFieldName, prototypeFieldId, PropertyDescriptor.Attributes.Data | PropertyDescriptor.Attributes.NotEnumerable);
                func.PrototypePropertyDescriptor = prototype;
                func.Fields[prototype.Index].Set(ref v);
              },
            }
            , PropertyDescriptor.Attributes.Accessor
            | PropertyDescriptor.Attributes.NotEnumerable
        );
      }
    }

  }
}
