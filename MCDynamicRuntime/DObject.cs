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

using m.Util.Diagnose;
using System.Runtime.CompilerServices;

namespace mdr
{
  public class DObject
  {
    public virtual ValueTypes ValueType { get { return ValueTypes.Object; } }
    public virtual string GetTypeOf() { return "object"; }
    public virtual int GetInlineFieldCount() {return 0;}
    /// <summary>
    /// Provides information about the properies of this object, and their offset in the Fields array
    /// </summary>
    PropertyMap _map;

    public int MapId;

    public PropertyMap Map
    {
      get { return _map; }
      //internal //TODO: for now it is public, to enable func context efficient change. We should have a separte Func context.
      set
      {
        _map = value;
        MapId = _map.UniqueId;
        ResizeFields(_map.Property.Index);
      }
    }

    PropertyMapMetadata _subMapsMetadata;
    public PropertyMapMetadata SubMapsMetadata
    {
      get { return _subMapsMetadata; }
      set
      {
        Debug.Assert(_subMapsMetadata == null, "Cannot change the SubMapsMetadata after it is initlized");
        _subMapsMetadata = value;
      }
    }

    //public bool Is(DType type) { return type.IsParentOf(DType); }
    public DObject Prototype { get { return Map.Metadata.Prototype; } }
    public string Class { get { return Map.Metadata.Name; } }

#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public DObject(int initialFieldCapacity, PropertyMap map)
    {
      Fields = new DValue[initialFieldCapacity];
      Map = map;
    }
#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public DObject(PropertyMap map) : this(0, map) { }
#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public DObject(DObject prototype) : this(Runtime.Instance.GetRootMapOfPrototype(prototype)) { }
#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public DObject()
      : this(Runtime.Instance.DObjectMap)
    { }

    public T FirstInPrototypeChainAs<T>(bool thorwIfMissing = true)
        where T : DObject
    {
      var obj = this;
      do
      {
        var o = obj as T;
        if (o != null)
          return o;
        obj = obj.Prototype;
      } while (obj != null);

      if (thorwIfMissing)
        throw new InvalidCastException(string.Format("Could not find type {0} in prorotype chain of object {1}", typeof(T).Name, ToString()));

      return null;
    }

    #region Value
    public DValue PrimitiveValue; // { get { return Fields[0][0]; } set { Fields[0][0] = value; } }
    //int _valueOfIndex = DType.InvalidIndex;
    //public DFunction ValueOfProperty
    //{
    //    get
    //    {
    //        if (_valueOfIndex != DType.InvalidIndex)
    //        {
    //            int row, col;
    //            FieldIndexToRowCol(_valueOfIndex, out row, out col);
    //            if (Fields[row][col].ValueType == ValueTypes.Function)
    //                return Fields[row][col].DObjectValue as DFunction;
    //        }
    //        return null;
    //    }
    //}

    //int _toStringIndex = DType.InvalidIndex;
    //public DFunction ToStringProperty
    //{
    //    get
    //    {
    //        if (_toStringIndex != DType.InvalidIndex)
    //        {
    //            int row, col;
    //            FieldIndexToRowCol(_toStringIndex, out row, out col);
    //            if (Fields[row][col].ValueType == ValueTypes.Function)
    //                return Fields[row][col].DObjectValue as DFunction;
    //        }
    //        return null;
    //    }
    //}

    protected string ToStringProperty()
    {
      var toStringPD = GetPropertyDescriptorByFieldId(Runtime.Instance.ToStringFieldId);
      if (!toStringPD.IsUndefined)
      {
        var cf = new mdr.CallFrame();
        DValue toStringFunc = toStringPD.Get(this);
        cf.Function = toStringFunc.AsDFunction();
        cf.Signature = mdr.DFunctionSignature.EmptySignature;
        cf.This = (this);
        cf.Function.Call(ref cf);
        return cf.Return.AsString();
      }
      else
        return ToString();
    }

    T InvalidConvertion<T>(T defaultValue)
    {
      Trace.Fail(new NotSupportedException(string.Format("Cannot convert {0} to {1}", this.ToString(), typeof(T))));
      return defaultValue;
    }

