package atraxi.core.world;

import atraxi.core.entities.Entity;

import java.awt.Point;
import java.util.TreeMap;

public class World
{
    public final long seed;
    protected TreeMap<Long, Entity> entities;
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

        entities = new TreeMap<>();
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
        return entities.get(convertCoordinateToKey(x, y));
    }

    public Entity getEntityAtIndex(Point gridTileIndex)
    {
        return getEntityAtIndex(gridTileIndex.x, gridTileIndex.y);
    }

    protected long convertCoordinateToKey(int x, int y)
    {
        return ((long)x << 32L) + (long)y;
    }

    public static Point convertAxialToOffset(int x, int y)
    {
        return new Point(x + (y - (y&1)) / 2,
                         y);
    }

    public static Point convertAxialToOffset(Point point)
    {
        point.setLocation(point.x + (point.y - (point.y&1)) / 2,
                          point.y);
        return point;
    }

    public static Point convertOffsetToAxial(Point point)
    {
        point.setLocation(point.x - (point.y - (point.y&1)) / 2,
                          point.y);
        return point;
    }
}