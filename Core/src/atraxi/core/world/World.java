package atraxi.core.world;

import atraxi.core.entities.Entity;

import java.awt.Point;

public class World
{
    public final long seed;
    protected Entity[][] entities;
    private int sizeX, sizeY;

    /**
     * Instantiates a new game world. Server side only, UI and rendering values are not set
     * @param seed
     * @param sizeX
     * @param sizeY
     */
    public World(long seed, int sizeX, int sizeY)
    {
        this.seed = seed;
        this.sizeX = sizeX;
        this.sizeY = sizeY;

        entities = new Entity[sizeX][sizeY];
    }

    public World(World world)
    {
        this.seed = world.seed;
        this.sizeX = world.sizeX;
        this.sizeY = world.sizeY;
        this.entities = world.entities;
    }

    /**
     * @return The number of rows for this world
     */
    public int getSizeY()
    {
        return sizeY;
    }

    /**
     * @return The number of columns wide for this world
     */
    public int getSizeX()
    {
        return sizeX;
    }

    public Entity getEntityAtIndex(int x, int y)
    {
        return entities[x][y];
    }

    public Entity getEntityAtIndex(Point gridTileIndex)
    {
        return entities[gridTileIndex.x][gridTileIndex.y];
    }
}