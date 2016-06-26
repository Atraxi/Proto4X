package atraxi.core.entities;

import atraxi.core.Player;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.util.Globals;

import java.awt.Point;
import java.math.BigDecimal;

public abstract class Entity
{
    private Globals.Identifiers type;
    private Point location;
    private int orientation;
    private Player owner;
    
    public Entity(Globals.Identifiers type, Player owner, Point location)
    {
        this.owner = owner;
        this.type = type;
        this.location = location;
    }

    public Entity(Entity entity)
    {
        this.owner = entity.owner;
        this.type = entity.type;
        this.location = entity.location;
        this.orientation = entity.orientation;
    }

    public abstract boolean canAcceptAction(Action action);
    protected abstract void startActionFromQueue(Action action);

    /**
     * Attempt to find a path to the given location
     * @param location The location to path towards
     * @return The distance to the target location, or -1 if the target cannot be reached
     */
    public int pathToLocation(Point location)
    {
        return -1;
    }

    public void doWork(BigDecimal timeAdjustment, boolean paused)
    {
//        if(actionInProgress == null && !actionQueue.isEmpty())
//        {
//            Action action = actionQueue.popAction();
//            startActionFromQueue(action);
//        }
    }
    
    @Override
    public String toString()
    {
        return "[" + type + " Pos:" + location.x + "," + location.y + " Orientation:" + orientation + "]";
    }

//    public void queueAction(Action action)
//    {
//        actionQueue.queueAction(action);
//    }
//
//    public void replaceQueue(Action action)
//    {
//        actionQueue.replaceQueue(action);
//        synchronized(actionInProgressLock)
//        {
//            actionInProgress = null;
//        }
//    }


    public Globals.Identifiers getType()
    {
        return type;
    }

    public Point getLocation()
    {
        return location;
    }

    public double getOrientationInRadians()
    {
        return orientation * Math.toRadians(60);
    }
}
