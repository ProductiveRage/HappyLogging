@echo off

pushd "%~dp0"

del *.nu* 2> nul
del *.dll 2> nul
del *.pdb 2> nul
del *.xml 2> nul

copy ..\HappyLogging\bin\Release\HappyLogging.dll > nul
copy ..\HappyLogging\bin\Release\HappyLogging.pdb > nul
copy ..\HappyLogging\bin\Release\HappyLogging.xml > nul

.\nuget.exe pack ..\HappyLogging.nuspec -BasePath .

popd