    public override string ToString() { return string.Format("[object {0}]", Map.Metadata.Name); }
    public virtual char ToChar() { return InvalidConvertion(default(char)); }
    public virtual bool ToBoolean() { return true; }
    public virtual float ToFloat() { return PrimitiveValue.AsFloat(); }
    public virtual double ToDouble() { return PrimitiveValue.AsDouble(); }

    public virtual sbyte ToInt8() { return PrimitiveValue.AsInt8(); }
    public virtual short ToInt16() { return PrimitiveValue.AsInt16(); }
    public virtual int ToInt32() { return PrimitiveValue.AsInt32(); }
    public virtual long ToInt64() { return PrimitiveValue.AsInt64(); }
    public virtual byte ToUInt8() { return PrimitiveValue.AsUInt8(); }
    public virtual ushort ToUInt16() { return PrimitiveValue.AsUInt16(); }
    public virtual uint ToUInt32() { return PrimitiveValue.AsUInt32(); }
    public virtual ulong ToUInt64() { return PrimitiveValue.AsUInt64(); }

    //public virtual float ToFloat() { return InvalidConvertion(default(float)); }
    //public virtual double ToDouble() { return InvalidConvertion(default(double)); }

    //public virtual sbyte ToInt8() { return InvalidConvertion(default(sbyte)); }
    //public virtual short ToInt16() { return InvalidConvertion(default(short)); }
    //public virtual int ToInt32() { return InvalidConvertion(default(int)); }
    //public virtual long ToInt64() { return InvalidConvertion(default(long)); }
    //public virtual byte ToUInt8() { return InvalidConvertion(default(byte)); }
    //public virtual ushort ToUInt16() { return InvalidConvertion(default(ushort)); }
    //public virtual uint ToUInt32() { return InvalidConvertion(default(uint)); }
    //public virtual ulong ToUInt64() { return InvalidConvertion(default(ulong)); }

    //public virtual DObject ToDObject() { return InvalidConvertion<DObject>(null); }
    public virtual DFunction ToDFunction() { return InvalidConvertion<DFunction>(null); }
    public virtual DArray ToDArray() { return InvalidConvertion<DArray>(null); }

    public virtual DValue ToDValue() { return InvalidConvertion(new DValue()); }

    DObject InvalidAssignment<T>(T v)
    {
      Trace.Fail(new NotSupportedException(string.Format("Cannot assign {0}:{1} to {2}", v, typeof(T), this.ToString())));
      return null;
    }

    //public virtual DObject Set(string v) { return InvalidAssignment(v); }
    //public virtual DObject Set(char v) { return InvalidAssignment(v); }
    //public virtual DObject Set(bool v) { return InvalidAssignment(v); }
    //public virtual DObject Set(float v) { return InvalidAssignment(v); }
    //public virtual DObject Set(double v) { return InvalidAssignment(v); }

    //public virtual DObject Set(sbyte v) { return InvalidAssignment(v); }
    //public virtual DObject Set(short v) { return InvalidAssignment(v); }
    //public virtual DObject Set(int v) { return InvalidAssignment(v); }
    //public virtual DObject Set(long v) { return InvalidAssignment(v); }
    //public virtual DObject Set(byte v) { return InvalidAssignment(v); }
    //public virtual DObject Set(ushort v) { return InvalidAssignment(v); }
    //public virtual DObject Set(uint v) { return InvalidAssignment(v); }
    //public virtual DObject Set(ulong v) { return InvalidAssignment(v); }

    //public virtual DObject Set(DObject v) { return InvalidAssignment(v); }
    //public virtual DObject Set(ref DValue v)
    //{
    //    switch (v.ValueType)
    //    {
    //        case ValueTypes.String: return Set(v.ToString());
    //        case ValueTypes.Double: return Set(v.DoubleValue);
    //        case ValueTypes.Int32: return Set(v.IntValue);
    //        case ValueTypes.Boolean: return Set(v.BooleanValue);
    //        //case ValueTypes.Long: return Set(v.LongValue);
    //        default:
    //            return InvalidAssignment(v);
    //    }
    //}
    #endregion

    #region Fields  //////////////////////////////////////////////////////////////////////////////////////////////////////

