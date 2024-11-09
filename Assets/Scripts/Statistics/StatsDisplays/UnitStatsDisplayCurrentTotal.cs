public class UnitStatsDisplayCurrentTotal : UnitStatsDisplay
{
    protected override int GetTextValue(int rawValue)
    {
        return rawValue;
    }

    protected override void OnUpdate()
    {
    }
}