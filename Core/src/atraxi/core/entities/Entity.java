package atraxi.core.entities;

import atraxi.core.Player;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.world.GridTile;

import java.math.BigDecimal;

public abstract class Entity
{
    private String type;
    private GridTile worldTile;
    private int orientation;
    private Player owner;
    
    public Entity(String type, Player owner, GridTile worldTile)
    {
        this.owner = owner;
        this.type = type;
        this.worldTile = worldTile;
    }
    
    public abstract boolean canAcceptAction(Action action);
    protected abstract void startActionFromQueue(Action action);

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
        return "["+type + " Pos:"+worldTile.getXIndex()+","+worldTile.getYIndex()+" Orientation:"+orientation+"]";
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


    public String getType()
    {
        return type;
    }

    public GridTile getWorldTile()
    {
        return worldTile;
    }

    public double getOrientationInRadians()
    {
        return orientation * Math.toRadians(60);
    }
}
