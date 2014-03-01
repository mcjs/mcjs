###########################################################################
# Tools used during the build process.
###########################################################################
#If necessary, overwrite the MONO_HOST when calling this makefile
#ZOOMM_DIR ?= $(shell pwd)/..
#MONO_HOST ?= packages/mono/mono-linux
#MONO_BIN ?= $(ZOOMM_DIR)/$(MONO_HOST)/bin
XBUILD = xbuild
MONO = mono

BINDING_GENERATOR_DIR := ./BindingGenerator
GENSRCS_DIR := $(BINDING_GENERATOR_DIR)/Build
IDLCODEGEN_EXE := $(BINDING_GENERATOR_DIR)/IDLCodeGen/bin/Release/IDLCodeGen.exe
MWR_DIR := ./MCWebRuntime
MJRO_DIR := ./MCJavascriptRuntime/Operations

ifeq ($(JS_BUILD_TYPE),)
  JS_BUILD_TYPE = Release
endif

ifeq ($(JS_BUILD_TYPE), Debug)
  JS_BUILD_TYPE_CONST = DEBUG
endif

ifeq ($(JS_BUILD_TYPE), Debug)
  ZOOMM_BIN = $(ZOOMM_DIR)/build/x86-DEV/bin
endif
ifeq ($(JS_BUILD_TYPE), Release)
  ZOOMM_BIN = $(ZOOMM_DIR)/build/x86-PERF/bin
endif


XBUILD_OPTS = /nologo /property:GenerateFullPaths=True 
#/verbosity:quiet 

default: mcjs

$(IDLCODEGEN_EXE):
	$(MAKE) -C $(BINDING_GENERATOR_DIR)

$(MWR_DIR)/AutoDOM.cs: $(IDLCODEGEN_EXE)
	$(MONO) $(IDLCODEGEN_EXE) --outdir=$(MWR_DIR)/ --idl=$(GENSRCS_DIR)/AutoDOMBind.idl.xml --AutoDOM

$(MWR_DIR)/AutoBindings.cs: $(IDLCODEGEN_EXE)
	$(MONO) $(IDLCODEGEN_EXE) --outdir=$(MWR_DIR)/ --idl=$(GENSRCS_DIR)/AutoDOMBind.idl.xml --AutoBindings

$(MWR_DIR)/AutoInternal.cs: $(IDLCODEGEN_EXE)
	$(MONO) $(IDLCODEGEN_EXE) --outdir=$(MWR_DIR)/ --idl=$(GENSRCS_DIR)/AutoDOMBind.idl.xml --AutoInternal

$(MWR_DIR)/AutoPrivate.cs: $(IDLCODEGEN_EXE)
	$(MONO) $(IDLCODEGEN_EXE) --outdir=$(MWR_DIR)/ --idl=$(GENSRCS_DIR)/AutoDOMBind.idl.xml --AutoPrivate

$(MWR_DIR)/AutoAPI.cs: $(IDLCODEGEN_EXE)
	$(MONO) $(IDLCODEGEN_EXE) --outdir=$(MWR_DIR)/ --idl=$(GENSRCS_DIR)/AutoDOMBind.idl.xml --AutoAPI

$(MJRO_DIR)/AutoJSOperations.cs: $(IDLCODEGEN_EXE)
	$(MONO) $(IDLCODEGEN_EXE) --outdir=$(MWR_DIR)/ --idl=$(GENSRCS_DIR)/AutoDOMBind.idl.xml --AutoJSOperations=$(MJRO_DIR)/AutoJSOperations.cs

idlgen: $(IDLCODEGEN_EXE)

autogens: $(MWR_DIR)/AutoDOM.cs $(MWR_DIR)/AutoBindings.cs $(MWR_DIR)/AutoInternal.cs $(MWR_DIR)/AutoPrivate.cs $(MWR_DIR)/AutoAPI.cs $(MJRO_DIR)/AutoJSOperations.cs

mcjs_rr: idlgen autogens
	echo "MCJavascript.exe Build type: $(JS_BUILD_TYPE) (Zoomm: $(BUILD_TYPE))"
	export PATH="$(MONO_BIN):$$PATH" && \
	echo "We first clean the RR project to make sure a rebuild is forced. Otherwise, the none RR build from last tims gets used" && \
	$(XBUILD) $(XBUILD_OPTS) /target:Clean MCWebRuntime/MCWebRuntime.csproj && \
	$(XBUILD) $(XBUILD_OPTS) /p:Configuration=$(JS_BUILD_TYPE) /p:DefineConstants="$(JS_BUILD_TYPE_CONST) TRACE ENABLE_RR ENABLE_HOOKS" MCWebRuntime/MCWebRuntime.csproj && \
	$(XBUILD) $(XBUILD_OPTS) /p:Configuration=$(JS_BUILD_TYPE) /p:DefineConstants="$(JS_BUILD_TYPE_CONST) TRACE ENABLE_RR ENABLE_HOOKS" MCJavascript.sln && \
	mv MCJavascript/bin/$(JS_BUILD_TYPE)/MCWebRuntime.dll MCJavascript/bin/$(JS_BUILD_TYPE)/MCWebRuntimeRR.dll && \
	echo "We now clean the RR project again to make sure normal project is giong to be build" && \
	$(XBUILD) $(XBUILD_OPTS) /target:Clean MCWebRuntime/MCWebRuntime.csproj && \
	$(XBUILD) $(XBUILD_OPTS) /target:Clean MCJavascript.sln 

mcjs_perf: idlgen autogens mcjs_rr
	export PATH="$(MONO_BIN):$$PATH" && \
	$(XBUILD) $(XBUILD_OPTS) /p:Configuration=$(JS_BUILD_TYPE) MCJavascript.sln

mcjs: mcjs_rr mcjs_perf


clean:
	find . -depth -name obj -type d -exec rm -rf {} \;
	find . -depth -name bin -type d -exec rm -rf {} \;
	rm -f $(MWR_DIR)/AutoDOM.cs $(MWR_DIR)/AutoBindings.cs $(MWR_DIR)/AutoInternal.cs $(MWR_DIR)/AutoPrivate.cs $(MWR_DIR)/AutoAPI.cs $(MJRO_DIR)/AutoJSOperations.cs
	$(MAKE) -C BindingGenerator clean

#The following is a hack for debug time to avoid running the slow Zoomm makefile
zoomm: mcjs
	cp MCJavascript/bin/$(JS_BUILD_TYPE)/* $(ZOOMM_BIN)
