// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System.Collections.Generic;

using m.Util.Diagnose;

namespace mdr
{
#if !GENERIC_SIGNATURE
  public struct DFunctionSignature
  {
    const byte TotalBits = sizeof(ulong) * 8;
    const byte BitsPerType = 5; //This is very sensitive to values of ValueTypes enum
    const byte TypeMask = (1 << BitsPerType) - 1;
    public const byte TypesPerElement = TotalBits / BitsPerType;

    public static DFunctionSignature EmptySignature;
    static DFunctionSignature()
    {
      EmptySignature = new DFunctionSignature();
      EmptySignature.Value = 0; //Set all unused bits to ValueTypes.Undefined
      Debug.Assert((int)ValueTypes.Undefined == 0x0, "Invalid value assigned to Undefined value type");

      //EmptySignature.Value = ulong.MaxValue; //Set all unused bits to ValueTypes.Unknown
      //Debug.Assert((int)ValueTypes.Unknown == 0xF, "Invalid value assigned to Unknown value type");
    }

    /// <summary>
    /// Generates a mask to clip the extra passed parameters to a function
    /// </summary>
    public static ulong GetMask(int ParametersCount)
    {
      if (ParametersCount > TypesPerElement)
      {
        ///In this case we return the empty signature which will cause the runtime to use the generic version of the function
        return EmptySignature.Value;
        //throw new System.ArgumentOutOfRangeException("Too many arguments in the function signature!");
      }
      //return (EmptySignature.Value << (ParametersCount * BitsPerType));
      return ~(ulong.MaxValue << ((ParametersCount * BitsPerType))); //in the form of 0x0..0F..F
    }

    /// <summary>
    /// Generates a mask to best represent parts of the signature to be compared
    /// </summary>
    public static ulong GetMask(ref DFunctionSignature signature)
    {
      var value = signature.Value;
      ulong mask = 0;
      //ulong argTypeMask = TypeMask; //(int)ValueTypes.Any;
      //while ((value & mask) != value)
      //{
      //  if ((value & argTypeMask) != 0)
      //    mask |= argTypeMask;
      //  argTypeMask <<= BitsPerType;
      //}

      var argOffset = 0;
      while (value != 0)
      {
        var argType = value & TypeMask;
        if (argType != 0)
          ///TODO: we used to have the following code so that higher types can capture lowe types. (e.g. Object captures also Array). However
          ///this causes problem in the code cache and we should make sure we don't add repetitive code to it. 

          //if (argType == (ulong)ValueTypes.Object) 
          //  mask |= ((ulong)ValueTypes.Object << argOffset);
          //else
            mask |= ((ulong)TypeMask << argOffset);
        argOffset += BitsPerType;
        value >>= BitsPerType;
      }
      
      return mask;
    }
    //public static DFunctionSignature GetMask(int ParametersCount)
    //{
    //    var mask = new DFunctionSignature();
    //    mask.Value = GetMaskSignature(ParametersCount);
    //    return mask;
    //}

    public ulong Value;

