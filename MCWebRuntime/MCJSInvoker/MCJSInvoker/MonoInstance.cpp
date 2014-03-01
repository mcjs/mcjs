#include <iostream>
#include <stdlib.h>
#include <stdarg.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>
#include "MonoInstance.h"

#define OUTPUT	stdout
#define DOM_DLL_NAME	"DOMBinding.dll"

using namespace std;

namespace JSBinding
{

void FatalError(const char *format, ...)
{
	va_list ap;
	va_start(ap, format);
	fprintf(OUTPUT, "Fatal Error: ");
	vfprintf(OUTPUT, format, ap);
	va_end(ap);
	fprintf(OUTPUT, "\n");
	
	exit(1); //TODO: perhaps a better error handling is needed
}
    
MonoInstance::MonoInstance(const char* executableName)
{
    _domain = mono_jit_init("MCJSBinding");
    if (!_domain)
    	FatalError("cannot init the application domain");
    
	_assembly = mono_domain_assembly_open (_domain, executableName);
	if (!_assembly)
		FatalError("cannot open assembly. Make sure the JS Engine path is correct.");

	string dllName(executableName);  	
  	size_t found = dllName.rfind('/');
  	if (found != string::npos)
    	dllName.replace(found + 1, dllName.length() - found, DOM_DLL_NAME);
	
	_domAssembly = mono_domain_assembly_open (_domain, dllName.c_str());
	if (!_domAssembly)
		FatalError("cannot open DOM assembly. Make sure it is in the same directory as the JS engine.");
	
	_image = mono_assembly_get_image(_assembly);
	if (!_image)
		FatalError("cannot get mono image from assembly");
}

MonoInstance::~MonoInstance()
{
	mono_jit_cleanup (_domain);
}

MonoInstance* MonoInstance::_instance;

MonoInstance* MonoInstance::Instance()
{
	if (_instance == NULL)
		FatalError("No mono instance was found");
	return _instance;
}

void MonoInstance::Init(const char *executableName)
{
	if (_instance == NULL)	
		_instance = new MonoInstance(executableName);
}

MonoMethod* MonoInstance::FindJSMethod(const char *FQName)
{
	MonoMethodDesc* desc = mono_method_desc_new(FQName, true);
	MonoMethod* method = mono_method_desc_search_in_image(desc, _image);
	if (method == NULL)
		FatalError("cannot find %s in the JS world!", FQName);
//	mono_method_desc_free(desc);
	return method;
}

MonoMethod* MonoInstance::FindDOMMethod(const char *FQName)
{
	MonoMethodDesc* desc = mono_method_desc_new(FQName, true);
	MonoImage* image = mono_assembly_get_image(_domAssembly);
	if (!image)
		FatalError("cannot get dom assembly image");
	MonoMethod* method = mono_method_desc_search_in_image(desc, image);
	if (method == NULL)
		FatalError("cannot find %s in the DOM assembly!", FQName);
	//mono_method_desc_free(desc);
	return method;
}

}
