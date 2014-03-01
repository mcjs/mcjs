// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using mdr;
using m.Util.Diagnose;
using System.Collections.Generic;

namespace mjr.Builtins
{
    class JSObject : JSBuiltinConstructor
    {
        public JSObject()
            : base(mdr.Runtime.Instance.DObjectPrototype, "Object")
        {
            JittedCode = ctor;

            this.DefineOwnProperty("getPrototypeOf", new DFunction(getPrototypeOf), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("getOwnPropertyDescriptor", new DFunction(getOwnPropertyDescriptor), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("getOwnPropertyNames", new DFunction(getOwnPropertyNames), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("create", new DFunction(create), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("defineProperty", new DFunction(defineProperty), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("defineProperties", new DFunction(defineProperties), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("seal", new DFunction(seal), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("freeze", new DFunction(freeze), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("preventExtensions", new DFunction(preventExtensions), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("isSealed", new DFunction(isSealed), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("isFrozen", new DFunction(isFrozen), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("isExtensible", new DFunction(isExtensible), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("keys", new DFunction(keys), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            TargetPrototype.DefineOwnProperty("toString", new DFunction(toString), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("toLocaleString", new DFunction(toLocaleString), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("valueOf", new DFunction(valueOf), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("hasOwnProperty", new DFunction(hasOwnProperty), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("isPrototypeOf", new DFunction(isPrototypeOf), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("propertyIsEnumerable", new DFunction(propertyIsEnumerable), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
        }

        private void DefineProperty(DObject O, string name, DObject desc)
        {
          if (desc == null || O == null || name == null)
            Trace.Fail("TypeError");

          var getter = new DValue();
          var setter = new DValue();
          var value = new DValue();
          var attributes = PropertyDescriptor.Attributes.NotEnumerable | PropertyDescriptor.Attributes.NotWritable | PropertyDescriptor.Attributes.NotConfigurable;

          attributes = PropertyDescriptor.Attributes.NotEnumerable | PropertyDescriptor.Attributes.NotWritable | PropertyDescriptor.Attributes.NotConfigurable;

          getter.SetUndefined();
          setter.SetUndefined();
          value.SetUndefined();

          value = desc.HasProperty("value") ? desc.GetField("value") : value;

          if (desc.HasProperty("enumerable"))
            attributes &= desc.GetField("enumerable").AsBoolean() ? ~PropertyDescriptor.Attributes.NotEnumerable : attributes;


          if (desc.HasProperty("configurable"))
            attributes &= desc.GetField("configurable").AsBoolean() ? ~PropertyDescriptor.Attributes.NotConfigurable : attributes;

          if (desc.HasProperty("writable"))
            attributes &= desc.GetField("writable").AsBoolean() ? ~PropertyDescriptor.Attributes.NotWritable : attributes;

          if (desc.HasProperty("get"))
          {
            getter = desc.GetField("get");
            if (!ValueTypesHelper.IsUndefined(getter.ValueType) && !ValueTypesHelper.IsFunction(getter.ValueType))
              Trace.Fail("TypeError");
          }

          if (desc.HasProperty("set"))
          {
            setter = desc.GetField("set");
            if (!ValueTypesHelper.IsUndefined(setter.ValueType) && !ValueTypesHelper.IsFunction(setter.ValueType))
              Trace.Fail("TypeError");
          }

          Trace.Assert(
            !((desc.HasProperty("get") || desc.HasProperty("set"))
            && (desc.HasProperty("value") || desc.HasProperty("writable"))),
            "If either getter or setter needs to be defined, value or writable shouldn't be defined.");

          if (desc.HasProperty("value"))
            O.DefineOwnProperty(name, ref value, attributes | PropertyDescriptor.Attributes.Data);
          else
          {
            var property = new DProperty();
            if (ValueTypesHelper.IsFunction(getter.ValueType))
              property.Getter = getter.AsDFunction();
            if (ValueTypesHelper.IsFunction(setter.ValueType))
              property.Setter = setter.AsDFunction();

            O.DefineOwnProperty(name, property, attributes | PropertyDescriptor.Attributes.Accessor);
          }
        }

        private void DefineProperties(DObject O, DObject props)
        {
          if (O == null)
            Trace.Fail("TypeError");

          var names = new List<string>();

          for (var m = props.Map; m.Property.Name != null; m = m.Parent)
            names.Add(m.Property.Name);
          names.Reverse();

          //TODO: This can be done in a single go.
          foreach (var P in names)
          {
            var desc = props.GetField(P).AsDObject();
            DefineProperty(O, P, desc);
          }
        }

        //ECMA-262 section 15.2.2.1
        void ctor(ref mdr.CallFrame callFrame)
        {
          if (IsConstrutor)
          {
            //var p = GetField("prototype");
            //callFrame.This.Set(new mdr.DObject(p.DObjectValue));
            callFrame.This = (new mdr.DObject(TargetPrototype));
          }
        }

        //ECMA-262 section 15.2.3.2   
        void getPrototypeOf(ref mdr.CallFrame callFrame) 
        {
          Debug.WriteLine("calling JSObject.getPrototypeOf");
          var O = callFrame.Arg0.AsDObject();
          if (O == null)
            Trace.Fail("TypeError");

          callFrame.Return.Set(O.Prototype);
        }

        //ECMA-262 section 15.2.3.3   
        void getOwnPropertyDescriptor(ref mdr.CallFrame callFrame) 
        {
          Debug.WriteLine("calling JSObject.getPropertyDescriptor");
          var O = callFrame.Arg0.AsDObject();
          if (O == null)
            Trace.Fail("TypeError");

          // TODO: Commented because name and desc are unused.
          /*var name = callFrame.Arg1.ToString();
          var desc = O.GetField(name);    //FIXME: GetField must be GetOwnProperty*/

          Trace.Fail("Unimplemented");
        }
        
        //ECMA-262 section 15.2.3.4   
        void getOwnPropertyNames(ref mdr.CallFrame callFrame) 
        {
          Debug.WriteLine("calling JSObject.getOwnPropertyNames");
          var O = callFrame.Arg0.AsDObject();
          if (O == null)
            Trace.Fail("TypeError");

          var retArray = new DArray(O.Map.Property.Index + 1);
          var i = 0;

          for (var m = O.Map; m.Property.Name != null; m = m.Parent)
          {
            retArray.Elements[i++].Set(m.Property.Name);
          }

          retArray.Length = i;

          //Debug.Assert(i == retArray.Length, "Array not populated correctly!");
          callFrame.Return.Set(retArray);
        }

        //ECMA-262 section 15.2.3.5   
        void create(ref mdr.CallFrame callFrame) 
        {
          Debug.WriteLine("calling JSObject.create");
          var O = callFrame.Arg0.AsDObject();
          if (O == null)
            Trace.Fail("TypeError");
          var obj = new DObject(O);
          var props = callFrame.Arg1.AsDObject();

          if (!ValueTypesHelper.IsUndefined(props.ValueType))
            DefineProperties(obj, props);

          callFrame.Return.Set(obj);
        }

        //ECMA-262 section 15.2.3.6   
        void defineProperty(ref mdr.CallFrame callFrame) 
        {
          Debug.WriteLine("calling JSObject.defineProperty");
          var O = callFrame.Arg0.AsDObject();
          if (O == null)
            Trace.Fail("TypeError");
          var name = callFrame.Arg1.AsString();
          var desc = callFrame.Arg2.AsDObject();

          DefineProperty(O, name, desc);

          callFrame.Return.Set(O);
        }

        //ECMA-262 section 15.2.3.7   
        void defineProperties(ref mdr.CallFrame callFrame) 
        {
          Debug.WriteLine("calling JSObject.defineProperties");
          var O = callFrame.Arg0.AsDObject();
          if (O == null)
            Trace.Fail("TypeError");
          var props = callFrame.Arg1.AsDObject();

          DefineProperties(O, props);

          callFrame.Return.Set(O);
        }

        //ECMA-262 section 15.2.3.8   
        void seal(ref mdr.CallFrame callFrame) { Trace.Fail("Unimplemented"); }
        //ECMA-262 section 15.2.3.9   
        void freeze(ref mdr.CallFrame callFrame) { Trace.Fail("Unimplemented"); }
        //ECMA-262 section 15.2.3.10  
        void preventExtensions(ref mdr.CallFrame callFrame) { Trace.Fail("Unimplemented"); }
        //ECMA-262 section 15.2.3.11  
        void isSealed(ref mdr.CallFrame callFrame) { Trace.Fail("Unimplemented"); }
        //ECMA-262 section 15.2.3.12  
        void isFrozen(ref mdr.CallFrame callFrame) { Trace.Fail("Unimplemented"); }
        //ECMA-262 section 15.2.3.13  
        void isExtensible(ref mdr.CallFrame callFrame) { Trace.Fail("Unimplemented"); }
        //ECMA-262 section 15.2.3.14  
        void keys(ref mdr.CallFrame callFrame) 
        {
          Debug.WriteLine("calling JSObject.keys");
          var O = callFrame.Arg0.AsDObject();
          if (O == null)
            Trace.Fail("TypeError"); 
          var retArray = new DArray(O.Map.Property.Index + 1);
          var i = 0;

          for (var m = O.Map; m.Property.Name != null; m = m.Parent)
          {
            if (!m.Property.IsNotEnumerable && !m.Property.IsInherited && (m.Property.IsDataDescriptor || m.Property.IsAccessorDescriptor))
            {
              retArray.Elements[i++].Set(m.Property.Name);
            }
          }

          retArray.Length = i;

          //Debug.Assert(i == retArray.Length, "Array not populated correctly!");
          callFrame.Return.Set(retArray);

        }

        //ECMA-262 15.2.4.1 
        void constructor(ref mdr.CallFrame callFramer){}
        //ECMA-262 15.2.4.2 

        void toString (ref mdr.CallFrame callFrame)
        {
          DObject obj;
          if (!ValueTypesHelper.IsDefined(callFrame.This.ValueType))
            obj = Runtime.Instance.GlobalContext;
          else
            obj = callFrame.This;

          //callFrame.Return.Set(obj.ToString());
          callFrame.Return.Set(string.Format("[object {0}]", obj.Map.Metadata.Name));
        }

        //ECMA-262 15.2.4.3 
        void toLocaleString(ref mdr.CallFrame callFrame) { Trace.Fail("Unimplemented"); }
        
        //ECMA-262 15.2.4.4 
        void valueOf(ref mdr.CallFrame callFrame) 
        { 
          callFrame.Return.Set(callFrame.This); 
        }

        //ECMA-262 15.2.4.5 
        void hasOwnProperty (ref mdr.CallFrame callFrame)
        {
          var obj = callFrame.This;
          var prop = callFrame.Arg0.AsString();
          callFrame.Return.Set(obj.HasOwnProperty(prop));
        }

        //ECMA-262 15.2.4.6
        void isPrototypeOf(ref mdr.CallFrame callFrame) { Trace.Fail("Unimplemented"); }

        //ECMA-262 15.2.4.7 
        void propertyIsEnumerable(ref mdr.CallFrame callFrame) 
        {
          DObject obj;
          if (!ValueTypesHelper.IsDefined(callFrame.This.ValueType))
            obj = Runtime.Instance.GlobalContext;
          else
            obj = callFrame.This;

          //callFrame.Return.Set(obj.ToString());
          if (callFrame.PassedArgsCount == 1)
          {
            string toString;
            if (callFrame.Arg0.ValueType == ValueTypes.String)
              toString = callFrame.Arg0.AsString();
            else
              toString = callFrame.Arg0.AsDObject().GetField("toString").AsString();
            PropertyDescriptor pd = obj.GetPropertyDescriptor(toString);
            if (pd != null)
              callFrame.Return.Set(!pd.IsNotEnumerable);
            else
              callFrame.Return.Set(false);
          }
          else
          {
            callFrame.Return.Set(false);
          }
        }
    }
}