    //public DFunctionSignature(int maxArgsCount)
    //{
    //    Value = GetMask(maxArgsCount);
    //}
    public DFunctionSignature(ref CallFrame callFrame, int maxArgsCount)
    {
      Value = EmptySignature.Value;
      if (callFrame.PassedArgsCount == 0)
        return;

      var argsCount = callFrame.PassedArgsCount;
      if (argsCount > maxArgsCount)
        argsCount = maxArgsCount;
      if (argsCount > TypesPerElement)
      {
        return;
        //throw new System.ArgumentOutOfRangeException("Too many arguments in the function signature!");
      }
      for (var i = argsCount - 1; i >= 0; --i)
        InitArgType(i, callFrame.Arg(i).ValueType);
      //var tmp = Value;
      //for (var i = argsCount - 1; i >= 0; --i)
      //    tmp = (tmp << BitsPerType) | (unchecked((uint)callFrame.Arg(i).ValueType) & TypeMask);
      ////for (var i = argsCount; i < TypesPerElement; ++i)
      ////    tmp = (tmp << TypeBits) | (unchecked((uint)ValueTypes.Unknown) & 0x0F);
      //Value = tmp;
    }
    public DFunctionSignature(params ValueTypes[] types)
    {
      Value = EmptySignature.Value;
      if (types == null || types.Length == 0)
      {
        return;
      }
      var argsCount = types.Length;
      if (argsCount > TypesPerElement)
      {
        return;
        //Trace.Fail(new System.ArgumentOutOfRangeException("Too many arguments in the function signature!"));
      }
      for (var i = argsCount - 1; i >= 0; --i)
        InitArgType(i, types[i]);

      //var tmp = Value;
      //for (var i = argsCount - 1; i >= 0; --i)
      //    tmp = (tmp << BitsPerType) | (unchecked((uint)types[i]) & TypeMask);
      ////for (var i = argsCount; i < TypesPerElement; ++i)
      ////    tmp = (tmp << TypeBits) | (unchecked((uint)ValueTypes.Unknown) & 0x0F);
      //Value = tmp;
    }
    public DFunctionSignature(DValue[] arguments, int maxArgsCount)
    {
      Value = EmptySignature.Value;
      if (arguments == null || arguments.Length == 0)
        return;

      var argsCount = arguments.Length;
      if (argsCount > maxArgsCount)
        argsCount = maxArgsCount;
      if (argsCount > TypesPerElement)
      {
        return;
        //Trace.Fail(new System.ArgumentOutOfRangeException("Too many arguments in the function signature!"));
      }
      for (var i = argsCount - 1; i >= 0; --i)
        InitArgType(i, arguments[i].ValueType);
      //var tmp = Value;
      //for (var i = argsCount - 1; i >= 0; --i)
      //    tmp = (tmp << BitsPerType) | (unchecked((uint)arguments[i].ValueType) & TypeMask);
      ////for (var i = argsCount; i < TypesPerElement; ++i)
      ////    tmp = (tmp << TypeBits) | (unchecked((uint)ValueTypes.Unknown) & 0x0F);
      //Value = tmp;
    }

    /// <summary>
    /// We assume that this function is called to set the values for the first time
    /// Therefore Value is 0 at the corresponding location
    /// This is not for overwriting the current value!
    /// </summary>
    /// <param name="argIndex"></param>
    /// <param name="argType"></param>
    public void InitArgType(int argIndex, ValueTypes argType)
    {
      if (argIndex >= TypesPerElement || argType >= ValueTypes.DValue)
      {
        return;
        //throw new System.ArgumentOutOfRangeException("Too many arguments in the function signature!");
      }
      Value |= (((ulong)argType & TypeMask) << (argIndex * BitsPerType));
    }

    public ValueTypes GetArgType(int argIndex)
    {
      if (argIndex > TypesPerElement)
      {
        return ValueTypes.Undefined;
        //return ValueTypes.Unknown;
        //throw new System.ArgumentOutOfRangeException("Too many arguments in the function signature!");
      }

      var type = (ValueTypes)((Value >> (argIndex * BitsPerType)) & TypeMask);
      return type;
    }

    /// <summary>
    /// Finds the total number of arg types that are not undefined
    /// </summary>
    public int GetKnownArgTypesCount()
    {
      var count = 0;
      var value = Value;
      while (value != 0)
      {
        if ((value & TypeMask) != 0)
          ++count;
        value >>= BitsPerType;
      }
      return count;
    }
    /// <summary>
    /// Finds the index of last arg type that is not undefined
    /// </summary>
    /// <returns></returns>
    public int GetLastKnownArgTypeIndex()
    {
      //Basically we should find last non-zero arg type.
      var index = -1;
      var value = Value;
      while (value != 0)
      {
        ++index;
        value >>= BitsPerType;
      }
      return index;
    }

    public ValueTypes[] Types
    {
      get
      {
        var types = new List<ValueTypes>();
        var tmp = Value;
        while (tmp != 0)
        {
          var type = (ValueTypes)(tmp & TypeMask);
          //if (type == ValueTypes.Unknown)
          //    break;
          types.Add(type);
          tmp >>= BitsPerType;
        }
        return types.ToArray();
      }
    }

