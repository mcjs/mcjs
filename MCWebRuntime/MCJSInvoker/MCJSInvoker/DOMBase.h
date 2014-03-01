#ifndef DOMBASE_H_
#define DOMBASE_H_

#include <mono/jit/jit.h>
#include "MonoInstance.h"
#include "AutoDOMBind.h"

namespace JSBinding {

class DOMBase
{
protected:
	uint32_t _JSWrapperWPtr; //weak pointer to the JS wrapper object	
	static MonoInstance* _monoInstance;
	static MonoMethod* _makeWrapperMethod;
	static MonoMethod* _setNextWrapperMethod;
	static MonoMethod* _setPrevWrapperMethod;
	
	MonoObject* MakeWrapper(DOMBase* domobj, int wrapperIndex);
	void FindMakeWrapperMethod();
	void FindSetNextWrapper();
	void FindSetPrevWrapper();
	virtual MakeWrapperIndexType GetMakeWrapperIndex() const = 0;
public:
	DOMBase();
	virtual ~DOMBase();

	//force child classes override this function	
	MonoObject* GetJSWrapper();
	void SetNextWrapper(MonoObject* next);
	void SetPrevWrapper(MonoObject* prev);
};

}
#endif
