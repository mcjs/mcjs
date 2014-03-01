#include <assert.h>
#include <mono/metadata/assembly.h> 
#include "DOMBase.h"

namespace JSBinding
{

MonoInstance* DOMBase::_monoInstance;
MonoMethod* DOMBase::_makeWrapperMethod;
MonoMethod* DOMBase::_setNextWrapperMethod;
MonoMethod* DOMBase::_setPrevWrapperMethod;

DOMBase::DOMBase() : _JSWrapperWPtr(0)
{
	if (_monoInstance == NULL)
		_monoInstance = MonoInstance::Instance();
}

DOMBase::~DOMBase()
{
	if (_JSWrapperWPtr != 0)
		mono_gchandle_free(_JSWrapperWPtr);
}

MonoObject* DOMBase::GetJSWrapper() 
{	
	if (_JSWrapperWPtr != 0)
	{
		//printf ("%s:%d %s() Mono calling C code\n", __FILE__, __LINE__, __FUNCTION__);
		MonoObject* domwrapper = mono_gchandle_get_target(_JSWrapperWPtr);
		if (!domwrapper)
			FatalError("Cannot find the JS Wrapper object through the weak refrence");
		return domwrapper;
	}
	else
	{
		//printf ("%s:%d %s() Mono calling C code\n", __FILE__, __LINE__, __FUNCTION__);	
		MonoObject* wrapperObject = MakeWrapper(this, GetMakeWrapperIndex());
		_JSWrapperWPtr = mono_gchandle_new_weakref(wrapperObject, false);
		return wrapperObject;
	}
}

void DOMBase::FindMakeWrapperMethod()
{
	_makeWrapperMethod = _monoInstance->FindDOMMethod("DOMBinding.DOMBinder:CallMakeWrapper");
}	

void DOMBase::FindSetNextWrapper()
{
	_setNextWrapperMethod = _monoInstance->FindDOMMethod("DOMBinding.DOMBinder:SetNextWrapper");
}	

void DOMBase::FindSetPrevWrapper()
{
	_setPrevWrapperMethod = _monoInstance->FindDOMMethod("DOMBinding.DOMBinder:SetPrevWrapper");
}	

MonoObject* DOMBase::MakeWrapper(DOMBase* domobj, int wrapperIndex)
{
	if (_makeWrapperMethod == NULL)
		FindMakeWrapperMethod();
	
	//printf ("%s:%d %s() Mono calling C code\n", __FILE__, __LINE__, __FUNCTION__);
	void *args[2];
	args[0] = &domobj;
	args[1] = &wrapperIndex;
	MonoObject* exception = NULL;
	MonoObject* result = mono_runtime_invoke(_makeWrapperMethod, NULL, args, &exception);
	if (exception != NULL)
	    FatalError("An exception occurred while calling MakeWrapper");
	return result;
	//return NULL;
}

void DOMBase::SetNextWrapper(MonoObject* next)
{	
	if (_setNextWrapperMethod == NULL)
		FindSetNextWrapper();
	
	void *args[2];
	args[0] = GetJSWrapper();
	args[1] = next;
	mono_runtime_invoke(_setNextWrapperMethod, NULL, args, NULL);
}

void DOMBase::SetPrevWrapper(MonoObject* prev)
{
	if (_setPrevWrapperMethod == NULL)
		FindSetPrevWrapper();
	
	void *args[2];
	args[0] = GetJSWrapper();
	args[1] = prev;
	mono_runtime_invoke(_setPrevWrapperMethod, NULL, args, NULL);
}

} //namespace
