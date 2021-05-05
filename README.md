# Stealth-Simulator

This is a framework that servers to create and study different NPC behaviors useful for stealth/action games.

## Installation

1. [Unity](https://unity.com/)
2. [ML-agents 1.7.0 or higher.](https://github.com/Unity-Technologies/ml-agents/blob/release_17_docs/docs/Readme.md)

## Scenarios

In this framework, we defined several scenarios to study the corresponding behaviors. These scenarios are:

### 1. Guards search

This scenario starts with an intruder is in the field of view (FOV) of a guard. The intruder is tasked with getting out of the guards' FOV and stay hidden. However, once the intruder is out of sight, the guards start the search behavior. Their goal is regain visibility of the intruder.



## How to use

1. Lunch the project through Unity.
2. Choose the scene "Main"
3. The scenario is created as a dictionary formed in "GameManager.cs" in EnumerateSessionData(). You can set up the session which is formed by putting different elements that makes a scenario such as, the map, number of intruders, number of guards,etc.
4. Click on play on Unity to lunch the scenario.
