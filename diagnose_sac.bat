@echo off
REM Script de diagnóstico para problemas de entrenamiento SAC
REM Uso: diagnose_sac.bat

echo === DIAGNÓSTICO DE ENTRENAMIENTO SAC BLOQUEADO ===
echo.

echo 1. VERIFICANDO PROCESOS ACTIVOS:
echo --------------------------------

REM Verificar procesos ML-Agents
tasklist | findstr /i "python" >nul
if %errorlevel%==0 (
    echo ✓ Procesos Python encontrados:
    tasklist | findstr /i "python"
) else (
    echo ✗ No se encontraron procesos Python activos
)

REM Verificar procesos Unity
tasklist | findstr /i "Unity" >nul
if %errorlevel%==0 (
    echo ✓ Procesos Unity encontrados:
    tasklist | findstr /i "Unity"
) else (
    echo ✗ No se encontraron procesos Unity activos
)
echo.

echo 2. VERIFICANDO PUERTOS:
echo ----------------------

netstat -an | findstr ":5005" >nul
if %errorlevel%==0 (
    echo ✓ Puerto 5005 en uso
    netstat -an | findstr ":5005"
) else (
    echo ✗ Puerto 5005 libre
)

netstat -an | findstr ":5006" >nul
if %errorlevel%==0 (
    echo ✓ Puerto 5006 en uso
    netstat -an | findstr ":5006"
) else (
    echo ✗ Puerto 5006 libre
)
echo.

echo 3. VERIFICANDO MEMORIA:
echo ----------------------
wmic OS get TotalVisibleMemorySize,FreePhysicalMemory /format:table
echo.

echo 4. VERIFICANDO ARCHIVOS RECIENTES:
echo ----------------------------------
if exist "results\" (
    echo Buscando logs recientes en results\...
    dir results\*.log /s /o:d 2>nul | findstr /v "bytes free" | tail -5
) else (
    echo ✗ Directorio results\ no encontrado
)
echo.

echo 5. SUGERENCIAS PARA RESOLVER BLOQUEOS:
echo ======================================
echo.
echo PASOS INMEDIATOS:
echo -----------------
echo 1. Presiona Ctrl+C en la ventana de entrenamiento para terminar limpiamente
echo 2. Si no responde, usa el Administrador de tareas para terminar Python/Unity
echo 3. Espera 30 segundos antes de reiniciar
echo.

echo CONFIGURACIÓN ESTABLE RECOMENDADA:
echo ----------------------------------
echo mlagents-learn config\sac\SoccerTwos_Stable.yaml --run-id=SAC_Stable_Test --force
echo.

echo ALTERNATIVA MÁS RÁPIDA Y ESTABLE:
echo ---------------------------------
echo mlagents-learn config\ppo\SoccerTwos.yaml --run-id=PPO_Test --force
echo PPO es mucho más estable y rápido que SAC para este entorno
echo.

echo MONITOREO:
echo ----------
echo • Usar Administrador de tareas para monitorear CPU/memoria
echo • Verificar que time_scale no sea demasiado alto
echo • Si se bloquea repetidamente, reducir num_envs a 1
echo.

echo ¿Deseas terminar todos los procesos Python? (S/N)
set /p response=
if /i "%response%"=="S" (
    echo Terminando procesos Python...
    taskkill /f /im python.exe >nul 2>&1
    echo Terminando procesos Unity...
    taskkill /f /im Unity.exe >nul 2>&1
    echo ✓ Procesos terminados
) else (
    echo Saltando limpieza de procesos
)

echo.
echo === DIAGNÓSTICO COMPLETADO ===
pause
