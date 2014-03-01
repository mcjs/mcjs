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
  /// <summary>
  /// Contains the information shared by all related DTypes in the type tree
  /// </summary>
  public class PropertyMapMetadata
  {
    public readonly DObject Prototype;

    /// <summary>
    /// depth in the property chain. 
    /// </summary>
    readonly int Level;
    /// <summary>
    /// Root of the type tree that contain field information of the objects
    /// </summary>
    public readonly PropertyMap Root;

    /// <summary>
    /// List of other DTypeMetadata whose prototype is using a DType from subtree of this.Root
    /// </summary>
    LinkedList<WeakReference> _children = new LinkedList<WeakReference>();

    LinkedList<PropertyDescriptor> _inheritedProperties = new LinkedList<PropertyDescriptor>();

    internal readonly PropertyCache Cache = new PropertyCache();

    string _name;
    public string Name
    {
      get
      {
        if (_name == null)
          return "Object";
        else
          return _name;
      }
      set
      {
        Debug.Assert(_name == null || _name == value, "Cannot change PropertyMapMetadata.Name from {0} to {1}", _name, value);
        _name = value;
      }
    }

    public PropertyMapMetadata(DObject prototype)
    {
      Prototype = prototype;
      if (Prototype == null)
        Level = 0;
      else
        Level = Prototype.Map.Metadata.Level + 1;

      //We create an empty ProperyDescriptor to avoid checking for null all the time
      Root = new PropertyMap(this, null, new PropertyDescriptor(null, Runtime.InvalidFieldId, Runtime.InvalidFieldIndex, PropertyDescriptor.Attributes.NotEnumerable | PropertyDescriptor.Attributes.Undefined), null);
    }

    public PropertyMapMetadata GetMapMetadataOfPrototype(DObject prototype, bool addIfMissing = true)
    {
      Debug.Assert(prototype != null, "cannot lookup type information for null prototype");

      WeakReference emptyNode = null; //sice we use weakref, we might want to reuse emty elements of the list

#if !SEARCH_CHILDREN_LIST //TODO: we can explore the effect of the following by commening the next few lines
      if (prototype.SubMapsMetadata != null)
        return prototype.SubMapsMetadata;

      var iter = _children.GetEnumerator();
      while (iter.MoveNext())
      {
        var childRef = iter.Current;
        if (!childRef.IsAlive)
        {
          emptyNode = childRef;
          break;
        }
      }
#else
      var iter = _children.GetEnumerator();
      while (iter.MoveNext())
      {
        var childRef = iter.Current;
        if (childRef.IsAlive)
        {
          var child = childRef.Target as PropertyMapMetadata;
          if (child.Prototype == prototype)
            return child;
        }
        else if (emptyNode == null)
          emptyNode = childRef;
      }
#endif
      ///Ok, we did not find any, let's add one to the list
      if (!addIfMissing)
        return null;

      var newRoot = new PropertyMapMetadata(prototype);
      if (emptyNode == null)
      {
        emptyNode = new WeakReference(newRoot);
        _children.AddLast(emptyNode);
      }
      else
        emptyNode.Target = newRoot;

      prototype.SubMapsMetadata = newRoot;

      return newRoot;
    }
    public PropertyMap GetRootMapOfPrototype(DObject prototype)
    {
      return GetMapMetadataOfPrototype(prototype).Root;
    }

    internal PropertyDescriptor AddInheritedProperty(string field, int fieldId, int index, PropertyDescriptor.Attributes attributes = PropertyDescriptor.Attributes.Undefined)
    {
      var propDesc = new PropertyDescriptor(field, fieldId, index, attributes);
      _inheritedProperties.AddFirst(propDesc);
      return propDesc;
    }
    PropertyDescriptor AddInheritedProperty(PropertyDescriptor propDesc)
    {
      if (propDesc != null)
      {
        var inheritedPD = AddInheritedProperty(propDesc.Name, propDesc.NameId, propDesc.Index, propDesc.GetAttributes() | PropertyDescriptor.Attributes.Inherited);
        if (propDesc.IsInherited)
          inheritedPD.Container = propDesc.Container;
        else
          inheritedPD.Container = Prototype;
        return inheritedPD;
      }
      return null;
    }
    internal PropertyDescriptor GetInheritedPropertyDescriptor(string field)
    {
      foreach (var p in _inheritedProperties)
        if (p.Name == field)
          return p;
      if (Prototype != null)
      {
        var propDesc = Prototype.Map.GetPropertyDescriptor(field);
        if (propDesc != null)
          return AddInheritedProperty(propDesc);
        //else
        //  return AddInheritedProperty(field, Runtime.Instance.GetFieldId(field), Runtime.InvalidFieldIndex, PropertyDescriptor.Attributes.Undefined);
      }
      return null;
    }

    internal PropertyDescriptor GetInheritedPropertyDescriptorByFieldId(int fieldId)
    {
      foreach (var p in _inheritedProperties)
        if (p.NameId == fieldId)
          return p;
      if (Prototype != null)
      {
        var propDesc = Prototype.Map.GetPropertyDescriptorByFieldId(fieldId);
        if (propDesc != null)
          return AddInheritedProperty(propDesc);
        //else
        //  return AddInheritedProperty(Runtime.Instance.GetFieldName(fieldId), fieldId, Runtime.InvalidFieldIndex, PropertyDescriptor.Attributes.Undefined);
      }
      return null;
    }

    internal void PropagateAdditionDownPrototypeChain(DObject obj, PropertyDescriptor propDesc)
    {
      //We should only find at most one matching element
      foreach (var p in _inheritedProperties)
      {
        if (p.NameId == propDesc.NameId)
        {
          if (p.IsUndefined)
          {
            p.Container = obj;
            p.Index = propDesc.Index;
            p.ResetAttributes(propDesc.GetAttributes() | PropertyDescriptor.Attributes.Inherited);
          }
          else
          {
            Debug.Assert(p.IsInherited, "{0} has invalid descriptor type {1}", p.Name, p.GetAttributes());
            if (p.Container.Map.Metadata.Level > obj.Map.Metadata.Level)
            {
              //this property is inherited from an object lower in the property chain. So no longer need to propogate
              return;
            }

            if (p.Container != obj && p.Container.Map.Metadata.Level < obj.Map.Metadata.Level)
            {
              p.Container = obj;
              p.Index = propDesc.Index;
              p.ResetAttributes(propDesc.GetAttributes() | PropertyDescriptor.Attributes.Inherited);
              if (p.ObjectCacheIndex != -1 && p.ObjectCacheIndex < Runtime._inheritPropertyObjectCache.Length)
              {
                Runtime._inheritPropertyObjectCache[p.ObjectCacheIndex] = null;
              }
            }
          }
        }
      }
      foreach (var child in _children)
        if (child.IsAlive)
          (child.Target as PropertyMapMetadata).PropagateAdditionDownPrototypeChain(obj, propDesc);
    }
    internal void PropagateDeletionDownPrototypeChain(DObject obj, PropertyDescriptor propDesc)
    {
      //We should only find at most one matching element
      foreach (var p in _inheritedProperties)
      {
        if (p.NameId == propDesc.NameId)
        {
          Debug.Assert(p.IsInherited, "{0} has invalid descriptor type {1}", p.Name, p.GetAttributes());
          if (p.Container.Map.Metadata.Level > obj.Map.Metadata.Level)
          {
            //this property is inherited from an object lower in the property chain. So no longer need to propogate
            return;
          }
          Debug.Assert(p.Container == obj, "Invalid situation!");
          //We check to see if the property exists in higher parts of protoype chain
          var upperPropDesc = obj.Map.Metadata.GetInheritedPropertyDescriptorByFieldId(propDesc.NameId);
          if (upperPropDesc == null || upperPropDesc.IsUndefined)
          {
            p.Container = null;
            p.Index = propDesc.Index;
            p.ResetAttributes(PropertyDescriptor.Attributes.Undefined);
          }
          else
          {
            if (p.Container != upperPropDesc.Container)
            {
              if (p.ObjectCacheIndex != -1 && p.ObjectCacheIndex < Runtime._inheritPropertyObjectCache.Length)
              {
                Runtime._inheritPropertyObjectCache[p.ObjectCacheIndex] = null;
              }
            }
            p.Container = upperPropDesc.Container;
            p.Index = upperPropDesc.Index;
            p.ResetAttributes(upperPropDesc.GetAttributes() | PropertyDescriptor.Attributes.Inherited);
          }
        }
      }
      foreach (var child in _children)
        if (child.IsAlive)
          (child.Target as PropertyMapMetadata).PropagateDeletionDownPrototypeChain(obj, propDesc);
    }
  }
}
