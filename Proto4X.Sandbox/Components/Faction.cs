namespace Proto4X.Sandbox.Components
{
    public enum FactionId
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2,
    }

    public struct Faction(FactionId id)
    {
        public FactionId Id = id;
    }
}
