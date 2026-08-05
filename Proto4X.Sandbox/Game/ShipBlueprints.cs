using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using Proto4X.Sandbox.Components;
using System;

namespace Proto4X.Sandbox.Game
{
    /// <summary>Cost and build time for each buildable ship, in one place so the HUD and the AI agree with the factory.</summary>
    public readonly record struct ShipBlueprint(int MineralCost, float BuildSeconds);

    /// <summary>
    /// Builds the <see cref="EntityBuilder"/> for every entity in the game. Component order matters:
    /// <c>IDependsOnOtherComponents</c> is checked at <c>With</c> time, so dependencies have to be
    /// added before the components that declare them.
    /// </summary>
    public static class ShipBlueprints
    {
        public static readonly ShipBlueprint Miner = new(MineralCost: 50, BuildSeconds: 6f);
        public static readonly ShipBlueprint Fighter = new(MineralCost: 75, BuildSeconds: 8f);

        public static ShipBlueprint For(ShipType shipType) => shipType switch
        {
            ShipType.Miner => Miner,
            ShipType.Fighter => Fighter,
            _ => throw new ArgumentOutOfRangeException(nameof(shipType), shipType, "Not a buildable ship type"),
        };

        public static EntityBuilder CreateMiner(Vector2 location, FactionId faction, int spriteId)
            => EntityBuilder.CreateEntity()
                .With(new Position(location))
                .With(new Motion(Vector2.Zero))
                .With(new MoveOrder())
                .With(new Faction(faction))
                .With(new Health(60f))
                .With(new Weapon(damagePerSecond: 0f, range: 0f))
                .With(new Selectable(pickRadius: 18f))
                .With(new Miner(capacity: 12f, miningRatePerSecond: 6f))
                .With(new Drawable(spriteId, null));

        public static EntityBuilder CreateFighter(Vector2 location, FactionId faction, int spriteId)
            => EntityBuilder.CreateEntity()
                .With(new Position(location))
                .With(new Motion(Vector2.Zero))
                .With(new MoveOrder())
                .With(new Faction(faction))
                .With(new Health(100f))
                .With(new Weapon(damagePerSecond: 14f, range: 150f))
                .With(new Selectable(pickRadius: 18f))
                .With(new Drawable(spriteId, null));

        public static EntityBuilder CreateStation(Vector2 location, FactionId faction, int spriteId)
            => EntityBuilder.CreateEntity()
                .With(new Position(location))
                .With(new Faction(faction))
                .With(new Health(900f))
                .With(new Timer(0, shouldRepeat: false))
                .With(new Weapon(damagePerSecond: 16f, range: 210f))
                .With(new Selectable(pickRadius: 34f))
                .With(new Station(rallyDistance: 70f))
                .With(new Drawable(spriteId, null));

        public static EntityBuilder CreateAsteroid(Vector2 location, int spriteId)
            => EntityBuilder.CreateEntity()
                .With(new Position(location))
                .With(new Gatherable(10))
                .With(new Drawable(spriteId, null));

        public static EntityBuilder CreateShip(ShipType shipType, Vector2 location, FactionId faction, SpriteSet sprites) => shipType switch
        {
            ShipType.Miner => CreateMiner(location, faction, sprites.MinerSpriteFor(faction)),
            ShipType.Fighter => CreateFighter(location, faction, sprites.FighterSpriteFor(faction)),
            _ => throw new ArgumentOutOfRangeException(nameof(shipType), shipType, "Not a buildable ship type"),
        };
    }
}
