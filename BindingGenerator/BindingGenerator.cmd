@ECHO OFF
SETLOCAL
SET ROOTDIR=%~dp0
REM ECHO %ROOTDIR%
PUSHD %ROOTDIR%

CALL :SetUpEnv
CALL :IDLXML
CALL :CodeGen %1

POPD
ENDLOCAL
EXIT /b


REM #############################################################################

:SetUpEnv
ECHO Set up the environment.
IF EXIST "%VS110COMNTOOLS%..\..\vc\vcvarsall.bat" (
	ECHO "Using VS 2011"
	CALL "%VS110COMNTOOLS%..\..\vc\vcvarsall.bat" x86
	SET SOLUTION_SUFFIX=-VS2011
) ELSE IF EXIST "%VS110COMNTOOLS%\vsvars32.bat" (
	ECHO "Using VS 2011"
	CALL "%VS110COMNTOOLS%\vsvars32.bat"
	SET SOLUTION_SUFFIX=-VS2011
) ELSE IF EXIST "%VS100COMNTOOLS%\vsvars32.bat" (
	ECHO "Using VS 2010"
	CALL "%VS100COMNTOOLS%\vsvars32.bat"
) ELSE (
	ECHO "Could not find Visual Studio!"
	EXIT
)
ECHO Ensure that the output directory exists.
IF NOT EXIST "Build\" ( MD "Build\" )
SET MONO="C:\Program Files (x86)\Mono-3.0.2\bin\mono.exe"
IF NOT EXIST %MONO% (
	ECHO "Could not find mono"
	SET MONO=
)
ECHO "Mono="%MONO%
EXIT /b

REM #############################################################################

:IDLXML
ECHO Ensure widlproc is built.
COPY /b ..\ExtraProjects\* widlproc\
MSBuild widlproc\widlproc%SOLUTION_SUFFIX%.sln /p:Configuration=Release
ECHO Generate AutoDOMBind.idl.xml from the IDL files.
COPY /b IDL\*.idl Build\AutoDOMBind.idl
widlproc\Release\widlproc%SOLUTION_SUFFIX%.exe Build\AutoDOMBind.idl > Build\AutoDOMBind.idl.xml
EXIT /b

REM #############################################################################

:CodeGen
ECHO Ensure IDLCodeGen is built.
MSBuild IDLCodeGen\IDLCodeGen.sln /p:Configuration=Release
ECHO Generating binding files.
ECHO %TIME%

echo %1
IF /I "%~1"=="--clean" (
	ECHO "Cleaning auto generated files"
	DEL ..\MCWebRuntime\AutoDOM.cs ..\MCWebRuntime\AutoBindings.cs ..\MCWebRuntime\AutoInternal.cs ..\MCWebRuntime\AutoPrivate.cs ..\MCWebRuntime\AutoAPI.cs ..\MCJavascriptRuntime\Operations\AutoJSOperations.cs
) ELSE IF "%~1"=="--AutoJSOperations" (
	ECHO "Generating js operations"
	%MONO% IDLCodeGen\bin\Release\IDLCodeGen.exe --outdir=Build --AutoJSOperations=..\MCJavascriptRuntime\Operations\AutoJSOperations.cs
) ELSE (
	ECHO "Generating binding files"
	%MONO% IDLCodeGen\bin\Release\IDLCodeGen.exe --outdir=Build --idl=Build\AutoDOMBind.idl.xml --AutoDOM=..\MCWebRuntime\AutoDOM.cs --AutoBindings=..\MCWebRuntime\AutoBindings.cs --AutoInternal=..\MCWebRuntime\AutoInternal.cs --AutoPrivate=..\MCWebRuntime\AutoPrivate.cs --AutoAPI=..\MCWebRuntime\AutoAPI.cs
)

ECHO %TIME%
EXIT /b
