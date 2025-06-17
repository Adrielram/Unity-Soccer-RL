#!/usr/bin/env python3
from mlagents.trainers.demo_loader import load_demonstration
from mlagents_envs.environment import UnityEnvironment
import numpy as np
import time

def main():
    demo_path = "D:/UnityProjects/Unity-Soccer-RL/Project/demos/aguspegay.demo"
    unity_exe = "D:/UnityProjects/Unity-Soccer-RL/Project/Builds/UnityEnvironment.exe"  # tu build de Unity
    #demo_data, metadata = load_demonstration(demo_path)
    print (load_demonstration(demo_path))
    print(f"Reproduciendo: {metadata['behavior_name']} con {metadata['number_steps']} pasos")

    env = UnityEnvironment(file_name=unity_exe, no_graphics=False)
    env.reset()

    behavior = metadata["behavior_name"]
    spec = env.behavior_specs[behavior]

    for step in demo_data:
        decision_steps, terminal_steps = env.get_steps(behavior)

        # Crea estructura de acción
        if spec.is_action_discrete():
            arr = np.array([step.action], dtype=np.int32)
            action_tuple = spec.action_spec.empty_action(len(decision_steps))
            action_tuple.discrete = arr
        else:
            arr = np.array([step.action], dtype=np.float32)
            action_tuple = spec.action_spec.empty_action(len(decision_steps))
            action_tuple.continuous = arr

        env.set_actions(behavior, action_tuple)
        env.step()
        time.sleep(0.02)

    print("Reproducción finalizada.")
    env.close()

if __name__ == "__main__":
    main()