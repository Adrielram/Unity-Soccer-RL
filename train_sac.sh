#!/bin/bash

# Training script for SoccerTwos with SAC
# Make sure you have ml-agents package installed: pip install mlagents

echo "Starting SoccerTwos training with SAC algorithm..."
echo "Make sure your Unity environment is built and ready to connect."
echo ""

# Default parameters
CONFIG_FILE="config/sac/SoccerTwos.yaml"
RUN_ID="SoccerTwos_SAC_$(date +%Y%m%d_%H%M%S)"
ENV_PATH=""

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --config)
            CONFIG_FILE="$2"
            shift 2
            ;;
        --run-id)
            RUN_ID="$2"
            shift 2
            ;;
        --env)
            ENV_PATH="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  --config FILE    Configuration file (default: config/sac/SoccerTwos.yaml)"
            echo "  --run-id ID      Run identifier (default: SoccerTwos_SAC_timestamp)"
            echo "  --env PATH       Path to Unity environment executable (optional)"
            echo "  --help           Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Check if config file exists
if [ ! -f "$CONFIG_FILE" ]; then
    echo "Error: Configuration file not found: $CONFIG_FILE"
    exit 1
fi

echo "Configuration: $CONFIG_FILE"
echo "Run ID: $RUN_ID"

# Build the mlagents-learn command
COMMAND="mlagents-learn $CONFIG_FILE --run-id=$RUN_ID"

if [ ! -z "$ENV_PATH" ]; then
    COMMAND="$COMMAND --env=$ENV_PATH"
    echo "Environment: $ENV_PATH"
else
    # Try to use default build if it exists
    if [ -f "Project/Builds/UnityEnvironment.exe" ]; then
        COMMAND="$COMMAND --env=Project/Builds/UnityEnvironment.exe"
        echo "Environment: Project/Builds/UnityEnvironment.exe (auto-detected)"
    else
        echo "Environment: Unity Editor (manual start required)"
        echo "Make sure to press PLAY in Unity Editor after starting training"
    fi
fi

echo ""
echo "Running command:"
echo "$COMMAND"
echo ""

# Execute the training
exec $COMMAND
