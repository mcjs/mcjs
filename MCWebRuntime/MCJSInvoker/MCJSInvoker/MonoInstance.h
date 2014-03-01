#ifndef MONOSERVICES_H_
#define MONOSERVICES_H_

#include <mono/jit/jit.h>

namespace JSBinding
{

void FatalError(const char *format, ...);

class MonoInstance
{
	MonoDomain* _domain;
	MonoAssembly* _assembly;
	MonoAssembly* _domAssembly;
	MonoImage* _image;
	MonoInstance(const char *);
	static MonoInstance* _instance;	
public:
	static MonoInstance* Instance();
	static void Init(const char *);
	~MonoInstance();
	MonoDomain* GetDomain() { return _domain; }
	MonoAssembly* GetAssembly() { return _assembly; }	
	MonoImage* GetImage() { return _image; }
	MonoMethod* FindJSMethod(const char *FQName);
	MonoMethod* FindDOMMethod(const char *FQName);
};

}

#endif
