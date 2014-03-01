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

namespace mdr
{

    public class PropertyMap
    {
        /// <summary>
        /// A unique id integer indentifying this property map (used by inline caches)
        /// </summary>
        public readonly int UniqueId;
        static private int _uniqueIdCounter = 0;

        /// <summary>
        /// Holds shared information, as well as list of inherited properties
        /// </summary>
        public readonly PropertyMapMetadata Metadata;

        /// <summary>
        /// The link up to "previous" properties in the type
        /// </summary>
        public readonly PropertyMap Parent;

        /// <summary>
        /// DTypes that extend this DType with a specific property
        /// </summary>
        Dictionary<int, PropertyMap> _children;

        /// <summary>
        /// Property information. This is the last property of the DObjects who point to this DType
        /// These are owned properties. Inherited ones are stored in the Metadata and shared by all DTypes. 
        /// </summary>
        public readonly PropertyDescriptor Property;
        //PropertyDescriptor[] Fields;

        /// <summary>
        /// If a similar propery in the prototype chain exists, we will point to it here
        /// </summary>
        public readonly PropertyDescriptor OverriddenPropery;

        ///// <summary>
        ///// List of all DTypes whose prorotype is using this DType!
        ///// </summary>
        //LinkedList<WeakReference> _subTypes;

        ///// <summary>
        ///// Number of actual fields (as opposed to inherited fields that might be added)
        ///// if (DObject.DType==this) DObject.Fields.Length >= this.OwnFiledsCount;
        ///// </summary>
        //public int OwnFieldsCount { get; private set; }

        internal PropertyMap(PropertyMapMetadata metadata, PropertyMap parent, PropertyDescriptor property, PropertyDescriptor overriddenProperty)
        {
            Metadata = metadata;
            Parent = parent;
            Property = property;
            OverriddenPropery = overriddenProperty;
            UniqueId = _uniqueIdCounter++;

            Debug.Assert(Metadata != null, "PropertyMap.Metadata cannot be null");
            Debug.Assert(Property != null, "PropertyMap.Property cannot be null");
        }

        /// <summary>
        /// This method is public for the compiler to generate faster code by directly manipulating the map when needed
        /// </summary>
        public PropertyMap AddOwnProperty(string field, int fieldId, PropertyDescriptor.Attributes attributes)
        {
          Debug.Assert(fieldId != Runtime.InvalidFieldId, "Invalid situation! FieldId of {0} is not assigned", field);
          Debug.Assert(GetOwnPropertyDescriptorByFieldId(fieldId) == null, "Invalid situation! Field {0} alread exists in the map", field);

            PropertyMap newMap = null;
            if (_children == null)
                _children = new Dictionary<int, PropertyMap>();
            int key = ((int)attributes << 16) | (fieldId & 0xFFFF);
            if (!_children.TryGetValue(key, out newMap))
            {
                var overridenProperty = Metadata.GetInheritedPropertyDescriptorByFieldId(fieldId);
                newMap = new PropertyMap(Metadata, this, new PropertyDescriptor(field, fieldId, this.Property.Index + 1, attributes), overridenProperty);
                _children[key] = newMap;
            }
            return newMap;
        }
        internal PropertyDescriptor AddOwnProperty(DObject obj, string field, int fieldId, PropertyDescriptor.Attributes attributes)
        {
            PropertyMap newMap = AddOwnProperty(field, fieldId, attributes);
            obj.Map = newMap;

            var mapMetadata = Metadata.GetMapMetadataOfPrototype(obj, false);
            if (mapMetadata != null)
                mapMetadata.PropagateAdditionDownPrototypeChain(obj, newMap.Property); //obj is some other objects' prototype

            return newMap.Property;
        }

        public PropertyDescriptor GetOwnPropertyDescriptor(string field)
        {
            for (var m = this; m != null; m = m.Parent)
                if (m.Property.Name == field)
                    return m.Property;
            return null;
        }

        /// <summary>
        /// It is better to user the GetPropertyDescriptorByFieldId since that one also uses a cache
        /// </summary>
        private PropertyDescriptor GetOwnPropertyDescriptorByFieldId(int fieldId)
        {
            for (var m = this; m != null; m = m.Parent)
                if (m.Property.NameId == fieldId)
                    return m.Property;
            return null;
        }

