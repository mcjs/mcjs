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
using System.Linq;
using System.Text;

namespace mjr.Operations.Unary
{
    /// <summary>
    /// ECMA-262, 11.3.1
    /// ECMA-262, 11.4.4
    /// ECMA-262, 11.4.5
    /// </summary>
    public static class IncDec
    {
        public static void Run(ref mdr.DValue i0, int i1)
        {
            switch (i0.ValueType)
            {
                case mdr.ValueTypes.Double:
                    {
                        double oldValue = i0.DoubleValue;
                        double newValue = oldValue + i1;
                        i0.DoubleValue = newValue;
                        //i0.Set(newValue);
                        break;
                    }
                case mdr.ValueTypes.Int32:
                    {
                        int oldValue = i0.IntValue;
                        int newValue = oldValue + i1;
                        i0.IntValue = newValue;
                        //i0.Set(newValue);
                        break;
                    }
                case mdr.ValueTypes.Boolean:
                    {
                        int oldValue = i0.BooleanValue ? 1 : 0;
                        int newValue = oldValue + i1;
                        i0.Set(newValue);
                        break;
                    }
                default:
                    {
                        double oldValue = i0.AsDouble();
                        double newValue = oldValue + i1;
                        i0.Set(newValue);
                        break;
                    }
            }
        }
        //public static mdr.DValue IncDec(ref mdr.DValue i0, int i1)
        //{
        //    var result = new mdr.DValue();
        //    switch (i0.ValueType)
        //    {
        //        case mdr.ValueTypes.Int:
        //            {
        //                int oldValue = i0.IntValue;
        //                int newValue = oldValue + i1;
        //                result.Set(newValue);
        //                break;
        //            }
        //        case mdr.ValueTypes.Boolean:
        //            {
        //                int oldValue = i0.BoolValue ? 1 : 0;
        //                int newValue = oldValue + i1;
        //                result.Set(newValue);
        //                break;
        //            }
        //        default:
        //            {
        //                double oldValue = i0.ToDouble();
        //                double newValue = oldValue + i1;
        //                result.Set(newValue);
        //                break;
        //            }
        //    }
        //    return result;
        //}

        /// <summary>
        /// The following is used for inc/dec that involves DValue. To handle arrays and properties well, we will have a separate object for 
        /// reading the value, and another for setting. To make the inc/dec and assign, etc. uniform, we should consider that on the stack 
        /// we have all the paramertes always for all kinds of values (symbols, arrays, properties, ...)
        /// 
        /// followings:
        ///     dest for writing
        ///     DObject  for setting (for array/property will be a member of the object itself)
        ///     DObject  for reading (for array/property may be a member of the object's prototype)
        /// </summary>
        /// <param name="result">for returing the value that is used in the next instruction.</param>
        /// <param name="dest">For updating the source itself</param>
        /// <param name="i0">the source for reading the value</param>
        /// <param name="i1">1 for inc and -1 for dec</param>
        /// <param name="isPostfix"></param>
        /// <returns></returns>
        public static void AddConst(ref mdr.DValue dest, /*const*/ ref mdr.DValue i0, int i1, bool isPostfix, ref mdr.DValue result)
        {
            switch (i0.ValueType)
            {
                case mdr.ValueTypes.Int32:
                    {
                        int oldValue = i0.IntValue;
                        int newValue = oldValue + i1;
                        dest.Set(newValue);
                        result.Set(isPostfix ? oldValue : newValue);
                        break;
                    }
                case mdr.ValueTypes.Boolean:
                    {
                        int oldValue = i0.BooleanValue ? 1 : 0;
                        int newValue = oldValue + i1;
                        dest.Set(newValue);
                        result.Set(isPostfix ? oldValue : newValue);
                        break;
                    }
                default:
                    {
                        double oldValue = i0.AsDouble();
                        double newValue = oldValue + i1;
                        dest.Set(newValue);
                        result.Set(isPostfix ? oldValue : newValue);
                        break;
                    }
            }
        }
    }
}
