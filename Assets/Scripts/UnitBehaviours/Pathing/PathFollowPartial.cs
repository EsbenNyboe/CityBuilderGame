public partial struct PathFollow
{
    public readonly bool IsMoving()
    {
        return PathIndex >= 0;
    }
}