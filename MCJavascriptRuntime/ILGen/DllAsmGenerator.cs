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
using System.Reflection;
using System.Reflection.Emit;

namespace mjr.ILGen
{
    class DllAsmGenerator : BaseAsmGenerator 
    {
        protected string InputFilename { get; private set; }
        protected string OutputDir { get; private set; }
        protected string Filename { get; private set; }

        string assemblyName = "Temp";
        AssemblyBuilder assemblyBuilder;
        public ModuleBuilder ModuleBuilder { get; private set; }

        public DllAsmGenerator(string inputFilename, string outputDir)
            : base()
        {
            InputFilename = inputFilename;
            OutputDir = outputDir;
            Filename = System.IO.Path.GetFullPath(System.IO.Path.Combine(outputDir, System.IO.Path.GetFileName(inputFilename)));
        }

        public override void BeginAssembly()
        {
            //base.BeginAssembly();

            AppDomain myCurrentDomain = AppDomain.CurrentDomain;
            System.Reflection.AssemblyName myAssemblyName = new System.Reflection.AssemblyName();
            if (Filename != null)
            {
                assemblyName = System.IO.Path.GetFileName(Filename);
                var assemblyPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Filename));
                myAssemblyName.Name = assemblyName + ".Assembly";
                assemblyBuilder = myCurrentDomain.DefineDynamicAssembly(myAssemblyName, System.Reflection.Emit.AssemblyBuilderAccess.RunAndSave, assemblyPath);
            }
            else
            {
                myAssemblyName.Name = assemblyName + ".Assembly";
                // Define a dynamic assembly in the current application domain.
                assemblyBuilder = myCurrentDomain.DefineDynamicAssembly(myAssemblyName, System.Reflection.Emit.AssemblyBuilderAccess.RunAndSave);
            }

            // Define a dynamic module in this assembly.
            ModuleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName, assemblyName + ".dll");

            // Define a runtime class with specified name and attributes.
            //typeBuilder = myModuleBuilder.DefineType("TempClass", System.Reflection.TypeAttributes.Public);

            // Add 'Greeting' field to the class, with the specified attribute and type.
            //FieldBuilder^ greetingField = myTypeBuilder->DefineField( "Greeting", String::typeid, FieldAttributes::Public );

            //array<Type^>^myMethodArgs = {String::typeid};

            // Add 'MyMethod' method to the class, with the specified attribute and signature.
            //var myMethod = typeBuilder.DefineMethod("MyMethod", System.Reflection.MethodAttributes.Public, System.Reflection.CallingConventions.Standard, null, null/*myMethodArgs */);
            //myMethod = myModuleBuilder->DefineGlobalMethod( "MyMethod", MethodAttributes::Static | MethodAttributes::Public, CallingConventions::Standard, nullptr, nullptr );

        }
        public override void EndAssembly()
        {
            //base.EndAssembly();
            assemblyBuilder.Save(assemblyName + ".dll");
        }

        public override BaseILGenerator GetILGenerator()
        {
            return new DllILGenerator(this); 
        }
    }
}
