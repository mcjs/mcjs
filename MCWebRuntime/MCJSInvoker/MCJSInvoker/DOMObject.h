#ifndef DOMOBJECT_H_
#define DOMOBJECT_H_

#include "DOMBase.h"

namespace JSBinding
{
class DOMObject : public DOMBase
{
	int number;
	virtual MakeWrapperIndexType GetMakeWrapperIndex() const;
public:	
	//All DOM classes that need to interact with JS must implement this function
	//virtual MakeWrapperIndexType GetMakeWrapperIndex() { return SampleObject; }
	
	DOMObject(int num);
	//These are a few sample methods
	int IntDoSomething();
	DOMObject* GetElementByName(MonoString* name);
	char* GetName(char*);
	int GetNumber();
	void SetNumber(int num);
};
}

using namespace JSBinding;
// Interface to our C++ code
//int GetWrapperIndex(DOMBase *domobj);
//void SetWrapperIndex(DOMBase *domobj, int index);
//void SetJSWrapperWPtr(DOMBase *domobj, MonoObject *monoobj);

// Interface to our C++ code for the sample methods
MonoObject* CreateDomObject();
int DOMObject_IntDoSomething(DOMObject *domObject);
MonoObject* DOMObject_GetElementByName(DOMObject *domObject, MonoString* name);
MonoString* DOMObject_GetName(DOMObject* domObject);
int DOMObject_GetNum(DOMObject *domObject);
void DOMObject_SetNum(DOMObject *domObject, int num);

#endif
