@ECHO off

SET "CONVERTERDIR=C:\Users\perner\My Projects\PresetConverter\PresetConverterProject\bin\Release\netcoreapp2.2\win-x64\publish"
SET "OUTPUTDIR=C:\Users\perner\My Projects\Temp"
SET "CONVERTER=PresetConverter.exe"

FOR %%i IN (%*) DO IF EXIST %%~si\NUL (CALL :ISDIR %%i) ELSE (CALL :ISFILE %%i)
GOTO EXIT

:ISDIR
ECHO %1 is a directory ...
CALL "%CONVERTERDIR%\%CONVERTER%" -i %1 -o "%OUTPUTDIR%" 
PAUSE
GOTO EXIT

:ISFILE
ECHO %1 is a file ...
CALL "%CONVERTERDIR%\%CONVERTER%" -i %1 -o "%OUTPUTDIR%" 
PAUSE
GOTO EXIT

:EXIT
