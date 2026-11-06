using Microsoft.Xna.Framework;

// The space layer's data model (plans/03, pillar 5). Like the factory's components: small blittable
// structs, no managed references. A ship's design is referenced by a key into SpaceLayer.Designs.
namespace Proto4X.Factory.Simulation.Space
{
    public enum Faction : byte
    {
        Player,
        Raider,
    }

    public enum BodyRole : byte
    {
        Ship,
        /// <summary>The player's orbital station: lose when it dies.</summary>
        Station,
        /// <summary>The raiders' base: win when it dies.</summary>
        Bastion,
    }

    public enum HelmOrder : byte
    {
        /// <summary>Hold the post; engage hostiles that come within aggro range, then return (AoE defensive stance).</summary>
        Idle,
        /// <summary>Fly to the target, ignoring enemies, then hold there.</summary>
        Move,
    }

    public struct SpaceShip
    {
        /// <summary>Stable name for orders and targeting. Assigned at spawn, never reused.</summary>
        public int Serial;
        public Faction Faction;
        public BodyRole Role;

        /// <summary>Index into <see cref="SpaceLayer.Designs"/>, or -1 for station and bastion.</summary>
        public int DesignKey;
        public float Hp;
        public float MaxHp;
    }

    public struct Body
    {
        public Vector2 Position;
        public Vector2 Velocity;

        /// <summary>Radians clockwise; the nose (local north) points along (sin r, -cos r).</summary>
        public float Rotation;
        public float AngularVelocity;
    }

    /// <summary>Flight numbers baked from the design's stats at spawn (tiles/s², rad/s², rad/s).</summary>
    public struct Drive
    {
        public float Forward;
        public float Reverse;
        public float Right;
        public float Left;
        public float Angular;
        public float MaxTurnRate;
    }

    public struct Helm
    {
        public HelmOrder Order;
        public Vector2 Target;

        /// <summary>Where an idle ship returns to after a fight.</summary>
        public Vector2 Post;

        /// <summary>Serial being engaged, or -1.</summary>
        public int Engaging;
    }

    public struct Gunnery
    {
        public float Dps;
        public float Range;
        public int Target;
        public Vector2 TargetPosition;
        public bool Firing;
    }
}
