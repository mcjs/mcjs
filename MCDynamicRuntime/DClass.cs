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

namespace mdr
{
    public class DClass : DObject
    {
        public static DObject GlobalObjectInstance;

        protected DClass(DType type) : base(type) { }
        public DClass() : this(DType.ClassType) { }

        public override DObject ToDClass() { return this; }

        public override void CopyTo(DVar v) { v.Object = this; }

        public DVar[] Fields = new DVar[12]; //This is explicityly left unprotected to reduce access overhead

        public DType SetType(DType type)
        {
            if (!DType.IsParentOf(type))
                throw new Exception(string.Format("Type {0} is not a parent of tyoe {1} and so cannot be extended", DType, type));
            DType = type;
            var maxFieldIndex = DType.FieldIndex;
            if (maxFieldIndex >= Fields.Length)
            {
                var newFields = new DVar[Fields.Length + 10];
                Array.Copy(Fields, newFields, Fields.Length);
                Fields = newFields;
            }
            return type;
        }

        public int GetOrAddFieldIndex(string fieldName)
        {
            int fieldIndex = DType.IndexOf(fieldName);
            if (fieldIndex == DType.InvalidIndex)
            {
                SetType(new DType(fieldName, DType));
                fieldIndex = DType.FieldIndex;
            }
            if (Fields[fieldIndex] == null)
                Fields[fieldIndex] = new DVar();
            return fieldIndex;
        }

        public DVar GetField(string fieldName)
        {
            int fieldIndex = DType.IndexOf(fieldName);
            if (fieldIndex != DType.InvalidIndex)
                return Fields[fieldIndex];
            else
                return null;
        }
        public int SetField(string fieldName, DVar dvar)
        {
            int index = GetOrAddFieldIndex(fieldName);
            Fields[index] = dvar;
            return index;
        }

        public void CopyTo(DObject obj)
        {
            if (!DType.IsParentOf(obj.DType))
                throw new Exception(string.Format("Type {0} is not a parent of type {1} and so cannot be extended", DType, obj.DType));
            Array.Copy(Fields, obj.Fields, Fields.Length);
        }

        #region Dictionary
        //Dictionary<string, DVar> _others;
        public override DVar Get(string index)
        {
            return GetField(index);

            //if (_others == null)
            //    _others = new Dictionary<string, DVar>();
            //DVar dvar;
            //_others.TryGetValue(index, out dvar);
            //return dvar;

            //return _others[index];
        }
        //public void Set(string index, object value)
        //{

        //    if (_others == null)
        //        _others = new Dictionary<string, DVar>();
        //    _others[index] = value;
        //}
        //protected int Count { get { return (_others != null) ? _others.Count : 0; } }
        #endregion

        [System.Diagnostics.DebuggerStepThrough]
        public override void Accept(IMdrVisitor visitor)
        {
            visitor.Visit(this);
        }

    }
}
