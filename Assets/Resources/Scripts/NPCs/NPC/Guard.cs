using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class Guard : NPC
{
    private float _overlapTime;
    private float _overlapDistance = 1f;

    private GameObject m_excMarkPrefab;
    private GameObject m_excMarkGo;

    // Guard's role assigned by the manager; default is in patrol
    public GuardRole role = GuardRole.Patrol;

    //************ Guard's Vision *****************//

    // time spent in a guard's FOV to be discovered
    public float _timeInFov;
    private const float _maxTimeInFov = 0.2f;
    
    public AnimationCurve _spottedCurve;

    // if this is one then the intruder is spotted
    private float _spottedFactor;
    private Color32 _spottedColor;

    // Initialize the guard
    public override void Initiate(Session session, NpcData data, VoiceParams _voice)
    {
        base.Initiate(session, data, _voice);

        AddFoV(Properties.GetFovAngle(Data.npcType), session.guardFov,
            Properties.GetFovColor(Data.npcType));

        m_excMarkPrefab = (GameObject)Resources.Load("Prefabs/exclamation_mark");
        m_excMarkGo = Instantiate(m_excMarkPrefab, transform);
        m_excMarkGo.AddComponent<SeparateRotator>();
        m_excMarkGo.SetActive(false);

        _timeInFov = 0;
        _spottedCurve = new AnimationCurve();
        _spottedCurve.AddKey(0f, 0f);
        _spottedCurve.AddKey(1f, 1f);
        _spottedColor = new Color32(255, 0, 0, 100);

        // Add guard label
        GameObject labelOg = new GameObject();

        labelOg.name = "Label";
        labelOg.transform.parent = transform;
        labelOg.AddComponent<SeparateRotator>();
        TextMeshPro textMeshPro = labelOg.AddComponent<TextMeshPro>();
        textMeshPro.text = gameObject.name.Replace("Guard0", "");
        textMeshPro.alignment = TextAlignmentOptions.Center;
        textMeshPro.fontSize = 4;
        textMeshPro.sortingOrder = 5;
        textMeshPro.color = Color.black;
    }

    public override void ResetNpc()
    {
        base.ResetNpc();
        _overlapTime = 0f;
        ClearGoal();
    }

    // Clear the lines the guard planned to go through
    public override void ClearLines()
    {
        while (LinesToPassThrough.Count > 0)
        {
            RoadMapLine current = LinesToPassThrough[0];
            current.RemovePassingGuard(this);
            LinesToPassThrough.RemoveAt(0);
        }
    }

    // Check if any intruder is spotted, return true if at least one is spotted
    public bool SpotIntruders(List<Intruder> intruders)
    {
        foreach (var intruder in intruders)
        {
            bool isIntruderInFov = GetFovPolygon().IsCircleInPolygon(intruder.transform.position, 0.03f);

            ModifyTimeInFOV(isIntruderInFov ? Time.deltaTime : -Time.deltaTime);

            // Check if the intruder is seen
            if (SpottedIntruder())
            {
                intruder.Seen();
                WorldState.Set(name + "_see_" + intruder.name, true.ToString());
                WorldState.Set("last_time_" + name + "_saw_" + intruder.name, Time.time.ToString());
                RenderIntruder(intruder, true);

                // skip if the intruder is in ghost mode
                if (!intruder.isGhost)
                {
                    ShowExclamation();
                    return true;
                }
            }

            // Intruder not seen
            WorldState.Set(name + "_see_" + intruder.name, false.ToString());
            RenderIntruder(intruder, false);
        }


        m_excMarkGo.SetActive(false);
        return false;
    }

    public void ModifyTimeInFOV(float timeDelta)
    {
        _timeInFov += timeDelta;
        _timeInFov = Mathf.Clamp(_timeInFov, 0f, _maxTimeInFov);
    }

    public bool SpottedIntruder()
    {
        _spottedFactor = _timeInFov / _maxTimeInFov;
        float value = _spottedCurve.Evaluate(_spottedFactor);
        SetFovColor(Color32.Lerp(Properties.GetFovColor(NpcType.Guard), _spottedColor, value));
        return value == 1f;
    }

    public override void UpdateMetrics(float timeDelta)
    {
        if (IsGuardOverlapping())
        {
            _overlapTime += timeDelta;
        }
    }

    private void ShowExclamation()
    {
        m_excMarkGo.SetActive(true);
        m_excMarkGo.transform.position = (Vector2)GetTransform().position + Vector2.up * 0.7f;
    }


    // Rendering the intruder
    public void RenderIntruder(Intruder intruder, bool seen)
    {
        if (GameManager.Instance.gameView == GameView.Spectator)
        {
            intruder.RenderIntruder(true);
            intruder.RenderIntruderFov(true);
            RenderGuard(true);
        }
        else if (GameManager.Instance.gameView == GameView.Guard)
        {
            RenderGuard(true);
            if (seen)
                intruder.RenderIntruder(true);
            else
                intruder.RenderIntruder(false);

            intruder.RenderIntruderFov(false);
        }
    }

    // Render the guard and the FoV if seen by the intruder
    public void RenderGuard(bool isSeen)
    {
        Renderer.enabled = isSeen;
        FovRenderer.enabled = isSeen;
    }

    // Check if a point is in the FoV
    public bool IsPointInFoV(Vector2 point)
    {
        bool isIn = GetFovPolygon().IsPointInPolygon(point, true);
        return isIn;
    }

    private bool IsGuardOverlapping()
    {
        float closestGuardDistance = Mathf.Infinity;
        foreach (var guard in NpcsManager.Instance.GetGuards())
        {
            if (Equals(guard, this)) continue;

            // float distance = PathFinding.Instance.GetShortestPathDistance(GetTransform().position, guard.GetTransform().position);
            float sqrMag = Vector2.SqrMagnitude(GetTransform().position - guard.GetTransform().position);

            if (sqrMag < closestGuardDistance) closestGuardDistance = sqrMag;
        }

        return closestGuardDistance <= _overlapDistance;
    }

    public override LogSnapshot LogNpcProgress()
    {
        return new LogSnapshot(GetTravelledDistance(), StealthArea.GetElapsedTimeInSeconds(), Data, "", 0, _overlapTime,
            0f, 0f,
            0, 0f, 0);
    }


    public override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
    }
}