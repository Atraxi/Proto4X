package atraxi.core.world;

public class World
{
    public final long seed;
    protected GridTile[][] tiles;
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

        tiles = new GridTile[sizeX][sizeY];
        for(int x = 0; x < tiles.length; x++)
        {
            for(int y = 0; y < tiles[x].length; y++)
            {
                tiles[x][y] = new GridTile(x, y);
            }
        }
    }

    public World(World world)
    {
        this.seed = world.seed;
        this.sizeX = world.sizeX;
        this.sizeY = world.sizeY;
        this.tiles = world.tiles;
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

    public GridTile getGridTileAtIndex(int x, int y)
    {
        return tiles[x][y];
    }
}