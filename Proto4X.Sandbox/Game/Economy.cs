using Proto4X.Sandbox.Components;
using System;

namespace Proto4X.Sandbox.Game
{
    /// <summary>
    /// Per-faction mineral stockpile. Not a component: it belongs to a faction, not an entity, and
    /// nothing about it wants to be iterated. Shared by reference between the systems that earn
    /// minerals (MinerSystem) and the ones that spend them (BuildCommandSystem, EnemyAiSystem).
    /// </summary>
    public class Economy
    {
        private readonly float[] mineralsByFaction = new float[Enum.GetValues<FactionId>().Length];

        public Economy(int startingMinerals)
        {
            mineralsByFaction[(int)FactionId.Player] = startingMinerals;
            mineralsByFaction[(int)FactionId.Enemy] = startingMinerals;
        }

        public int GetMinerals(FactionId faction) => (int)mineralsByFaction[(int)faction];

        public void Deposit(FactionId faction, float amount) => mineralsByFaction[(int)faction] += amount;

        public bool TrySpend(FactionId faction, int cost)
        {
            if (mineralsByFaction[(int)faction] < cost)
            {
                return false;
            }

            mineralsByFaction[(int)faction] -= cost;
            return true;
        }
    }
}
