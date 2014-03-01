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
    public class PropertyDescriptor
    {
        public delegate void AccessorFunc(PropertyDescriptor pd, DObject obj, ref DValue v);
        // TODO: Commented Ignore_Get since it's never used.
        /*static readonly AccessorFunc Ignore_Get = (PropertyDescriptor pd, DObject obj, ref DValue v) => { Debug.WriteLine("Ignored getting property"); };*/
        static readonly AccessorFunc Ignore_Set = (PropertyDescriptor pd, DObject obj, ref DValue v) => { Debug.WriteLine("Ignored setting property"); };
        static readonly AccessorFunc Undefined_Get = (PropertyDescriptor pd, DObject obj, ref DValue v) => { v.Set(Runtime.Instance.DefaultDUndefined); };
        static readonly AccessorFunc Undefined_Set = (PropertyDescriptor pd, DObject obj, ref DValue v) => { Trace.Fail("Should not set value for undefeined"); };
        static readonly AccessorFunc Own_Data_Get = (PropertyDescriptor pd, DObject obj, ref DValue v) => { v = obj.Fields[pd.Index]; };
        static readonly AccessorFunc Own_Data_Set = (PropertyDescriptor pd, DObject obj, ref DValue v) => { obj.Fields[pd.Index] = v; };
        //static readonly AccessorFunc Own_Data_Inline0_Get = (PropertyDescriptor pd, DObject obj, ref DValue v) => { v = (obj as DObject1).InlineField0; };
        //static readonly AccessorFunc Own_Data_Inline0_Set = (PropertyDescriptor pd, DObject obj, ref DValue v) => { (obj as DObject1).InlineField0 = v; };
        static readonly AccessorFunc Own_Accessor_Get = (PropertyDescriptor pd, DObject obj, ref DValue v) => { (obj.Fields[pd.Index].AsDProperty()).Get(obj, ref v); };
        static readonly AccessorFunc Own_Accessor_Set = (PropertyDescriptor pd, DObject obj, ref DValue v) => { (obj.Fields[pd.Index].AsDProperty()).Set(obj, ref v); };
        static readonly AccessorFunc Inherited_Data_Get = (PropertyDescriptor pd, DObject obj, ref DValue v) => { v = pd.Container.Fields[pd.Index]; };
        //static Action Inherited_Data_Set = (PropertyDescriptor pd, DObject obj, ref DValue v) => { Trace.Fail("Must not set value for inherited property {0}", pd.Name); };
        //Technically, we should not set value of inherited data desc. But, for Global and ParentLocal variables in the function context, this can happen.
        static readonly AccessorFunc Inherited_Data_Set = (PropertyDescriptor pd, DObject obj, ref DValue v) => { pd.Container.Fields[pd.Index] = v; };
        static readonly AccessorFunc Inherited_Accessor_Get = (PropertyDescriptor pd, DObject obj, ref DValue v) => { (pd.Container.Fields[pd.Index].AsDProperty()).Get(obj, ref v); };
        static readonly AccessorFunc Inherited_Accessor_Set = (PropertyDescriptor pd, DObject obj, ref DValue v) => { (pd.Container.Fields[pd.Index].AsDProperty()).Set(obj, ref v); };

        public virtual DProperty GetProperty() {
            if (Index < 0)
                return null;
            var prop = Container.Fields[Index].AsDProperty();
            return prop.GetEffectiveProperty();
        }

        //We use the negative version of attributes to make the most common cases as default
        [Flags]
        public enum Attributes
        {
            None = 0,
            NotEnumerable = 1 << 0,
            NotConfigurable = 1 << 1,
            NotWritable = 1 << 2,
            //Deleted = 1 << 3,
            Data = 1 << 3,
            Accessor = 1 << 4,
            Undefined = 1 << 5,
            Inherited = 1 << 6,
            Inlined = 1 << 7,
        }
        Attributes _flags;
        public Attributes GetAttributes() { return _flags; }
        public bool HasAttributes(Attributes flags) { return (_flags & flags) != 0; }
        public void SetAttributes(Attributes flags, bool set)
        {
            if (set)
                _flags |= flags;
            else
                _flags &= ~flags;

            if (!IsInherited)
            {
                if (IsDataDescriptor)
                {
                    _getter = Own_Data_Get;
                    _setter = Own_Data_Set;
                }
                else if (IsAccessorDescriptor)
                {
                    _getter = Own_Accessor_Get;
                    _setter = Own_Accessor_Set;
                }
                else
                {
                    Debug.Assert(IsUndefined || _flags == Attributes.None, "Property descriptor of {0} has invalid type {1}", Name, _flags);
                    _getter = Undefined_Get;
                    _setter = Undefined_Set;
                }
            }
            else
            {
                if (IsDataDescriptor)
                {
                    _getter = Inherited_Data_Get;
                    _setter = Inherited_Data_Set;
                }
                else if (IsAccessorDescriptor)
                {
                    _getter = Inherited_Accessor_Get;
                    _setter = Inherited_Accessor_Set;
                }
                else
                {
                    Debug.Assert(IsUndefined, "Property descriptor of {0} has invalid type {1}", Name, _flags);
                    //TODO: should we remove Inhereted from _flags?
                    _getter = Undefined_Get;
                    _setter = Undefined_Set;
                }
            }
            if (IsNotWritable)
                _setter = Ignore_Set;

        }
        public void ResetAttributes(Attributes flags)
        {
            _flags = Attributes.None;
            SetAttributes(flags, true);
        }
        public bool IsNotEnumerable { get { return HasAttributes(Attributes.NotEnumerable); } set { SetAttributes(Attributes.NotEnumerable, value); } }
        public bool IsNotConfigurable { get { return HasAttributes(Attributes.NotConfigurable); } set { SetAttributes(Attributes.NotConfigurable, value); } }
        public bool IsNotWritable { get { return HasAttributes(Attributes.NotWritable); } set { SetAttributes(Attributes.NotWritable, value); } }
        //public bool IsDeleted { get { return HasAttributes(Attributes.Deleted); } set { SetAttributes(Attributes.Deleted, value); } }

        public bool IsDataDescriptor { get { return HasAttributes(Attributes.Data); } set { SetAttributes(Attributes.Data, value); } }
        public bool IsAccessorDescriptor { get { return HasAttributes(Attributes.Accessor); } set { SetAttributes(Attributes.Accessor, value); } }
        public bool IsInherited { get { return HasAttributes(Attributes.Inherited); } set { SetAttributes(Attributes.Inherited, value); } }
        public bool IsUndefined { get { return HasAttributes(Attributes.Undefined); } set { SetAttributes(Attributes.Undefined, value); } }
        public bool IsInlined { get { return HasAttributes(Attributes.Inlined); } set { SetAttributes(Attributes.Inlined, value); } }

        //The following fields should never change, so they are readonly
        public readonly string Name;
        public readonly int NameId;

        //If prototype gets a new field, we may have to change Index (only valid if IsInherited)
        public int Index;

        public int ObjectCacheIndex = -1;

        /// <summary>
        /// This is valid only for inherited descriptors and refers to actual object that contains the field.
        /// </summary>
        public DObject Container;

        // To avoid checking the conditions every time, we use the followings, which are initialized based on attributes
        AccessorFunc _getter;
        AccessorFunc _setter;

        //We use the following for special types of PropertyDescriptor that does not change attributes etc. Use this with caution!
        public AccessorFunc Getter { set { _getter = value; } }
        public AccessorFunc Setter { set { _setter = value; } }

        public PropertyDescriptor(string name, int nameId = Runtime.InvalidFieldId, int index = Runtime.InvalidFieldIndex, Attributes flags = Attributes.None)
        {
            Name = name;
            if (nameId == Runtime.InvalidFieldId)
                NameId = Runtime.Instance.GetFieldId(name);
            else
                NameId = nameId;
            Index = index;
            SetAttributes(flags, true);
            Debug.Assert(
                HasAttributes(Attributes.Data | Attributes.Accessor | Attributes.Undefined)
                || flags==Attributes.None
                , "descriptor of {0} does not specify type", Name
            );
        }

//#if __MonoCS__
//    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
//#endif
        public void Get(DObject obj, ref DValue value)
        {
          /*if (mdr.Runtime.Instance.Configuration.ProfileStats)
          {
            mdr.Runtime.Instance.Counters.GetCounter("PD.GET").Count++;
            mdr.Runtime.Instance.Counters.GetCounter("PD.GET[" + _flags + "]").Count++;
          } */     
            _getter(this, obj, ref value);
        }
        public DValue Get(DObject obj)
        {
            DValue tmp = new DValue();
            Get(obj, ref tmp);
            return tmp;
        }

        public void Set(DObject obj, string value)
        {
            //if (IsDataDescriptor && !IsInherited)
            if (_setter == Own_Data_Set) //This is the most common case, and we can make it faster this way
                obj.Fields[Index].Set(value);
            else
            {
                var tmp = new DValue();
                tmp.Set(value);
                Set(obj, ref tmp);
            }
        }
        public void Set(DObject obj, double value)
        {
            if (_setter == Own_Data_Set) //This is the most common case, and we can make it faster this way
                obj.Fields[Index].Set(value);
            else
            {
                var tmp = new DValue();
                tmp.Set(value);
                Set(obj, ref tmp);
            }
        }

        public void Set(DObject obj, int value)
        {
          if (_setter == Own_Data_Set) //This is the most common case, and we can make it faster this way
            obj.Fields[Index].Set(value);
          else
          {
            var tmp = new DValue();
            tmp.Set(value);
            Set(obj, ref tmp);
          }
        }
        public void Set(DObject obj, uint value)
        {
          if (_setter == Own_Data_Set) //This is the most common case, and we can make it faster this way
            obj.Fields[Index].Set(value);
          else
          {
            var tmp = new DValue();
            tmp.Set(value);
            Set(obj, ref tmp);
          }
        }
        public void Set(DObject obj, bool value)
        {
            if (_setter == Own_Data_Set) //This is the most common case, and we can make it faster this way
                obj.Fields[Index].Set(value);
            else
            {
                var tmp = new DValue();
                tmp.Set(value);
                Set(obj, ref tmp);
            }
        }
        public void Set(DObject obj, DObject value)
        {
            if (_setter == Own_Data_Set) //This is the most common case, and we can make it faster this way
                obj.Fields[Index].Set(value);
            else
            {
                var tmp = new DValue();
                tmp.Set(value);
                Set(obj, ref tmp);
            }
        }
        public void Set(DObject obj, DFunction value) { Set(obj, (DObject)value); } //To make sure CodeGen can detect this
        public void Set(DObject obj, DArray value) { Set(obj, (DObject)value); } //To make sure CodeGen can detect this
        public void Set(DObject obj, DUndefined value) { Set(obj, (DObject)value); } //To make sure CodeGen can detect this
        public void Set(DObject obj, DNull value) { Set(obj, (DObject)value); } //To make sure CodeGen can detect this
        public void Set(DObject obj, ref DValue value)
        {
            _setter(this, obj, ref value);
        }

        public override string ToString()
        {
          return string.Format("{0}->({1},{2})[{3}]", Index, Name, NameId, GetAttributes());
        }
    }
}
