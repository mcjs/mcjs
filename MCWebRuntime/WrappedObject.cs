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
using System.Runtime.CompilerServices;

using m.Util.Diagnose;

namespace mwr.DOM
{

    /// <summary>
    /// This is a generic wrapper for all DOM/CSS Node types. It stores a DOM node pointer from the 
    /// unmanaged world and has a prototype that has an entry for every method of the DOM node.
    /// </summary>
    public partial class WrappedObject : mdr.DObject
    {

        public IntPtr Domptr { get; set; }
        public WrappedObject Parent { get; set; }
        protected HashSet<WrappedObject> Children;

        /// <summary>
        /// Constructs a wrapper in the managed world for a DOM/CSS node residing in the unmanaged world
        /// </summary>
        /// <param name="domptr">pointer to a DOM/CSS object in the unmanaged world</param>
        public WrappedObject(IntPtr objPtr)
            : base(mdr.Runtime.Instance.EmptyPropertyMapMetadata.Root) 
        {
            Map = HTMLRuntime.Instance.GetPropertyMapOfWrappedObjectType(this.GetType());
            Domptr = objPtr;
            Parent = null;
            Children = new HashSet<WrappedObject>();
#if ENABLE_RR
            if (mwr.RecordReplayManager.Instance != null && mwr.RecordReplayManager.Instance.RecordEnabled)
            {
                RecordReplayManager.Instance.Record("WrappedObject", null, "ctor", false, objPtr.ToInt64(), GetType().ToString());

            }
#endif
        }

        // This will be filled in by AutoInternal.cs.
        public partial class Internal { }

        // This will be filled in by AutoBindings.cs.
        public partial class Bindings { }

        // Empty; this is just here so that subclasses which shadow this method can use consistent syntax (ie, they always
        // use the new keyword) which makes code generation easier.
        public static void PreparePrototype(mdr.DObject prototype)
        {
        }

        /// <summary>
        /// Attaching and dettaching nodes into the WrappedObject tree. 
        /// This tree reflects the parent/child relation between the DOM node in the C++ side and is used for communication between the two side
        /// </summary>
        public void SetParent(WrappedObject newParent)
        {
            // If there hasn't been a change, do nothing
            if (newParent == Parent)
              return;

            // Remove self from children of existing parent, if any
            if (Parent != null)
              Parent.Children.Remove(this);

            // Record new parent (we are detaching ourselves if new parent is null)
            Parent = newParent;

            // If new parent is non-null, update its child list
            if (Parent != null)
              Parent.Children.Add(this);
#if ENABLE_RR
            if (mwr.RecordReplayManager.Instance != null && mwr.RecordReplayManager.Instance.RecordEnabled)
            {
                RecordReplayManager.Instance.Record("WrappedObject", null, "SetParent", false, this, newParent);

            }
#endif
        }

        ~WrappedObject()
        {
            Debug.WriteLine("******** DOM Wrapper was collected {0} *********", this.Domptr.ToString("x"));
            Bindings.gcDestroy(this.Domptr);
        }

    }
}
