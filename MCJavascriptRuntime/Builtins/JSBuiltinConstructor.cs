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

namespace mjr.Builtins
{
    class JSBuiltinConstructor : mdr.DFunction
    {
        protected bool IsConstrutor;
        protected mdr.DObject TargetPrototype;
        protected mdr.PropertyMap TargetDType;

        protected JSBuiltinConstructor(mdr.DObject prototype, string Class)
            : base(null, null)
        {
            TargetPrototype = prototype;
            TargetDType = mdr.Runtime.Instance.GetRootMapOfPrototype(TargetPrototype);
            TargetDType.Metadata.Name = Class;
            //var protoPD = PrototypePropertyDescriptor;
            //protoPD.SetAttributes(mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable, true);
            //protoPD.Set(this, prototype);
            SetField("prototype", prototype);
            prototype.DefineOwnProperty(
                "constructor"
                , this
                , mdr.PropertyDescriptor.Attributes.Data | mdr.PropertyDescriptor.Attributes.NotEnumerable
            );
            //prototype.SetField("constructor", this);
        }

        /// <summary>
        /// We use this function, because in all builtings, the original prototype object (i.e. initial value of the .prototype property) is used 
        /// rather than the current version. E.g. 15.5.2.1
        /// </summary>
        /// <param name="callFrame"></param>
        public override void Construct(ref mdr.CallFrame callFrame)
        {
            IsConstrutor = true;
            Call(ref callFrame);
            IsConstrutor = false;
        }

        //internal static void SetBuiltinField(mdr.DObject obj, string field, string v, mdr.PropertyDescriptor.Attributes attributes = mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data)
        //{
        //    obj.DefineOwnProperty(field, v, attributes);
        //    //We cannot use the following in case we have NotWritable attribute
        //    //var pd = obj.AddPropertyDescriptor(field, attributes); 
        //    //pd.Set(obj, v);
        //}
        //internal static void SetBuiltinField(mdr.DObject obj, string field, double v, mdr.PropertyDescriptor.Attributes attributes = mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data)
        //{
        //    obj.DefineOwnProperty(field, v, attributes);
        //}
        //internal static void SetBuiltinField(mdr.DObject obj, string field, mdr.DObject v, mdr.PropertyDescriptor.Attributes attributes = mdr.PropertyDescriptor.Attributes.NotEnumerable| mdr.PropertyDescriptor.Attributes.Data)
        //{
        //    obj.DefineOwnProperty(field, v, attributes);
        //}
    }
}
