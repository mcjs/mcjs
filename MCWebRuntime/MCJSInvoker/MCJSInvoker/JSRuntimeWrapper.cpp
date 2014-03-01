#include <iostream>
#include <assert.h>
#include <stdlib.h>
#include "DOMObject.h"
#include "JSRuntimeWrapper.h"
#include "AutoDOMBind.h"

using namespace std;

namespace JSBinding
{
bool JSRuntimeWrapper::isInternalCallsInitialized = false;

JSRuntimeWrapper::JSRuntimeWrapper(int argc, char** argv)
{
	if (argc == 0)
		FatalError("JSRuntimeWrapper requires the path of JS Engine");
	
	MonoInstance::Init(argv[0]); //initialize a mono instance
	_domain = MonoInstance::Instance()->GetDomain();
	InitInternalCalls();
	FindJSMethods();
	Init(argc, argv);
}

JSRuntimeWrapper::~JSRuntimeWrapper()
{
}

void JSRuntimeWrapper::FindJSMethods()
{
	MonoInstance* monoinst = MonoInstance::Instance();
	_initMethod = monoinst->FindJSMethod("MCJavascript.JSRuntime:Init(string[])");
	_runFileMethod = monoinst->FindJSMethod("MCJavascript.JSRuntime:RunScriptFile(string)");
	_runStringMethod = monoinst->FindJSMethod("MCJavascript.JSRuntime:RunScriptString(string)");
}

void JSRuntimeWrapper::Init(int argc, char** argv) const
{
	assert(_initMethod != NULL);
	void* args[1];	
	MonoArray* params = (MonoArray*)mono_array_new(_domain, mono_get_string_class(), argc);	
	for (int i = 0; i < argc; i++)
		mono_array_set(params, MonoString*, i, mono_string_new (_domain, argv[i]));
		
	args[0] = params;
	mono_runtime_invoke(_initMethod, NULL, args, NULL);
}

void JSRuntimeWrapper::RunScriptFile(const char* filename) const
{
	void* args[1];
	args[0] = mono_string_new(_domain, filename);
	mono_runtime_invoke(_runFileMethod, NULL, args, NULL);
}

void JSRuntimeWrapper::RunScriptString(const char* string) const
{
	void* args[1];
	args[0] = mono_string_new(_domain, string);
	mono_runtime_invoke(_runStringMethod, NULL, args, NULL);
}

void JSRuntimeWrapper::InitInternalCalls()
{
	if (isInternalCallsInitialized)
		return;

#include "AutoMonoInternal.inc"
	mono_add_internal_call ("DOMBinding.DOMBinder::GetDOMRoot", (const void*)CreateDomObject);	
	mono_add_internal_call ("DOMBinding.SampleDOMNode::GetElementByName", (const void*)DOMObject_GetElementByName);
	mono_add_internal_call ("DOMBinding.SampleDOMNode::GetName", (const void*)DOMObject_GetName);
	mono_add_internal_call ("DOMBinding.SampleDOMNode::GetNum", (const void*)DOMObject_GetNum);
	mono_add_internal_call ("DOMBinding.SampleDOMNode::SetNum", (const void*)DOMObject_SetNum);
	
	isInternalCallsInitialized = true;
}
}
