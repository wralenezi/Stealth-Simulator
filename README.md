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

This scenario tests the ability of an agent to navigate through an environment unnoticed against patroling adversaries. https://dl.acm.org/doi/abs/10.1145/3561975.3562948


## How to use

1. Lunch the project through Unity.
2. Choose the scene "Main"
3. The main script "GameManager.cs" loads and runs the sessions in the function LoadSavedSessions().
4. Each session stores data about the length of the session, count of guards, behavior parameters,etc.
4. Click on play on Unity to lunch the scenario.

## Create custom sessions

You can define custom sessions to test different behaviors. You need to load the session into the list of sessions to be loaded in the game; they are loaded in GameManager.LoadSavedSessions(). 

You can create a session to define a scenario by using its class constructor which takes:

- _episodeLength: the length of the session
- _gameCode (string) : the code of the game (for logging purposes)
- _gameType (GameType): The game type, here each game type has different win or losing conditions
- pScenario (Scenario): The scenario of the session; determines how the game starts
- _guardColor (string): the color name of the guards
- _guardSpawnType (GuardSpawnType): How the guards will spawn
- pGuardsCount (int): The number of guards in the session
- _guardBehaviorParams (GuardBehaviorParams): the guards' behavior parameters; determines what set of behaviors they show
- pIntruderCount (int): number of intruders in the sessions; the current code is designed for 1 intruder
- _intruderBehaviorParams (IntruderBehaviorParams): Intruder parameters.
- _map (MapData): contains data of the map used in the session
- _speechType (SpeechType): the group of barks the guards use
- _surveyType (SurveyType): The survey displayed at the end of this


### Prefined scenarios

1. Dynamic Guard Patrol: Call the function "PatrolUserStudy.GetSessions()" to get the list of sessions to be used in "GameManager.LoadSavedSessions()". These sessions are mainly focused on testing guard patrol behaviors.
  
