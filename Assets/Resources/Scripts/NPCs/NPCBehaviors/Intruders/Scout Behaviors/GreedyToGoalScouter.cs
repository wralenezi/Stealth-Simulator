using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreedyToGoalScouter : Scouter
{
    public override void Begin()
    {
        foreach (var intruder in NpcsManager.Instance.GetIntruders())
            intruder.ClearIntruderGoal();
    }

    public override void Refresh(GameType gameType)
    {
        foreach (var intruder in NpcsManager.Instance.GetIntruders())
        {
            if (intruder.IsBusy()) return;

            Vector2? goal = GetDestination(gameType);

            if (!Equals(goal, null))
                intruder.SetDestination(goal.Value, true, false);
        }
    }
}
public class GreedyToGoalScouterParams : ScouterParams
{
}