package atraxi.core.entities.action.definitions;

import atraxi.core.entities.Entity;
import atraxi.core.world.GridTile;

/**
 * Created by Atraxi on 2/05/2016.
 */
public abstract class ActionMove extends Action
{
    protected GridTile target;
    public ActionMove(Entity source, GridTile target)
    {
        super(source);
        this.target = target;
    }
}
