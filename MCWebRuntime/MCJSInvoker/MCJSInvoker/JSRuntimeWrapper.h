#ifndef JSRUNTIMEWRAPPER_H_
#define JSRUNTIMEWRAPPER_H_

#include "MonoInstance.h"

namespace JSBinding
{

class JSRuntimeWrapper
{
	MonoDomain* _domain;
	MonoMethod* _initMethod;
	MonoMethod* _runFileMethod;
	MonoMethod* _runStringMethod;	
			
	static bool isInternalCallsInitialized;
	void InitInternalCalls();
	void Init(int argc, char** argv) const;
	void FindJSMethods();	
public:
	JSRuntimeWrapper(int argc, char** argv);	
	~JSRuntimeWrapper();	
	void RunScriptFile(const char* filename) const;
	void RunScriptString(const char* string) const;	
};

}
#endif