    public override string ToString()
    {
      return string.Format("0x{0:X}-->{1}", Value, string.Join(",", Types));
    }
    //public bool Equals(ref DFunctionSignature other)
    //{
    //    return Value == other.Value;
    //}
    //public bool Match(ref DFunctionSignature other, ulong mask)
    //{
    //    return Value == (other.Value | mask);
    //}
  }
#else
    public struct DFunctionSignature
    {
        const byte TotalBits = sizeof(ulong) * 8;
        const byte TypeBits = 4; //This is very sensitive to values of ValueTypes enum
        const byte TypeMask = (1 << TypeBits) - 1;
        const byte TypesPerElement = TotalBits / TypeBits;

        int _argsCount;
        ulong _signature;
        ulong[] _signatureTail;

        void CalculateSignature(DValue[] arguments, int maxArgsCount)
        {
            ulong tmp = 0;
            var argsCount = arguments.Length;
            if (argsCount > maxArgsCount)
                argsCount = maxArgsCount;
            _argsCount = maxArgsCount;
            var args = arguments;
            if (argsCount <= TypesPerElement)
            {
                _signatureTail = null;
                tmp = 0;
                for (var i = 0; i < argsCount; ++i)
                    tmp = (tmp << TypeBits) | (unchecked((uint)args[i].ValueType) & TypeMask);
                //for (var i = argsCount; i < TypesPerElement; ++i)
                //    tmp = (tmp << TypeBits) | (unchecked((uint)ValueTypes.Unknown) & 0x0F);
                _signature = tmp;

            }
            else
            {
                var i = 0;
                for (; i < TypesPerElement; ++i)
                    tmp = (tmp << TypeBits) | (unchecked((uint)args[i].ValueType) & TypeMask);
                _signature = tmp;

                _signatureTail = new ulong[argsCount / TypesPerElement];
                for (var j = 0; j < _signatureTail.Length - 1; ++j)
                {
                    tmp = 0;
                    var maxIndex = (j + 2) * TypesPerElement;
                    for (; i < maxIndex; ++i)
                        tmp = (tmp << TypeBits) | (unchecked((uint)args[i].ValueType) & TypeMask);
                    _signatureTail[j] = tmp;
                }
                //for (var j = _signatureTail.Length - 1; j < _signatureTail.Length; ++j)
                {
                    var j = _signatureTail.Length;
                    tmp = 0;
                    var maxIndex = (j + 2) * TypesPerElement;
                    for (; i < argsCount; ++i)
                        tmp = (tmp << TypeBits) | (unchecked((uint)args[i].ValueType) & TypeMask);
                    //for (; i < maxIndex; ++i)
                    //    tmp = (tmp << TypeBits) | (unchecked((uint)ValueTypes.Unknown) & 0x0F);
                    _signatureTail[j] = tmp;
                }
            }
        }

