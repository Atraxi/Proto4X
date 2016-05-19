package atraxi.core.world;

import atraxi.core.entities.Entity;

/**
 * Created by Atraxi on 19/05/2016.
 */
public class GridTile
{
    private int xIndex, yIndex;
    private Entity entity;

    public GridTile(int xIndex, int yIndex)
    {
        this.xIndex = xIndex;
        this.yIndex = yIndex;
    }

    public Entity getEntity()
    {
        return entity;
    }

    public int getXIndex()
    {
        return xIndex;
    }

    public int getYIndex()
    {
        return yIndex;
    }
}