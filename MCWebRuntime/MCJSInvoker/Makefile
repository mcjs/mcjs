CC=g++
OSTYPE=MacOS
# OSTYPE=Linux

ifeq ($(OSTYPE), MacOS)
	CFLAGS=-m32 -D_THREAD_SAFE -I/Library/Frameworks/Mono.framework/Headers/mono-2.0 -O2
	LDFLAGS=-m32 -L/Library/Frameworks/Mono.framework/Versions/2.8/lib -lmono-2.0 -pthread -lpthread
else
	# The demo code here is compiled for Linux x86. The actual Zoomm code in src/javascript implements
	# compilation for both x86 and ARM.
	export PKG_CONFIG_PATH=../../mono/mono-linux/lib/pkgconfig

	CFLAGS=`pkg-config --cflags mono-2`
	# Force libmono-2.0 to be linked statically, but leave all the other libs as is
	LDFLAGS=`pkg-config --libs-only-L mono-2` -Wl,-Bstatic -lmono-2.0 -Wl,-Bdynamic `pkg-config --libs-only-l mono-2 | sed -e "s/\-lmono-2.0 //"`
endif

# We require xbuild, and we will use the version built in the external/mono directory that we compile ourself
#PATH := ../../mono/mono-linux/bin:$(PATH)

INVOKERPATH=MCJSInvoker
#CALLNATPATH=CallNativeCode
#CALLNATCODE=$(CALLNATPATH)/bin/Debug/CallNativeCode
OBJDIR=$(INVOKERPATH)/bin

all: $(OBJDIR) $(OBJDIR)/mcjsinvoker 

install: all

#$(CALLNATCODE): $(CALLNATPATH)/CallNativeCode.csproj
#	xbuild CallNativeCode/CallNativeCode.csproj

$(OBJDIR)/mcjsinvoker: $(OBJDIR)/main.o
	$(CC) $(CFLAGS) $(OBJDIR)/*.o -o $@ $(LDFLAGS)

$(OBJDIR)/main.o: $(INVOKERPATH)/main.cpp 
	$(CC) $(CFLAGS) -c $< -o $@

$(OBJDIR)/%.o: $(INVOKERPATH)/%.cpp $(INVOKERPATH)/%.h
	$(CC) $(CFLAGS) -c $< -o $@

$(OBJDIR):
	mkdir $@

clean:
	rm -rf $(OBJDIR) 

run: $(OBJDIR)/mcjsinvoker 
	$(OBJDIR)/mcjsinvoker 

#===== Dependencies ======
$(OBJDIR)/main.o: $(OBJDIR)/JSRuntimeWrapper.o
$(OBJDIR)/JSRuntimeWrapper.o: $(OBJDIR)/MonoInstance.o $(OBJDIR)/DOMObject.o
$(OBJDIR)/DOMObject.o: $(OBJDIR)/DOMBase.o
