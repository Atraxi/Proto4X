using Microsoft.Xna.Framework;
using Proto4X.Sandbox.Components;

namespace Proto4X.Sandbox.Game
{
    /// <summary>
    /// A ship a station has finished building, waiting to be turned into a real entity.
    /// </summary>
    /// <remarks>
    /// Systems can't add entities - <c>ArchetypeChunk</c> deliberately exposes removal only, since
    /// blind addition through the chunk's own archetype would produce a wrongly shaped entity (see
    /// "No way to spawn or destroy an entity from inside a running system" in ENGINE_REQUESTS.md).
    /// So ProductionSystem describes what it wants built and <c>Sandbox.OnUpdate</c> drains the
    /// list through <c>GameWorld.AddEntity</c>. This works out fine here precisely because nothing
    /// needs the new ship's id afterwards.
    /// </remarks>
    public readonly record struct SpawnRequest(ShipType ShipType, FactionId Faction, Vector2 Location);
}
