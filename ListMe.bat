@ECHO off

SET "CONVERTERDIR=%~dp0\PresetConverterProject\bin\Release\net7.0\win-x64\publish"
SET "OUTPUTDIR=%UserProfile%\Projects\Temp"
SET "CONVERTER=PresetConverter.exe"
SET "DOTNET_ENVIRONMENT=Development"

FOR %%i IN (%*) DO IF EXIST %%~si\NUL (CALL :ISDIR %%i) ELSE (CALL :ISFILE %%i)
%SystemRoot%\explorer.exe "%OUTPUTDIR%"
GOTO EXIT

:ISDIR
ECHO %1 is a directory ...
REM for verbose logging use ... -i %1 -o "%OUTPUTDIR%" -l -v
CALL "%CONVERTERDIR%\%CONVERTER%" -i %1 -o "%OUTPUTDIR%" -l
PAUSE
GOTO EXIT

:ISFILE
ECHO %1 is a file ...
REM for verbose logging use ... -i %1 -o "%OUTPUTDIR%" -k6 -l -v
CALL "%CONVERTERDIR%\%CONVERTER%" -i %1 -o "%OUTPUTDIR%" -k6 -l
PAUSE
GOTO EXIT

:EXIT
