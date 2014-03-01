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

namespace mdr
{
  /// <summary>
  /// The following coding is used for the enums in this class
  ///     xxx00 for single value types
  ///     xx011 for different object types
  ///     x0111 for function
  ///     xxx10 & xxx01 for numbers
  /// </summary>
  public enum ValueTypes// : byte
  {
    // Undefined should be the first (default value) since all variables by default are undefined
    Undefined = /**/0x00, //0b0000-0000
    Null = /*     */0x18, //0b0001-1000
    Boolean = /*  */0x04, //0b0000-0100
    String = /*   */0x0C, //0b0000-1100

    Char = /*     */0x02, //0b0000-0010
    Float = /*    */0x06, //0b0000-0110
    Double = /*   */0x0E, //0b0000-1110
    // = /*       */0x00, //0b0000-1010
    // = /*       */0x00, //0b0001-xx10

    Int8 = /*     */0x01, //0b0000-0001
    Int16 = /*    */0x05, //0b0000-0101
    Int32 = /*    */0x09, //0b0000-1001
    Int64 = /*    */0x0D, //0b0000-1101
    UInt8 = /*    */0x11, //0b0001-0001
    UInt16 = /*   */0x15, //0b0001-0101
    UInt32 = /*   */0x19, //0b0001-1001
    UInt64 = /*   */0x1D, //0b0001-1101

    Object = /*   */0x03, //0b0000-0011
    Function = /* */0x0B, //0b0000-1011
    Array = /*    */0x13, //0b0001-0011
    Property = /* */0x1B, //0b0001-1011

    Any = /*      */0x1F, //0b0000-0111 //Used when we have a generic object value, and the producer,consumer know what to cast/unbox the value to

    //The followings are used only at compile time and potentially for type inference
    //At run time, all these are equivalent to Undefined
    DValue = /*   */0x20, //0b0010-0000
    DValueRef = /**/0x40, //0b0100-0000

    //The following must always have all bits set
    Known = /*    */0x1F, //0b0001-1111 //Used in the masks to match all known types other than DValue
    Unknown = /*  */0xE0, //0b1110-0000
  }
  public static class ValueTypesHelper
  {
    public static bool IsUndefined(ValueTypes type) { return type == ValueTypes.Undefined; }
    public static bool IsNull(ValueTypes type) { return type == ValueTypes.Null; }
    public static bool IsBoolean(ValueTypes type) { return type == ValueTypes.Boolean; }
    public static bool IsString(ValueTypes type) { return type == ValueTypes.String; }
    public static bool IsFunction(ValueTypes type) { return type == ValueTypes.Function; }

    public static bool IsNumber(ValueTypes type) { var typeBits = (int)type & 0x03; return (typeBits == 0x01) || (typeBits == 0x02); }
    public static bool IsObject(ValueTypes type) { var typeBits = (int)type & 0x03; return typeBits == 0x03; }
    public static bool IsPrimitive(ValueTypes type) { var typeBits = (int)type & 0x03; return typeBits != 0x03; }
    public static bool IsDefined(ValueTypes type) { var typeBits = (int)type & 0x07; return typeBits != 0; }

  }
}
