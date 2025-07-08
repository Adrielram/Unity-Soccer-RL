#!/bin/bash

# Script de diagnóstico para problemas de entrenamiento SAC
# Uso: ./diagnose_sac.sh

echo "=== DIAGNÓSTICO DE ENTRENAMIENTO SAC BLOQUEADO ==="
echo ""

# Función para verificar procesos
check_processes() {
    echo "1. VERIFICANDO PROCESOS ACTIVOS:"
    echo "--------------------------------"
    
    # Buscar procesos de ML-Agents
    MLAGENTS_PROC=$(ps aux | grep mlagents-learn | grep -v grep)
    if [ ! -z "$MLAGENTS_PROC" ]; then
        echo "✓ Proceso ML-Agents encontrado:"
        echo "$MLAGENTS_PROC"
    else
        echo "✗ No se encontró proceso ML-Agents activo"
    fi
    
    # Buscar procesos de Unity
    UNITY_PROC=$(ps aux | grep Unity | grep -v grep)
    if [ ! -z "$UNITY_PROC" ]; then
        echo "✓ Proceso Unity encontrado:"
        echo "$UNITY_PROC"
    else
        echo "✗ No se encontró proceso Unity activo"
    fi
    echo ""
}

# Función para verificar puertos
check_ports() {
    echo "2. VERIFICANDO PUERTOS:"
    echo "----------------------"
    
    for port in 5005 5006 5007 5008; do
        PORT_USAGE=$(netstat -tulpn 2>/dev/null | grep ":$port ")
        if [ ! -z "$PORT_USAGE" ]; then
            echo "✓ Puerto $port en uso: $PORT_USAGE"
        else
            echo "✗ Puerto $port libre"
        fi
    done
    echo ""
}

# Función para verificar memoria
check_memory() {
    echo "3. VERIFICANDO MEMORIA:"
    echo "----------------------"
    
    MEMORY_INFO=$(free -h)
    echo "$MEMORY_INFO"
    
    # Verificar si hay poca memoria
    FREE_MEM=$(free | awk 'FNR==2{printf "%.0f", $7/1024/1024}')
    if [ "$FREE_MEM" -lt 2 ]; then
        echo "⚠️  ADVERTENCIA: Poca memoria libre (${FREE_MEM}GB)"
    else
        echo "✓ Memoria suficiente disponible (${FREE_MEM}GB)"
    fi
    echo ""
}

# Función para verificar archivos de log
check_logs() {
    echo "4. VERIFICANDO LOGS RECIENTES:"
    echo "-----------------------------"
    
    # Buscar archivos de log más recientes
    RECENT_LOGS=$(find results/ -name "*.log" -mtime -1 2>/dev/null | head -5)
    
    if [ ! -z "$RECENT_LOGS" ]; then
        echo "Logs recientes encontrados:"
        for log in $RECENT_LOGS; do
            echo "  - $log ($(stat -c %y "$log" | cut -d' ' -f1-2))"
            
            # Mostrar últimas líneas si hay errores
            ERRORS=$(tail -20 "$log" | grep -i "error\|exception\|timeout\|freeze\|hang")
            if [ ! -z "$ERRORS" ]; then
                echo "    ⚠️  ERRORES ENCONTRADOS:"
                echo "$ERRORS" | sed 's/^/      /'
            fi
        done
    else
        echo "✗ No se encontraron logs recientes"
    fi
    echo ""
}

# Función para limpiar procesos colgados
cleanup_processes() {
    echo "5. FUNCIÓN DE LIMPIEZA:"
    echo "----------------------"
    echo "¿Deseas terminar procesos colgados? (y/n)"
    read -r response
    
    if [[ "$response" =~ ^[Yy]$ ]]; then
        echo "Terminando procesos ML-Agents..."
        pkill -f mlagents-learn
        
        echo "Terminando procesos Unity colgados..."
        pkill -f Unity
        
        echo "✓ Procesos terminados"
    else
        echo "Saltando limpieza de procesos"
    fi
    echo ""
}

# Función para sugerencias
provide_suggestions() {
    echo "6. SUGERENCIAS PARA RESOLVER BLOQUEOS:"
    echo "======================================"
    echo ""
    echo "CONFIGURACIÓN CONSERVADORA:"
    echo "---------------------------"
    echo "• Usar config/sac/SoccerTwos_Stable.yaml"
    echo "• num_envs: 1 (un solo entorno)"
    echo "• time_scale: 10 (más conservador)"
    echo "• threaded: false (sin threading)"
    echo "• timeout_wait: 300 (timeout más largo)"
    echo ""
    
    echo "COMANDO RECOMENDADO:"
    echo "-------------------"
    echo "mlagents-learn config/sac/SoccerTwos_Stable.yaml --run-id=SAC_Stable_Test --force"
    echo ""
    
    echo "MONITOREO:"
    echo "----------"
    echo "• Usar 'htop' o 'top' para monitorear CPU/memoria"
    echo "• Verificar logs en tiempo real: tail -f results/[run-id]/[behavior].log"
    echo "• Si se bloquea de nuevo, usar Ctrl+C para terminar limpiamente"
    echo ""
    
    echo "ALTERNATIVAS:"
    echo "-------------"
    echo "• Considerar usar PPO en lugar de SAC (mucho más estable)"
    echo "• Comando PPO: mlagents-learn config/ppo/SoccerTwos.yaml --run-id=PPO_Test"
    echo "• PPO converge más rápido y es más estable para este tipo de entorno"
    echo ""
}

# Ejecutar todas las verificaciones
check_processes
check_ports
check_memory
check_logs
cleanup_processes
provide_suggestions

echo "=== DIAGNÓSTICO COMPLETADO ==="
