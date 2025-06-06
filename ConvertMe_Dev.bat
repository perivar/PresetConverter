@ECHO off

SET "CONVERTERDIR=%~dp0\PresetConverterProject\bin\Release\net9.0\win-x64\publish"
SET "OUTPUTDIR=%UserProfile%\Projects\Temp"
SET "CONVERTER=PresetConverter.exe"
SET "DOTNET_ENVIRONMENT=Development"

FOR %%i IN (%*) DO IF EXIST %%~si\NUL (CALL :ISDIR %%i) ELSE (CALL :ISFILE %%i)
%SystemRoot%\explorer.exe "%OUTPUTDIR%"
GOTO EXIT

:ISDIR
ECHO %1 is a directory ...
IF "%DOTNET_ENVIRONMENT%" == "Development" (
    ECHO DOTNET_ENVIRONMENT is Development, using verbose logging ...
    CALL "%CONVERTERDIR%\%CONVERTER%" -i %1 -o "%OUTPUTDIR%" -v
) ELSE (
    CALL "%CONVERTERDIR%\%CONVERTER%" -i %1 -o "%OUTPUTDIR%"
)
PAUSE
GOTO EXIT

:ISFILE
ECHO %1 is a file ...
IF "%DOTNET_ENVIRONMENT%" == "Development" (
    ECHO DOTNET_ENVIRONMENT is Development, using verbose logging ...
    CALL "%CONVERTERDIR%\%CONVERTER%" -i %1 -o "%OUTPUTDIR%" -v
) ELSE (
    CALL "%CONVERTERDIR%\%CONVERTER%" -i %1 -o "%OUTPUTDIR%"
)
PAUSE
GOTO EXIT

:EXIT
