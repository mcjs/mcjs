###########################################################################
# Tools used during the build process.
###########################################################################
#If necessary, overwrite the MONO_HOST when calling this makefile
#ZOOMM_DIR := $(shell pwd)/../..
#MONO_HOST ?= packages/mono/mono-linux
#MONO_BIN = $(ZOOMM_DIR)/$(MONO_HOST)/bin
XBUILD = xbuild
RUN_MONO = mono

ifeq (${USE_SYSTEM_MONO},true)
	XBUILD = xbuild
	RUN_MONO = mono
endif

BINDING_GENERATOR_DIR := .
GENSRCS_DIR := $(BINDING_GENERATOR_DIR)/Build
IDL_DIR := $(BINDING_GENERATOR_DIR)/IDL
WIDLPROC := $(BINDING_GENERATOR_DIR)/widlproc/obj/widlproc
IDLCODEGEN := $(BINDING_GENERATOR_DIR)/IDLCodeGen/bin/Release/IDLCodeGen.exe

XBUILD_OPTS = /nologo /verbosity:quiet /property:GenerateFullPaths=True

default: $(IDLCODEGEN) $(GENSRCS_DIR)/AutoDOMBind.idl.xml

$(WIDLPROC): $(BINDING_GENERATOR_DIR)/widlproc/src/*.[ch]
	$(MAKE) CC="gcc" -C $(BINDING_GENERATOR_DIR)/widlproc

$(IDLCODEGEN): $(shell find $(BINDING_GENERATOR_DIR)/IDLCodeGen/ -name '*.cs')
	export PATH="$(MONO_BIN):$$PATH" && \
	$(XBUILD) $(XBUILD_OPTS) /p:Configuration=Release $(BINDING_GENERATOR_DIR)/IDLCodeGen/IDLCodeGen.sln

$(GENSRCS_DIR)/AutoDOMBind.idl.xml: $(wildcard $(IDL_DIR)/*.idl) $(WIDLPROC)
	mkdir -p $(GENSRCS_DIR)
	$(WIDLPROC) $(wildcard $(IDL_DIR)/*.idl) > $@

clean:
	$(MAKE) CC="gcc" -C $(BINDING_GENERATOR_DIR)/widlproc clean
	rm -rf $(BINDING_GENERATOR_DIR)/IDLCodeGen/bin $(BINDING_GENERATOR_DIR)/IDLCodeGen/obj $(GENSRCS_DIR)
