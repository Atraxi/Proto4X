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

    public GridTile(GridTile gridTile)
    {
        this.entity = gridTile.entity;
        this.xIndex = gridTile.xIndex;
        this.yIndex = gridTile.yIndex;
    }

    public Entity getEntity()
    {
        return entity;
    }

    public void setEntity(Entity entity)
    {
        this.entity = entity;
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