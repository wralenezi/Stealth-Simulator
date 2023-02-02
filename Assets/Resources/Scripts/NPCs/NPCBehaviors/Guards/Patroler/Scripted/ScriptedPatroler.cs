using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class ScriptedPatroler : Patroler
{
    private PatrolPlan plan;

    public override void Initiate(MapManager mapManager, GuardBehaviorParams guardParams)
    {
        LoadPatrolPaths(mapManager);
    }

    public void LoadPatrolPaths(MapManager mapManager)
    {
        string patrolPathsJson =
            CsvController.ReadString(GameManager.PatrolPathsPath + mapManager.mapData.name + ".json");

        plan = JsonConvert.DeserializeObject<PatrolPlan>(patrolPathsJson);
    }

    public override void Start()
    {
        LoadPatrolPaths(MapManager.Instance);
    }

    public override void UpdatePatroler(List<Guard> guards, float speed, float timeDelta)
    {
    }

    public override void Patrol(List<Guard> guards)
    {
        foreach (var guard in guards)
        {
            if (guard.IsBusy()) continue;

            PatrolStep step = plan.patrols[guard.GetNpcData().id - 1].GetCurrentPatrolStep(guard);

            if (Equals(step, null)) continue;

            guard.SetDestination(new Vector2(step.position.x, step.position.y), true, false);
            guard.SetDirection(new Vector2(step.direction.x, step.direction.y));
        }
    }
}

public class ScriptedPatrolerParams : PatrolerParams
{
}


[Serializable]
public class PatrolPlan
{
    public List<PatrolPath> patrols;
}

[Serializable]
public class PatrolPath
{
    public int id;
    public List<PatrolStep> path;

    private PatrolStep _currentStep;
    private int _index = 0;

    // timestamp when the current step was chosen
    private float _timestampCurrentStep;


    public PatrolStep GetCurrentPatrolStep(Guard guard)
    {
        if (!Equals(_currentStep, null))
        {
            if (!_currentStep.IsReached)
            {
                float distance = Vector2.Distance(guard.GetTransform().position,
                    new Vector2(_currentStep.position.x, _currentStep.position.y));

                if (distance >= 0.1f) return null;

                _currentStep.IsReached = true;
                _timestampCurrentStep = GameManager.GetDateTimestamp();

                return null;
            }

            float elapsedTime = GameManager.GetDateTimestamp() - _timestampCurrentStep;

            if (elapsedTime < _currentStep.duration)
                return null;

            _index = (_index + 1) % path.Count;
            _currentStep = null;
        }

        PatrolStep patrolStep = path[_index];
        _currentStep = patrolStep;
        _currentStep.IsReached = false;

        return _currentStep;
    }
}

[Serializable]
public class PatrolStep
{
    // The position of the patrol position
    public Position2D position;

    // The direction of the guard at this step
    public Position2D direction;

    // The duration the guard will stay in this step.
    public float duration;

    // flag if the patrol step has been reached by the guard
    public bool IsReached;
}