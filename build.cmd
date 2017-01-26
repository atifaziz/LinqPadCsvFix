@echo off
pushd "%~dp0"
call :main %*
popd

:main
setlocal
nuget restore ^
  && msbuild /v:m /p:Configuration=Debug %* ^
  && msbuild /v:m /p:Configuration=Release %*
goto :EOF