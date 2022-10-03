# Stealth-Simulator

This is a framework that servers to create and study different NPC behaviors useful for stealth/action games.

## Installation 

1. Download and install the game engine [Unity](https://unity.com/).
2. Clone this project.

## Scenarios

In this framework, we defined several scenarios to study the corresponding behaviors. These scenarios are:

### 1. Guard Patrol Behavior

This scenario allows us to study how guards can performe patrol routines to secure a certain enclosed area. Example of our research in this scenarion can be found here: https://ojs.aaai.org/index.php/AIIDE/article/view/7425/7308

### 2. Guard Search Behavior


This scenario tests how guards can track and search for an opposing agent after the line of sight is broken. It starts with an intruder is in the field of view (FOV) of a guard. The intruder is tasked with getting out of the guards' FOV and stay hidden. However, once the intruder is out of sight, the guards start the search behavior. Their goal is regain visibility of the intruder. Example of our research can be found here: https://ieeexplore.ieee.org/abstract/document/9619054


### 3. Stealth Agent Pathfinding

This scenario tests the ability of an agent to navigate through an environment unnoticed against patroling adversaries


## How to use

1. Lunch the project through Unity.
2. Choose the scene "Main"
3. The main script "GameManager.cs" loads and runs the sessions in the function LoadSavedSessions().
4. Each session stores data about the length of the session, count of guards, behavior parameters,etc.
4. Click on play on Unity to lunch the scenario.
