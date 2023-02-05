using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RMThresholds
{
    private static float _maxSafeRisk;

    private static int _currentAttemptsCount;
    private static int maxAttempts = 10;

    private static int _maxDepth = 1;

    private static float _minSearchRisk = 0.7f;
    private static float _maxSearchRisk = 0.99f;

    private static float _minPathRisk = 0.5f;
    private static float _maxPathRisk = 0.99f;

    public static void SetMaxSafeRisk(float maxSafeRisk)
    {
        _maxSafeRisk = maxSafeRisk;
    }


    public static float GetMaxRisk()
    {
        return _maxSearchRisk;
    }

    public static void ResetAttempts()
    {
        _currentAttemptsCount = 0;
    }

    public static void IncrementAttempts()
    {
        _currentAttemptsCount = Mathf.Min(_currentAttemptsCount + 1, maxAttempts);
    }


    public static int GetMaxAttempts()
    {
        return maxAttempts;
    }


    public static int GetSearchDepth(RiskThresholdType type)
    {
        switch (type)
        {
            case RiskThresholdType.Danger:
                return GetSearchDepth(RMRiskEvaluator.Instance.GetRisk());

            case RiskThresholdType.Attempts:
                return GetSearchDepth(_currentAttemptsCount);

            case RiskThresholdType.Binary:
                if (RMRiskEvaluator.Instance.GetRisk() > 0f)
                    return 2;
                else
                    return 1;

            case RiskThresholdType.Fixed:
                return 1;
        }

        return 1;
    }

    private static int GetSearchDepth(float risk)
    {
        int depth = Mathf.CeilToInt(_maxDepth * risk);
        return depth;
    }

    private static int GetSearchDepth(int attempts)
    {
        int depth = Mathf.CeilToInt(attempts / maxAttempts);
        return depth;
    }


    public static float GetMaxSearchRisk(RiskThresholdType type)
    {
        switch (type)
        {
            case RiskThresholdType.Danger:
                return GetMaxSearchRisk(RMRiskEvaluator.Instance.GetRisk());

            case RiskThresholdType.Attempts:
                return GetMaxSearchRisk(_currentAttemptsCount);

            case RiskThresholdType.Binary:
                if (RMRiskEvaluator.Instance.GetRisk() > 0f)
                    return _maxSearchRisk;
                else
                    return _minSearchRisk;

            case RiskThresholdType.Fixed:
                return _maxSafeRisk;
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
                return GetMaxPathRisk(_currentAttemptsCount);

            case RiskThresholdType.Binary:
                if (RMRiskEvaluator.Instance.GetRisk() > 0f)
                    return _maxPathRisk;
                else
                    return _minPathRisk;

            case RiskThresholdType.Fixed:
                return _maxSafeRisk;
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

    Binary,

    None
}