// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿
namespace mdr
{
    /// <summary>
    /// This class is the base all classes that have internal arrays in addition to the property hash of DObject
    /// </summary>
    public class DArrayBase : DObject
    {
        protected DArrayBase(PropertyMap map) : base(map) { }
        protected DArrayBase(DObject prototype) : base(prototype) { }

        protected static readonly PropertyDescriptor UndefinedItemAccessor = new PropertyDescriptor(null)
        {
            Getter = (mdr.PropertyDescriptor pd, mdr.DObject obj, ref mdr.DValue value) =>
            {
                value.Set(Runtime.Instance.DefaultDUndefined);
            },
            Setter = (mdr.PropertyDescriptor pd, mdr.DObject obj, ref mdr.DValue value) =>
            {
                value.Set(Runtime.Instance.DefaultDUndefined);
                //throw new IndexOutOfRangeException();
            },
        };

        #region GetPropertyDescriptor
        public override PropertyDescriptor GetPropertyDescriptor(string field)
        {
            int i;
            if (int.TryParse(field, out i) && i >=0)
                return GetPropertyDescriptor(i);
            else
                return base.GetPropertyDescriptor(field);
        }
        public override PropertyDescriptor GetPropertyDescriptor(double field)
        {
            var intField = (int)field;
            if (field == intField)
                return GetPropertyDescriptor(intField);
            else
                return base.GetPropertyDescriptor(field);
        }
        #endregion

        #region AddPropertyDescriptor
        public override PropertyDescriptor AddPropertyDescriptor(string field)
        {
            int i;
            if (int.TryParse(field, out i) && i >=0)
                return AddPropertyDescriptor(i);
            else
                return base.AddPropertyDescriptor(field);
        }
        public override PropertyDescriptor AddPropertyDescriptor(double field)
        {
            var intField = (int)field;
            if (field == intField)
                return AddPropertyDescriptor(intField);
            else
                return base.AddPropertyDescriptor(field);
        }
        #endregion

        #region DeletePropertyDescriptor
        public override PropertyMap.DeleteStatus DeletePropertyDescriptor(string field)
        {
            int i;
            if (int.TryParse(field, out i))
                return DeletePropertyDescriptor(i);
            else
                return base.DeletePropertyDescriptor(field);
        }
        public override PropertyMap.DeleteStatus DeletePropertyDescriptor(double field)
        {
            var intField = (int)field;
            if (field == intField)
                return DeletePropertyDescriptor(intField);
            else
                return base.DeletePropertyDescriptor(field);
        }
        #endregion

    }
}