    public DValue[] Fields;// = new DValue[0];//This is explicityly left unprotected to reduce access overhead
    void ResizeFields(int maxIndex)
    {
      if (maxIndex >= Fields.Length)
      {
        int newCapacity = maxIndex + 8; //TODO: should we just extend by one?

        var newFields = new DValue[newCapacity];
        Array.Copy(Fields, newFields, Fields.Length);
        Fields = newFields;
      }
    }
    #endregion

    #region GetPropertyDescriptor
    //public PropertyDescriptor GetPropertyDescriptorByLineId(int fieldId, int lineId)
    //{
    //    return GetPropertyDescriptorByFieldId(fieldId);
    //}

    public PropertyDescriptor GetPropertyDescriptorByFieldId(int fieldId)
    {
      PropertyDescriptor pd = mdr.Runtime.LookupLastAcessedPropertyCacheForRead(fieldId, Map);
      if (pd == null)
      {
        pd = Map.GetPropertyDescriptorByFieldId(fieldId);
        if (pd == null)
          pd = Map.Metadata.AddInheritedProperty(Runtime.Instance.GetFieldName(fieldId), Runtime.InvalidFieldIndex, fieldId, PropertyDescriptor.Attributes.Undefined);
        mdr.Runtime.UpdateLastAcessedPropertyCache(fieldId, Map, pd);
      }

      return pd;
    }
    public virtual PropertyDescriptor GetPropertyDescriptor(string field)
    {
      var pd = Map.GetPropertyDescriptor(field);
      if (pd == null)
        pd = Map.Metadata.AddInheritedProperty(field, Runtime.Instance.GetFieldId(field), Runtime.InvalidFieldIndex, PropertyDescriptor.Attributes.Undefined);
      return pd;
    }
    public virtual PropertyDescriptor GetPropertyDescriptor(double field) { return GetPropertyDescriptor(field.ToString()); }
    public virtual PropertyDescriptor GetPropertyDescriptor(int field) { return GetPropertyDescriptor(field.ToString()); }
    public virtual PropertyDescriptor GetPropertyDescriptor(bool field) { return GetPropertyDescriptor(field ? "true" : "false"); }
    public virtual PropertyDescriptor GetPropertyDescriptor(DObject field) { return GetPropertyDescriptor(field.ToString()); }
    public PropertyDescriptor GetPropertyDescriptor(ref DValue field)
    {
      switch (field.ValueType)
      {
        case ValueTypes.Undefined: return GetPropertyDescriptor(Runtime.Instance.DefaultDUndefined.ToString());
        case ValueTypes.String: return GetPropertyDescriptor(field.AsString());
        case ValueTypes.Char: return GetPropertyDescriptor(field.AsChar());
        case ValueTypes.Boolean: return GetPropertyDescriptor(field.AsBoolean());
        case ValueTypes.Float: return GetPropertyDescriptor(field.AsFloat());
        case ValueTypes.Double: return GetPropertyDescriptor(field.AsDouble());
        case ValueTypes.Int8: return GetPropertyDescriptor(field.AsInt8());
        case ValueTypes.Int16: return GetPropertyDescriptor(field.AsInt16());
        case ValueTypes.Int32: return GetPropertyDescriptor(field.AsInt32());
        case ValueTypes.Int64: return GetPropertyDescriptor(field.AsInt64());
        case ValueTypes.UInt8: return GetPropertyDescriptor(field.AsUInt8());
        case ValueTypes.UInt16: return GetPropertyDescriptor(field.AsUInt16());
        case ValueTypes.UInt32: return GetPropertyDescriptor(field.AsUInt32());
        case ValueTypes.UInt64: return GetPropertyDescriptor(field.AsUInt64());

        case ValueTypes.Object:
        case ValueTypes.Function:
        case ValueTypes.Array:
          //case ValueTypes.Property:
          return GetPropertyDescriptor(field.AsDObject().ToString());
        case ValueTypes.Null: return GetPropertyDescriptor(Runtime.Instance.DefaultDNull.ToString());
        //default:
        //    return GetPropertyDescriptor(field.ToString()); //This should never happen
      }
      Trace.Fail(new InvalidOperationException(string.Format("Cannot lookup property using field index type {0}", field.ValueType)));
      return null;
    }
    #endregion