        //public DFunctionSignature(DFunction func)
        //{
        //    _argsCount = 0;
        //    _signature = 0;
        //    _signatureTail = null;
        //    if (func == null || func.Arguments == null)
        //        return;
        //    CalculateSignature(func.Arguments);
        //}
        public DFunctionSignature(DValue[] arguments, int maxArgsCount)
        {
            _argsCount = 0;
            _signature = 0;
            _signatureTail = null;
            if (arguments == null)
                return;
            CalculateSignature(arguments, maxArgsCount);
        }
        public DFunctionSignature(params ValueTypes[] types)
        {
            _argsCount = 0;
            var tmp = _signature = 0;
            if (types == null || types.Length == 0)
            {
                _signatureTail = null;
                return;
            }

            var argsCount = _argsCount = types.Length;
            if (argsCount <= TypesPerElement)
            {
                _signatureTail = null;
                for (var i = 0; i < argsCount; ++i)
                    tmp = (tmp << TypeBits) | (unchecked((uint)types[i]) & TypeMask);
                //for (var i = argsCount; i < TypesPerElement; ++i)
                //    tmp = (tmp << TypeBits) | (unchecked((uint)ValueTypes.Unknown) & 0x0F);
                _signature = tmp;
            }
            else
            {
                var i = 0;
                for (; i < TypesPerElement; ++i)
                    tmp = (tmp << TypeBits) | (unchecked((uint)types[i]) & TypeMask);
                _signature = tmp;

                _signatureTail = new ulong[argsCount / TypesPerElement];
                for (var j = 0; j < _signatureTail.Length - 1; ++j)
                {
                    tmp = 0;
                    var maxIndex = (j + 2) * TypesPerElement;
                    for (; i < maxIndex; ++i)
                        tmp = (tmp << TypeBits) | (unchecked((uint)types[i]) & TypeMask);
                    _signatureTail[j] = tmp;
                }
                //for (var j = _signatureTail.Length - 1; j < _signatureTail.Length; ++j)
                {
                    var j = _signatureTail.Length;
                    tmp = 0;
                    var maxIndex = (j + 2) * TypesPerElement;
                    for (; i < argsCount; ++i)
                        tmp = (tmp << TypeBits) | (unchecked((uint)types[i]) & TypeMask);
                    //for (; i < maxIndex; ++i)
                    //    tmp = (tmp << TypeBits) | (unchecked((uint)ValueTypes.Unknown) & 0x0F);
                    _signatureTail[j] = tmp;
                }
            }
        }

        ValueTypes GetType(int argIndex)
        {
            if (argIndex >= _argsCount)
                return ValueTypes.Undefined;
            ulong signatureChunk;
            if (argIndex < TypesPerElement)
                signatureChunk = _signature;
            else
            {
                signatureChunk = _signatureTail[argIndex >> TypeBits - 1];
                argIndex &= TypeMask;
            }
            var type = (ValueTypes)((signatureChunk >> (argIndex * TypeBits)) & TypeMask);
            return type;
        }
        void SetType(int argIndex, ValueTypes argType)
        {
            if (argIndex >= _argsCount)
                throw new System.ArgumentOutOfRangeException("Signature index out of range");
            if (argIndex < TypesPerElement)
            {
                var totalShift = argIndex * TypeBits;
                var newSignature = (_signature & ~((ulong)TypeMask << totalShift)) | (((ulong)argType & TypeMask) << totalShift);
                _signature = newSignature;
            }
            else
            {
                var elemIndex = argIndex >> TypeBits - 1;
                var newSignature = _signatureTail[elemIndex];
                var totalShift = (argIndex & TypeMask) * TypeBits;
                newSignature = (newSignature & ~((ulong)TypeMask << totalShift)) | (((ulong)argType & TypeMask) << totalShift);
                _signatureTail[elemIndex] = newSignature;
            }

        }

        public ValueTypes[] Types
        {
            get
            {
                //var typesCount = (_argsCount) * TypesPerElement;
                var types = new ValueTypes[_argsCount];
                var index = 0;
                ExtractTypes(types, ref index, _signature);
                if (_signatureTail != null)
                    for (var i = 0; i < _signatureTail.Length; ++i)
                        ExtractTypes(types, ref index, _signatureTail[i]);

                return types;
            }
        }
        private void ExtractTypes(ValueTypes[] destination, ref int index, ulong signature)
        {
            for (var i = 0; index < _argsCount && i < TypesPerElement; ++i, ++index)
            {
                var type = (ValueTypes)(signature & TypeMask);
                if (type == ValueTypes.Unknown)
                    break;
                destination[_argsCount - index - 1] = type;
                signature >>= TypeBits;
            }
        }

        public bool Equals(ref DFunctionSignature other)
        {
            if (_argsCount != other._argsCount)
                return false;
            if (_signature != other._signature)
                return false;
            if (_signatureTail != null)
            {
                if (other._signatureTail == null)
                    return false;
                if (_signatureTail.Length != other._signatureTail.Length)
                    return false;
                var tailCount = _signatureTail.Length;
                for (var i = 0; i < tailCount; ++i)
                    if (_signatureTail[i] != other._signatureTail[i])
                        return false;
            }
            else if (other._signatureTail != null)
                return false;
            return true;
        }

    }
#endif
}
