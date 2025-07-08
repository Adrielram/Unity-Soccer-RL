# SAC Training for SoccerTwos Environment

This guide explains how to train the SoccerTwos environment using the SAC (Soft Actor-Critic) algorithm.

## Files Created/Modified

### Configuration File
- `config/sac/SoccerTwos.yaml` - SAC-specific configuration for the soccer environment

### Modified Scripts
- `AgentSoccer.cs` - Updated to support both discrete (POCA/PPO) and continuous (SAC) action spaces
- `SoccerEnvController.cs` - Added null reference checks to prevent crashes
- `SoccerBallController.cs` - Added null reference checks for robust error handling

### Training Scripts
- `train_sac.sh` - Linux/Mac training script
- `train_sac.bat` - Windows training script

## Key Changes Made

### 1. Action Space Adaptation
The `AgentSoccer.cs` script now automatically detects whether to use discrete or continuous actions:
- **Discrete Actions (POCA/PPO)**: 3 discrete actions (forward/back, left/right, rotate)
- **Continuous Actions (SAC)**: 3 continuous actions with values between -1 and 1

### 2. SAC Configuration Parameters
The SAC configuration includes:
- **Learning Rate**: 0.0003 (standard for SAC)
- **Batch Size**: 256 (good balance for stability and performance)
- **Buffer Size**: 100,000 (experience replay buffer)
- **Tau**: 0.005 (soft update rate for target networks)
- **Init Entropy Coefficient**: 0.1 (encourages exploration)

### 3. Self-Play Configuration
Maintained self-play settings similar to POCA:
- Team change every 200,000 steps
- Window size of 15 for opponent selection
- 50% chance to play against latest model

## How to Use

### Prerequisites
1. Install ML-Agents: `pip install mlagents`
2. Make sure Unity project is set up with the modified scripts

### Method 1: Using Training Scripts

#### Windows:
```cmd
train_sac.bat
```

#### Linux/Mac:
```bash
chmod +x train_sac.sh
./train_sac.sh
```

#### Custom Parameters:
```cmd
# Windows
train_sac.bat --run-id MyCustomRun --config config\sac\SoccerTwos.yaml

# Linux/Mac
./train_sac.sh --run-id MyCustomRun --config config/sac/SoccerTwos.yaml
```

### Method 2: Direct ML-Agents Command
```bash
mlagents-learn config/sac/SoccerTwos.yaml --run-id=SoccerTwos_SAC
```

### Method 3: Training with Built Environment
If you have a built Unity environment:
```bash
mlagents-learn config/sac/SoccerTwos.yaml --run-id=SoccerTwos_SAC --env=path/to/your/SoccerEnvironment.exe
```

## Unity Setup

### For SAC Training:
1. Open the SoccerTwos scene in Unity
2. Select the agents in the scene
3. In the Behavior Parameters component:
   - Set **Vector Action Space Type** to **Continuous**
   - Set **Vector Action Space Size** to **3**
   - Set **Behavior Name** to **SoccerTwos** (must match YAML config)

### For POCA/PPO Training:
1. Select the agents in the scene
2. In the Behavior Parameters component:
   - Set **Vector Action Space Type** to **Discrete**
   - Set **Branches Size** to **3**
   - Set each branch size to **3**
   - Set **Behavior Name** to **SoccerTwos**

## Training Tips

### SAC-Specific Considerations:
1. **Longer Training Time**: SAC typically requires more steps to converge than POCA
2. **Exploration**: The continuous action space allows for more nuanced movements
3. **Stability**: SAC is generally more stable but may learn slower initially

### Monitoring Training:
1. Use TensorBoard to monitor training progress:
   ```bash
   tensorboard --logdir results
   ```
2. Key metrics to watch:
   - **Cumulative Reward**: Should increase over time
   - **Episode Length**: Should stabilize
   - **Policy Loss**: Should decrease and stabilize
   - **Value Loss**: Should decrease

### Hyperparameter Tuning:
If training is not progressing well, try adjusting:
- **Learning Rate**: Decrease to 0.0001 for more stability
- **Batch Size**: Increase to 512 for more stable updates
- **Buffer Size**: Increase to 200,000 for more diverse experience
- **Entropy Coefficient**: Increase for more exploration

## Troubleshooting

### Common Issues:
1. **NullReferenceException**: Fixed with the null checks added to the scripts
2. **Action Space Mismatch**: Make sure Unity's Behavior Parameters match the training algorithm
3. **Training Not Starting**: Verify the config file path and Unity connection

### Performance Tips:
1. **Graphics Settings**: Use `no_graphics: true` in the config for faster training
2. **Time Scale**: Increase `time_scale` to 20-50 for faster simulation
3. **Multiple Environments**: Use `num_envs` > 1 for parallel training

## Results Location
Training results will be saved in:
- `results/[run-id]/` - Contains models, configuration, and logs
- Models are saved as `[behavior-name].onnx` files

## Comparing Algorithms
You can compare SAC performance with POCA by:
1. Training with SAC using this configuration
2. Training with POCA using `config/poca/SoccerTwos.yaml`
3. Comparing results in TensorBoard or by testing the final models
