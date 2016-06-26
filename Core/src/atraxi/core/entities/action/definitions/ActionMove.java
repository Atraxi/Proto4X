package atraxi.core.entities.action.definitions;

import atraxi.core.entities.Entity;

import java.awt.Point;

/**
 * Created by Atraxi on 2/05/2016.
 */
public abstract class ActionMove extends Action
{
    protected Point target;
    public ActionMove(Entity source, Point target)
    {
        super(source);
        this.target = target;
    }
}