    #region AddPropertyDescriptor
    //public PropertyDescriptor AddPropertyDescriptorByLineId(int fieldId, int lineId)
    //{
    //    return AddPropertyDescriptorByFieldId(fieldId);
    //}
    bool MustAddOwnPropertyDescriptor(PropertyDescriptor propDesc)
    {
      return propDesc == null || propDesc.IsUndefined || (propDesc.IsInherited && propDesc.IsDataDescriptor);
    }
    public PropertyDescriptor AddPropertyDescriptorByFieldId(int fieldId)
    {
      PropertyMap newMap;
      PropertyDescriptor propDesc = mdr.Runtime.LookupLastAcessedPropertyCacheForWrite(fieldId, Map, out newMap);
      //            PropertyDescriptor propDesc = mdr.Runtime.LookupLastAcessedPropertyCacheForRead(fieldId, Map);
      if (propDesc == null)
      {
        propDesc = Map.GetPropertyDescriptorByFieldId(fieldId);
      }
      if (MustAddOwnPropertyDescriptor(propDesc)
        && newMap == null //this means either there was no hit in the cache, or maps matched
        )
      {
        propDesc = Map.AddOwnProperty(this, Runtime.Instance.GetFieldName(fieldId), fieldId, PropertyDescriptor.Attributes.Data);
        mdr.Runtime.UpdateLastAcessedPropertyCache(fieldId, Map, propDesc);
      }
      else if (newMap != null)
      {
        var overriddenProp = newMap.OverriddenPropery;
        if (MustAddOwnPropertyDescriptor(overriddenProp))
        {
          Debug.Assert(
            propDesc == newMap.Property
            && propDesc.NameId == fieldId
            && newMap.Property.Name == Runtime.Instance.GetFieldName(fieldId)
            , "Invalid situation!");
          this.Map = newMap;
        }
        else
        {
          Debug.Assert(overriddenProp.IsInherited && overriddenProp.IsAccessorDescriptor, "Invalid situation");
          return overriddenProp;
        }
      }
      return propDesc;
    }
    public virtual PropertyDescriptor AddPropertyDescriptor(string field)
    {
      var propDesc = Map.GetPropertyDescriptor(field);
      if (MustAddOwnPropertyDescriptor(propDesc))
        propDesc = Map.AddOwnProperty(this, field, Runtime.Instance.GetFieldId(field), PropertyDescriptor.Attributes.Data);
      return propDesc;
    }
    public virtual PropertyDescriptor AddPropertyDescriptor(double field) { return AddPropertyDescriptor(field.ToString()); }
    public virtual PropertyDescriptor AddPropertyDescriptor(int field) { return AddPropertyDescriptor(field.ToString()); }
    public virtual PropertyDescriptor AddPropertyDescriptor(bool field) { return AddPropertyDescriptor(field ? "true" : "false"); }
    public virtual PropertyDescriptor AddPropertyDescriptor(DObject field) { return AddPropertyDescriptor(field.ToString()); }
    public PropertyDescriptor AddPropertyDescriptor(ref DValue field)
    {
      switch (field.ValueType)
      {
        case ValueTypes.Undefined: return AddPropertyDescriptor(Runtime.Instance.DefaultDUndefined.ToString());
        case ValueTypes.String: return AddPropertyDescriptor(field.AsString());
        case ValueTypes.Char: return AddPropertyDescriptor(field.AsChar());
        case ValueTypes.Boolean: return AddPropertyDescriptor(field.AsBoolean());
        case ValueTypes.Float: return AddPropertyDescriptor(field.AsFloat());
        case ValueTypes.Double: return AddPropertyDescriptor(field.AsDouble());
        case ValueTypes.Int8: return AddPropertyDescriptor(field.AsInt8());
        case ValueTypes.Int16: return AddPropertyDescriptor(field.AsInt16());
        case ValueTypes.Int32: return AddPropertyDescriptor(field.AsInt32());
        case ValueTypes.Int64: return AddPropertyDescriptor(field.AsInt64());
        case ValueTypes.UInt8: return AddPropertyDescriptor(field.AsUInt8());
        case ValueTypes.UInt16: return AddPropertyDescriptor(field.AsUInt16());
        case ValueTypes.UInt32: return AddPropertyDescriptor(field.AsUInt32());
        case ValueTypes.UInt64: return AddPropertyDescriptor(field.AsUInt64());
        case ValueTypes.Object:
        case ValueTypes.Function:
        case ValueTypes.Array:
          //case ValueTypes.Property:
          return AddPropertyDescriptor(field.AsDObject().ToString());
        case ValueTypes.Null: return AddPropertyDescriptor(Runtime.Instance.DefaultDNull.ToString());
        //default:
        //    return AddPropertyDescriptor(field.ToString()); //This should never happen
      }
      Trace.Fail(new InvalidOperationException(string.Format("Cannot add property using field index type {0}", field.ValueType)));
      return null;
    }
    #endregion

