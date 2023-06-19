using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionSetup : MonoBehaviour
{
    [SerializeField]
    private Session _session;

    private PatrolerParams _patrolerParams;
    private SearcherParams _searcherParams;
    private SearchEvaderParams _searchEvader;

    private float length = 120f;

    private int _guardCount;
    private MapData _map;

    public void AssignPatrolBehavior(PatrolerParams patrolerParams)
    {
        _patrolerParams = patrolerParams;
    }

    public void AssignSearchBehavior(SearcherParams searcherParams)
    {
        _searcherParams = searcherParams;
    }

    public void SetMapGuards(MapData map, int guardCount)
    {
        _map = map;
        _guardCount = guardCount;
    }

    public void CreateSession()
    {
        GuardBehaviorParams guardBehaviorParams = new GuardBehaviorParams(_patrolerParams,
            _searcherParams, null);

        IntruderBehaviorParams intruderBehaviorParams =
            new IntruderBehaviorParams(null, _searchEvader, null);

        _session = new Session(length, "", GameType.CoinCollection, Scenario.Stealth,
            "blue",
            GuardSpawnType.Separate, _guardCount, 0.1f, guardBehaviorParams, 1,
            0.1f, intruderBehaviorParams, _map, SpeechType.Simple, SurveyType.EndEpisode);

        _session.coinCount = 1;
        _session.MinScore = Mathf.NegativeInfinity;
        _session.MaxScore = Mathf.Infinity;

        // Add guards
        for (int i = 0; i < _session.guardsCount; i++)
            _session.AddNpc(i + 1, NpcType.Guard, null);

        for (int i = 0; i < _session.intruderCount; i++)
            _session.AddNpc(i + 1, NpcType.Intruder, null, true);
    }


    public Session GetSession()
    {
        CreateSession();
        return _session;
    }
}