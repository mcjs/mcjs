#include "DOMObject.h"
#include "stdio.h"
#include <string.h>

namespace JSBinding
{
DOMObject::DOMObject(int num) : number(num)
{}

int DOMObject::IntDoSomething()
{
	printf ("%s:%d %s() Returning back %d\n", __FILE__, __LINE__, __FUNCTION__, number);
	return number;
}

char* DOMObject::GetName(char* buffer)
{
	//This is sample code. Let's assume buffer is large enough, but do make such assumptions in real code!
	switch(number) {
		case 1:
			strncpy(buffer, "One", 4);
			break;
		case 2:
			strncpy(buffer, "Two", 4);
			break;
		default:
			strncpy(buffer, "larger than two", 17);
	}
//	printf ("%s:%d %s() Returning back %s using number %d\n", __FILE__, __LINE__, __FUNCTION__, buffer, number);
	return buffer;
}

MakeWrapperIndexType DOMObject::GetMakeWrapperIndex() const
{
	return SampleObject;
}

DOMObject* DOMObject::GetElementByName(MonoString *name)
{
	return this;	
}

int DOMObject::GetNumber()
{
  return number;
}

void DOMObject::SetNumber(int num)
{
  number = num;
}


}

///////// Mono can call C functions only. So, the followings act as an interface to our C++ code:

using namespace JSBinding;
////// Extra sample functions
MonoObject* CreateDomObject()
{
//	printf ("%s:%d %s() Mono calling C code\n", __FILE__, __LINE__, __FUNCTION__);
	DOMObject* temp = (new DOMObject(12));
	return temp->GetJSWrapper();
}

int DOMObject_IntDoSomething(DOMObject *domObject)
{
//	printf ("%s:%d %s() Mono calling C code\n", __FILE__, __LINE__, __FUNCTION__);
	return domObject->IntDoSomething();
}

MonoObject* DOMObject_GetElementByName(DOMObject *domObject, MonoString *name)
{
//	printf ("%s:%d %s() Mono calling C code\n", __FILE__, __LINE__, __FUNCTION__);
	DOMBase* element = domObject->GetElementByName(name);		
	return element->GetJSWrapper();
}

MonoString* DOMObject_GetName(DOMObject *domObject)
{
//	printf ("%s:%d %s() Mono calling C code\n", __FILE__, __LINE__, __FUNCTION__);
	char buffer[20];
	return mono_string_new (mono_domain_get(), domObject->GetName(buffer));
}

int DOMObject_GetNum(DOMObject *domObject)
{
  return domObject->GetNumber();
}

void DOMObject_SetNum(DOMObject *domObject, int num)
{
  domObject->SetNumber(num);
}