    #region DeletePropertyDescriptor
    //public PropertyDescriptor DeletePropertyDescriptorByLineId(int fieldId, int lineId)
    //{
    //    return DeletePropertyDescriptorByFieldId(fieldId);
    //}
    public PropertyMap.DeleteStatus DeletePropertyDescriptorByFieldId(int fieldId) { return Map.DeleteOwnPropertyDescriptorByFieldId(this, fieldId); }
    public virtual PropertyMap.DeleteStatus DeletePropertyDescriptor(string field) { return Map.DeleteOwnPropertyDescriptor(this, field); }
    public virtual PropertyMap.DeleteStatus DeletePropertyDescriptor(double field) { return DeletePropertyDescriptor(field.ToString()); }
    public virtual PropertyMap.DeleteStatus DeletePropertyDescriptor(int field) { return DeletePropertyDescriptor(field.ToString()); }
    public virtual PropertyMap.DeleteStatus DeletePropertyDescriptor(bool field) { return DeletePropertyDescriptor(field ? "true" : "false"); }
    public virtual PropertyMap.DeleteStatus DeletePropertyDescriptor(DObject field) { return DeletePropertyDescriptor(field.ToString()); }
    public PropertyMap.DeleteStatus DeletePropertyDescriptor(ref DValue field)
    {
      switch (field.ValueType)
      {
        case ValueTypes.Undefined: return DeletePropertyDescriptor(Runtime.Instance.DefaultDUndefined.ToString());
        case ValueTypes.String: return DeletePropertyDescriptor(field.AsString());
        case ValueTypes.Char: return DeletePropertyDescriptor(field.AsChar());
        case ValueTypes.Boolean: return DeletePropertyDescriptor(field.AsBoolean());
        case ValueTypes.Float: return DeletePropertyDescriptor(field.AsFloat());
        case ValueTypes.Double: return DeletePropertyDescriptor(field.AsDouble());
        case ValueTypes.Int8: return DeletePropertyDescriptor(field.AsInt8());
        case ValueTypes.Int16: return DeletePropertyDescriptor(field.AsInt16());
        case ValueTypes.Int32: return DeletePropertyDescriptor(field.AsInt32());
        case ValueTypes.Int64: return DeletePropertyDescriptor(field.AsInt64());
        case ValueTypes.UInt8: return DeletePropertyDescriptor(field.AsUInt8());
        case ValueTypes.UInt16: return DeletePropertyDescriptor(field.AsUInt16());
        case ValueTypes.UInt32: return DeletePropertyDescriptor(field.AsUInt32());
        case ValueTypes.UInt64: return DeletePropertyDescriptor(field.AsUInt64());
        case ValueTypes.Object:
        case ValueTypes.Function:
        case ValueTypes.Array:
          //case ValueTypes.Property:
          return DeletePropertyDescriptor(field.AsDObject().ToString());
        case ValueTypes.Null: return DeletePropertyDescriptor(Runtime.Instance.DefaultDNull.ToString());
        //default:
        //    return DeletePropertyDescriptor(field.ToString()); //This should never happen
      }
      Trace.Fail(new InvalidOperationException(string.Format("Cannot delete property using field index type {0}", field.ValueType)));
      return PropertyMap.DeleteStatus.NotFound;
    }
    #endregion

