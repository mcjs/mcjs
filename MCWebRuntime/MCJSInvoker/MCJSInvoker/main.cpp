#include "JSRuntimeWrapper.h"

using namespace JSBinding;
void foo(const char* name);

int main (int argc, char *argv[])
{       
	/*for (int i = 0; i < argc; i++)
		printf("arg%d: %s\n", i, argv[i]);*/
		
	JSRuntimeWrapper jsruntime(argc - 1, &(argv[1]));
	jsruntime.RunScriptFile(argv[2]);
	jsruntime.RunScriptString("var x = 10; print(x);");
	return 0;
}
