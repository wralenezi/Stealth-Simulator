public class SimpleGreedyScouter : Scouter
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

            _HsC.AssignHidingSpotsFitness(NpcsManager.Instance.GetGuards());
            intruder.SetDestination(_HsC.GetBestHidingSpot().Value, true, false);
            StartCoroutine(intruder.waitThenMove(_HsC.GetBestHidingSpot().Value));
        }
    }
}

public class SimpleGreedyScouterParams : ScouterParams
{
    public override string ToString()
    {
        string output = "";

        output += GetType();

        return output;
    }
}