    #region HasOwnProperty
    public bool HasOwnPropertyByFieldId(int fieldId)
    {
      var pd = GetPropertyDescriptorByFieldId(fieldId);
      return (pd != null) && (!pd.IsUndefined) && (!pd.IsInherited);
    }
    public bool HasOwnProperty(string field)
    {
      var pd = GetPropertyDescriptor(field);
      return (pd != null) && (!pd.IsUndefined) && (!pd.IsInherited);
    }
    public bool HasOwnProperty(double field)
    {
      var pd = GetPropertyDescriptor(field);
      return (pd != null) && (!pd.IsUndefined) && (!pd.IsInherited);
    }
    public bool HasOwnProperty(int field)
    {
      var pd = GetPropertyDescriptor(field);
      return (pd != null) && (!pd.IsUndefined) && (!pd.IsInherited);
    }
    public bool HasOwnProperty(bool field)
    {
      var pd = GetPropertyDescriptor(field);
      return (pd != null) && (!pd.IsUndefined) && (!pd.IsInherited);
    }
    public bool HasOwnProperty(DObject field)
    {
      var pd = GetPropertyDescriptor(field);
      return (pd != null) && (!pd.IsUndefined) && (!pd.IsInherited);
    }
    public bool HasOwnProperty(ref DValue field)
    {
      var pd = GetPropertyDescriptor(ref field);
      return (pd != null) && (!pd.IsUndefined) && (!pd.IsInherited);
    }
    #endregion

    #region DefineOwnProperty
    /// only none-array fields can have attributes other than Data, therefore we only need the followings and no need to virtualize
    public PropertyDescriptor AddOwnPropertyDescriptorByFieldId(int fieldId, PropertyDescriptor.Attributes attributes)
    {
      Debug.Assert(fieldId >= 0, "FieldId cannot be negative!");
      var propDesc = Map.GetPropertyDescriptorByFieldId(fieldId);
      if (propDesc == null || propDesc.IsUndefined || propDesc.IsInherited)
        propDesc = Map.AddOwnProperty(this, Runtime.Instance.GetFieldName(fieldId), fieldId, attributes);
      else
        Trace.Assert(attributes == propDesc.GetAttributes(), "Updating existing own propery with different attributes is not supported via FieldID");

      Debug.Assert(!propDesc.IsInherited && (propDesc.IsDataDescriptor || propDesc.IsAccessorDescriptor), "You can use this method only for own properties");
      return propDesc;
    }
    PropertyDescriptor AddOwnPropertyDescriptor(string field, PropertyDescriptor.Attributes attributes)
    {
      var propDesc = Map.GetPropertyDescriptor(field);
      if (propDesc == null || propDesc.IsUndefined || propDesc.IsInherited)
        propDesc = Map.AddOwnProperty(this, field, Runtime.Instance.GetFieldId(field), attributes);
      else if (attributes != propDesc.GetAttributes())
      {
        Trace.Assert(!propDesc.IsNotConfigurable, "Cannot reconfigure property {0} in the object", field);
        var maps = new PropertyMap[Map.Property.Index + 1];
        var i = 0;
        var m = Map;
        while (m.Property != propDesc)
        {
          maps[i++] = m;
          m = m.Parent;
        }
        Debug.Assert(
          propDesc != null
          && m != null
          && m.Parent != null
          , "Invalid situation, we must have found a proper map pointing to existing property descriptor");
        m = m.Parent.AddOwnProperty(field, Runtime.Instance.GetFieldId(field), attributes);
        for (--i; i >= 0; --i)
        {
          var pd = maps[i].Property;
          m = m.AddOwnProperty(pd.Name, pd.NameId, pd.GetAttributes());
        }
        Map = m;
      }
      Debug.Assert(!propDesc.IsInherited && (propDesc.IsDataDescriptor || propDesc.IsAccessorDescriptor), "You can use this method only for own properties");
      return propDesc;
    }
    public void DefineOwnProperty(string field, string v, PropertyDescriptor.Attributes attributes) { var pd = AddOwnPropertyDescriptor(field, attributes); Fields[pd.Index].Set(v); }
    public void DefineOwnProperty(string field, double v, PropertyDescriptor.Attributes attributes) { var pd = AddOwnPropertyDescriptor(field, attributes); Fields[pd.Index].Set(v); }
    public void DefineOwnProperty(string field, int v, PropertyDescriptor.Attributes attributes) { var pd = AddOwnPropertyDescriptor(field, attributes); Fields[pd.Index].Set(v); }
    public void DefineOwnProperty(string field, bool v, PropertyDescriptor.Attributes attributes) { var pd = AddOwnPropertyDescriptor(field, attributes); Fields[pd.Index].Set(v); }
    public void DefineOwnProperty(string field, DObject v, PropertyDescriptor.Attributes attributes) { var pd = AddOwnPropertyDescriptor(field, attributes); Fields[pd.Index].Set(v); }
    public void DefineOwnProperty(string field, ref DValue v, PropertyDescriptor.Attributes attributes) { var pd = AddOwnPropertyDescriptor(field, attributes); Fields[pd.Index].Set(ref v); }
    public void DefineOwnProperty(string field, DProperty v, PropertyDescriptor.Attributes flags = PropertyDescriptor.Attributes.None)
    {
      var pd = AddOwnPropertyDescriptor(field, PropertyDescriptor.Attributes.Accessor | flags);
      Fields[pd.Index].Set(v);
    }

