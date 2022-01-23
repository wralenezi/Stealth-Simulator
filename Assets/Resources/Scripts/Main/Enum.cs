// List of enums used in the game

// Struct for the NPC planners

using UnityEngine;

public struct Behavior
{
    public PatrolPlanner patrol;
    public AlertPlanner alert;
    public SearchPlanner search;
    public PlanOutput searchFormat;

    public Behavior(PatrolPlanner _patrol, AlertPlanner _alert, SearchPlanner _search,
        PlanOutput _searchFormat)
    {
        patrol = _patrol;
        alert = _alert;
        search = _search;
        searchFormat = _searchFormat;
    }
}

// Guard decision maker for patrol situations
public enum PatrolPlanner
{
    gStalest,
    iSimple,
    Random,
    UserInput
}


// Guard decision maker for chasing an intruder
public enum AlertPlanner
{
    Simple,
    Intercepting,
    UserInput,
    RmPropSimple,
    iHeuristic
}

// Decision maker for search phase
public enum SearchPlanner
{
    // Randomly traverse the nodes of the Abstraction graph
    Random,
    
    iHeuristic,

    // The guards search the road map while propagating the probability of the intruder's presence.
    // The probability is diffused similarly to Damian Isla's implementation
    RmPropOccupancyDiffusal,

    // The probability is simply propagated through the road map.
    RmPropSimple,

    // The guards know the intruder's position at all times.
    Cheating,
    
    UserInput
}

// Intruder behavior 
public enum IntruderPlanner
{
    Random,
    RandomMoving,
    UserInput,
    Heuristic,
    HeuristicMoving
}

// Heuristic for path finding 
public enum PathFindingHeursitic
{
    EuclideanDst
}

// Path following algorithm
public enum PathFollowing
{
    SimpleFunnel
}


public enum NpcType
{
    Guard,
    Intruder
}

public enum SpeechType
{
    None,
    Simple,
    Full
}


// The format of the plan generate
public enum PlanOutput
{
    // This is a point, where the npc simply find the shortest path to
    Point,

    // This is greedy hill climbing path where the npc follows. 
    HillClimbPath,

    // This is a path where the guard follows dijkstra's algorithm; taking the path with the highest average utility
    DijkstraPath,

    // This is a path where the guard follows dijkstra's algorithm; taking the path with the highest segment utility
    DijkstraPathMax
}