        public PropertyDescriptor GetPropertyDescriptor(string field)
        {
            //var propDesc = GetCachedPropertyDescriptor(obj, field, false);
            var propDesc = GetOwnPropertyDescriptor(field);
            if (propDesc == null)
                propDesc = Metadata.GetInheritedPropertyDescriptor(field);
            return propDesc;
        }
        public PropertyDescriptor GetPropertyDescriptorByFieldId(int fieldId)
        {
            PropertyDescriptor propDesc;
#if !ENABLE_CACHE
//#if __STAT__PD 
            if (Runtime.Instance.Configuration.ProfileStats)
            {
              Runtime.Instance.Counters.GetCounter("PMCache accesses").Count++;
            } 
//#endif
            propDesc = Metadata.Cache.Get(fieldId, this);
            if (propDesc == null)
#endif
            {
                propDesc = GetOwnPropertyDescriptorByFieldId(fieldId);
                if (propDesc == null)
                    propDesc = Metadata.GetInheritedPropertyDescriptorByFieldId(fieldId);
#if !ENABLE_CACHE
                if (propDesc != null)
                    Metadata.Cache.Add(propDesc, this);
#endif
            }

#if __STAT__PD
            if (Runtime.Instance.Configuration.ProfileStats)
            {
              if (propDesc != null)
              {
                Runtime.Instance.Counters.GetCounter("PMCache hits").Count++;
                if (propDesc.IsInherited)
                  Runtime.Instance.Counters.GetCounter("PMCache inherited hits").Count++;
              }
            }
#endif
            return propDesc;
        }

        ///For the followings we need to call the actual virtual ones on the objects.
        //public bool HasOwnProperty(string field)
        //{
        //    var pd = GetOwnPropertyDescriptor(field);
        //    return pd != null;
        //}
        //public bool HasProperty(string field)
        //{
        //    var pd = GetPropertyDescriptor(field);
        //    return pd != null && !pd.IsUndefined;
        //}

        public enum DeleteStatus
        {
          NotFound,
          NotDeletable,
          Deleted
        }
        private PropertyMap DeleteOwnPropertyDescriptor(DObject obj, PropertyMap currMap, PropertyMap delMap)
        {
            Debug.Assert(delMap != null, "Invalid situation!");
            ///In this function, we recursively add all the properties between the delMap, and this to the delMap.Parent
            ///currMap==this would be the end of recursion
            ///at the end we need to propagate the deletion to all objects whose prototype is obj

            var newMap = (delMap == currMap.Parent || delMap == currMap) ? delMap.Parent : DeleteOwnPropertyDescriptor(obj, currMap.Parent, delMap);
            if (delMap != this)
            {
                var currPropDesc = currMap.Property;
                newMap = newMap.AddOwnProperty(currPropDesc.Name, currPropDesc.NameId, currPropDesc.GetAttributes());
            }
            if (currMap == this)
            {
                Debug.Assert(this.Property.Index == newMap.Property.Index + 1, "Invalid situation, we should delete on one field here!");
                obj.Map = newMap;
                if (delMap != this) //otherwise it is just the last field so no need to copy
                    Array.Copy(obj.Fields, delMap.Property.Index + 1, obj.Fields, delMap.Property.Index, this.Property.Index - delMap.Property.Index);

                var mapMetadata = Metadata.GetMapMetadataOfPrototype(obj, false);
                if (mapMetadata != null)
                    mapMetadata.PropagateDeletionDownPrototypeChain(obj, delMap.Property);
            }
            return newMap;
        }
        public DeleteStatus DeleteOwnPropertyDescriptorByFieldId(DObject obj, int fieldId)
        {
            Debug.Assert(obj.Map == this, "Invalid situation! this is the map of current object");
            Debug.Assert(fieldId != Runtime.InvalidFieldId, "Cannot remove null field from object");

            for (var m = this; m != null; m = m.Parent)
              if (m.Property.NameId == fieldId)
              {
                if (m.Property.IsNotConfigurable)
                  return DeleteStatus.NotDeletable;
                else
                {
                  DeleteOwnPropertyDescriptor(obj, this, m);
                  return DeleteStatus.Deleted;
                }
              }
            return DeleteStatus.NotFound;
        }
        public DeleteStatus DeleteOwnPropertyDescriptor(DObject obj, string field)
        {
            Debug.Assert(obj.Map == this, "Invalid situation! this is the map of current object");
            Debug.Assert(field != null, "Cannot remove null field from object");

            for (var m = this; m != null; m = m.Parent)
                if (m.Property.Name == field)
                {
                  if (m.Property.IsNotConfigurable)
                    return DeleteStatus.NotDeletable;
                  else
                  {
                    DeleteOwnPropertyDescriptor(obj, this, m);
                    return DeleteStatus.Deleted;
                  }
                }
            return DeleteStatus.NotFound;
        }


        [System.Diagnostics.DebuggerStepThrough]
        public virtual void Accept(IMdrVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