    #endregion

    #region HasProperty
    public bool HasPropertyByFieldId(int fieldId)
    {
      var pd = GetPropertyDescriptorByFieldId(fieldId);
      return (pd != null) && (!pd.IsUndefined);
    }
    public bool HasProperty(string field)
    {
      var pd = GetPropertyDescriptor(field);
      return (pd != null) && (!pd.IsUndefined);
    }
    public bool HasProperty(double field)
    {
      var pd = GetPropertyDescriptor(field);
      return (pd != null) && (!pd.IsUndefined);
    }
    public bool HasProperty(int field)
    {
      var pd = GetPropertyDescriptor(field);
      return (pd != null) && (!pd.IsUndefined);
    }
    public bool HasProperty(bool field)
    {
      var pd = GetPropertyDescriptor(field);
      return (pd != null) && (!pd.IsUndefined);
    }
    public bool HasProperty(DObject field)
    {
      var pd = GetPropertyDescriptor(field);
      return (pd != null) && (!pd.IsUndefined);
    }
    public bool HasProperty(ref DValue field)
    {
      var pd = GetPropertyDescriptor(ref field);
      return (pd != null) && (!pd.IsUndefined);
    }
    #endregion

    #region GetField

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public void GetFieldByFieldId(int fieldId, ref DValue v) { GetPropertyDescriptorByFieldId(fieldId).Get(this, ref v); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public void GetField(string field, ref DValue v) { GetPropertyDescriptor(field).Get(this, ref v); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public void GetField(double field, ref DValue v) { GetPropertyDescriptor(field).Get(this, ref v); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public void GetField(int field, ref DValue v) { GetPropertyDescriptor(field).Get(this, ref v); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public void GetField(bool field, ref DValue v) { GetPropertyDescriptor(field).Get(this, ref v); }
    public void GetField(DObject field, ref DValue v) { GetPropertyDescriptor(field).Get(this, ref v); }
    public void GetField(ref DValue field, ref DValue v) { GetPropertyDescriptor(ref field).Get(this, ref v); }

    public void GetFieldByPD(PropertyDescriptor pd, ref DValue v) { pd.Get(this, ref v); }
    //public void GetField(DValue field, ref DValue v) {GetPropertyDescriptor(field).Get(this, ref v); }

    public DValue GetFieldByFieldId(int fieldId) { DValue tmp = new DValue(); GetFieldByFieldId(fieldId, ref tmp); return tmp; }
    public DValue GetField(string field) { DValue tmp = new DValue(); GetField(field, ref tmp); return tmp; }
    public DValue GetField(double field) { DValue tmp = new DValue(); GetField(field, ref tmp); return tmp; }
    public DValue GetField(int field) { DValue tmp = new DValue(); GetField(field, ref tmp); return tmp; }
    public DValue GetField(bool field) { DValue tmp = new DValue(); GetField(field, ref tmp); return tmp; }
    public DValue GetField(DObject field) { DValue tmp = new DValue(); GetField(field, ref tmp); return tmp; }
    public DValue GetField(ref DValue field) { DValue tmp = new DValue(); GetField(ref field, ref tmp); return tmp; }
    //public DValue GetField(DValue field) { DValue tmp = new DValue(); GetField(field, ref tmp); return tmp; }
    #endregion

    #region SetField
    public void SetFieldByFieldId(int fieldId, string v) { AddPropertyDescriptorByFieldId(fieldId).Set(this, v); }
    public void SetFieldByFieldId(int fieldId, double v) { AddPropertyDescriptorByFieldId(fieldId).Set(this, v); }
    public void SetFieldByFieldId(int fieldId, int v) { AddPropertyDescriptorByFieldId(fieldId).Set(this, v); }
    public void SetFieldByFieldId(int fieldId, bool v) { AddPropertyDescriptorByFieldId(fieldId).Set(this, v); }
    public void SetFieldByFieldId(int fieldId, DObject v) { AddPropertyDescriptorByFieldId(fieldId).Set(this, v); }
    public void SetFieldByFieldId(int fieldId, ref DValue v) { AddPropertyDescriptorByFieldId(fieldId).Set(this, ref v); }
    //public void SetFieldByFieldId(int fieldId, DValue v) { AddPropertyDescriptorByLineId(fieldId, lineId).Set(this, v); }

    public void SetField(string field, string v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(string field, double v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(string field, int v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(string field, bool v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(string field, DObject v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(string field, ref DValue v) { AddPropertyDescriptor(field).Set(this, ref v); }
    //public void SetField(string field, DValue v) { AddPropertyDescriptor(field).Set(this, v);}

    public void SetField(double field, string v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(double field, double v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(double field, int v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(double field, bool v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(double field, DObject v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(double field, ref DValue v) { AddPropertyDescriptor(field).Set(this, ref v); }
    //public void SetField(double field, DValue v) { AddPropertyDescriptor(field).Set(this, v);}

    public void SetField(int field, string v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(int field, double v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(int field, int v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(int field, bool v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(int field, DObject v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(int field, ref DValue v) { AddPropertyDescriptor(field).Set(this, ref v); }
    //public void SetField(int field, DValue v) { AddPropertyDescriptor(field).Set(this, v);}

    public void SetField(bool field, string v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(bool field, double v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(bool field, int v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(bool field, bool v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(bool field, DObject v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(bool field, ref DValue v) { AddPropertyDescriptor(field).Set(this, ref v); }
    //public void SetField(bool field, DValue v) { AddPropertyDescriptor(field).Set(this, v);}

    public void SetField(DObject field, string v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(DObject field, double v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(DObject field, int v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(DObject field, bool v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(DObject field, DObject v) { AddPropertyDescriptor(field).Set(this, v); }
    public void SetField(DObject field, ref DValue v) { AddPropertyDescriptor(field).Set(this, ref v); }
    //public void SetField(DObject field, DValue v) { AddPropertyDescriptor(field).Set(this, v);}

    public void SetField(ref DValue field, string v) { AddPropertyDescriptor(ref field).Set(this, v); }
    public void SetField(ref DValue field, double v) { AddPropertyDescriptor(ref field).Set(this, v); }
    public void SetField(ref DValue field, int v) { AddPropertyDescriptor(ref field).Set(this, v); }
    public void SetField(ref DValue field, bool v) { AddPropertyDescriptor(ref field).Set(this, v); }
    public void SetField(ref DValue field, DObject v) { AddPropertyDescriptor(ref field).Set(this, v); }
    public void SetField(ref DValue field, ref DValue v) { AddPropertyDescriptor(ref field).Set(this, ref v); }
    //public void SetField(ref DValue field, DValue v) { AddPropertyDescriptor(ref field).Set(this, v);}
    #endregion

    [System.Diagnostics.DebuggerStepThrough]
    public virtual void Accept(IMdrVisitor visitor)
    {
      visitor.Visit(this);
    }
  }
}
