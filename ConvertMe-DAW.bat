@ECHO off

SET "CONVERTERDIR=%~dp0\PresetConverterProject\bin\Release\netcoreapp3.0\win-x64\publish"
SET "OUTPUTDIR=G:\Development Projects\Own Projects\Temp"
SET "CONVERTER=PresetConverter.exe"

FOR %%i IN (%*) DO IF EXIST %%~si\NUL (CALL :ISDIR %%i) ELSE (CALL :ISFILE %%i)
%SystemRoot%\explorer.exe "%OUTPUTDIR%"
GOTO EXIT

:ISDIR
ECHO %1 is a directory ...
CALL "%CONVERTERDIR%\%CONVERTER%" -i %1 -o "%OUTPUTDIR%" 
PAUSE
GOTO EXIT

:ISFILE
ECHO %1 is a file ...
CALL "%CONVERTERDIR%\%CONVERTER%" -i %1 -o "%OUTPUTDIR%" -k6
PAUSE
GOTO EXIT

:EXIT
