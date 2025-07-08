@echo off
REM Training script for SoccerTwos with SAC
REM Make sure you have ml-agents package installed: pip install mlagents

echo Starting SoccerTwos training with SAC algorithm...
echo Make sure your Unity environment is built and ready to connect.
echo.

REM Default parameters
set CONFIG_FILE=config\sac\SoccerTwos.yaml
set RUN_ID=SoccerTwos_SAC_%date:~10,4%%date:~4,2%%date:~7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set ENV_PATH=

REM Remove spaces from RUN_ID
set RUN_ID=%RUN_ID: =%

REM Parse command line arguments
:parse_args
if "%1"=="--config" (
    set CONFIG_FILE=%2
    shift
    shift
    goto parse_args
)
if "%1"=="--run-id" (
    set RUN_ID=%2
    shift
    shift
    goto parse_args
)
if "%1"=="--env" (
    set ENV_PATH=%2
    shift
    shift
    goto parse_args
)
if "%1"=="--help" (
    echo Usage: %0 [options]
    echo Options:
    echo   --config FILE    Configuration file ^(default: config\sac\SoccerTwos.yaml^)
    echo   --run-id ID      Run identifier ^(default: SoccerTwos_SAC_timestamp^)
    echo   --env PATH       Path to Unity environment executable ^(optional^)
    echo   --help           Show this help message
    exit /b 0
)
if not "%1"=="" (
    echo Unknown option: %1
    echo Use --help for usage information
    exit /b 1
)

REM Check if config file exists
if not exist "%CONFIG_FILE%" (
    echo Error: Configuration file not found: %CONFIG_FILE%
    exit /b 1
)

echo Configuration: %CONFIG_FILE%
echo Run ID: %RUN_ID%

REM Build the mlagents-learn command
set COMMAND=mlagents-learn %CONFIG_FILE% --run-id=%RUN_ID%

if not "%ENV_PATH%"=="" (
    set COMMAND=%COMMAND% --env=%ENV_PATH%
    echo Environment: %ENV_PATH%
) else (
    REM Try to use default build if it exists
    if exist "Project\Builds\UnityEnvironment.exe" (
        set COMMAND=%COMMAND% --env=Project\Builds\UnityEnvironment.exe
        echo Environment: Project\Builds\UnityEnvironment.exe ^(auto-detected^)
    ) else (
        echo Environment: Unity Editor ^(manual start required^)
        echo Make sure to press PLAY in Unity Editor after starting training
    )
)

echo.
echo Running command:
echo %COMMAND%
echo.

REM Execute the training
%COMMAND%
