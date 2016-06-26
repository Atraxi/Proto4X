package atraxi.core.entities;

import atraxi.core.Player;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.util.Globals;

import java.awt.Point;
import java.math.BigDecimal;

public class Ship extends Entity
{
    public Ship(Globals.Identifiers type, Player owner, Point location)
    {
        super(type, owner, location);
    }

    @Override
    public void doWork(BigDecimal timeAdjustment, boolean paused)
    {
        super.doWork(timeAdjustment, paused);
//        if ( !paused)
//        {
//            synchronized(actionInProgressLock)
//            {
//                if(actionInProgress!=null && actionInProgress.isExecuting())
//                {
//                    boolean finishedRotation, finishedMoving;
//
//                    finishedRotation = updateOrientation(timeAdjustment);
//                    finishedMoving = updatePosition(timeAdjustment);
//
//                    if(finishedRotation && finishedMoving)
//                    {
//                        // TODO: allow drag for target orientation
//                        actionInProgress = null;
//                    }
//                }
//            }
//        }
    }
    
    @Override
    public boolean canAcceptAction(Action action)
    {
        return true;
    }

    @Override
    protected void startActionFromQueue(Action nextActionFromQueue)
    {

    }
}
