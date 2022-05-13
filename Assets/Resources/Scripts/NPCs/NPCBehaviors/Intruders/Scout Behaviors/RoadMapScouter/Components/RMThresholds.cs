using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RMThresholds
{
    private static int maxAttempts = 10;

    private static float _minSearchRisk = 0.5f;
    private static float _maxSearchRisk = 099f;

    private static float _minPathRisk = 0.1f;
    private static float _maxPathRisk = 0.99f;

    public static int GetMaxAttempts()
    {
        return maxAttempts;
    }

    public static float GetMaxSearchRisk(RiskThresholdType type)
    {
        switch (type)
        {
            case RiskThresholdType.Danger:
                return GetMaxSearchRisk(RMRiskEvaluator.Instance.GetRisk());

            case RiskThresholdType.Attempts:
                return GetMaxSearchRisk(RoadMapScouter.GetAttemptsCount());
            
            case RiskThresholdType.Binary:
                if (RMRiskEvaluator.Instance.GetRisk() > 0f)
                    return _maxSearchRisk;
                else
                    return _minSearchRisk;

            case RiskThresholdType.Fixed:
                return 0.5f;
        }

        return 0.5f;
    }

    public static float GetMaxPathRisk(RiskThresholdType type)
    {
        switch (type)
        {
            case RiskThresholdType.Danger:
                return GetMaxPathRisk(RMRiskEvaluator.Instance.GetRisk());

            case RiskThresholdType.Attempts:
                return GetMaxPathRisk(RoadMapScouter.GetAttemptsCount());

            case RiskThresholdType.Binary:
                if (RMRiskEvaluator.Instance.GetRisk() > 0f)
                    return _maxPathRisk;
                else
                    return _minPathRisk;
            
            case RiskThresholdType.Fixed:
                return 0.5f;
        }

        return 0.5f;
    }


    private static float GetMaxSearchRisk(float intruderRisk)
    {
        return Mathf.Clamp(intruderRisk, _minSearchRisk, _maxSearchRisk);
    }

    private static float GetMaxSearchRisk(int attemptNumber)
    {
        float normalizedAttempts = (float) attemptNumber / maxAttempts;
        return Mathf.Clamp(normalizedAttempts, _minSearchRisk, _maxSearchRisk);
    }

    private static float GetMaxPathRisk(float intruderRisk)
    {
        return Mathf.Clamp(intruderRisk, _minPathRisk, _maxPathRisk);
    }

    private static float GetMaxPathRisk(int attemptNumber)
    {
        float normalizedAttempts = (float) attemptNumber / maxAttempts;
        return Mathf.Clamp(normalizedAttempts, _minPathRisk, _maxPathRisk);
    }
}


public enum RiskThresholdType
{
    Fixed,

    Danger,

    Attempts,

    Binary
}