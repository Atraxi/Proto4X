package atraxi.entities;

import java.math.BigDecimal;

import atraxi.util.Logger;
import atraxi.util.Logger.LogLevel;
import atraxi.util.ResourceManager;
import atraxi.game.World;
import atraxi.entities.actionQueue.Action;
import atraxi.entities.actionQueue.Action.ActionType;
import atraxi.game.Player;

public class Ship extends Entity
{
    private static final double MAXROTATIONSPEED = 0.05;
    private static final double MAXTRAVELSPEED   = 2;
    
    public Ship(String type, Player owner, double x, double y, World world)
    {
        super(type, x, y, owner, world);
    }
    /**
     * Rotate to the target orientation stored in actionInProgress' data field,
     *  where data[2]==orientationTarget;
     * @param timeAdjustment The fraction by which all motion should be adjusted to account for the current frame rate
     * @return true if there is no further rotation needed for this action
     */
    private boolean updateOrientation(BigDecimal timeAdjustment)
    {
        double orientationTarget = (double) actionInProgress.getData()[2];
        
        // the amount we need to rotate
        double orientationDelta = orientationTarget - orientation;
        //separate the direction to reduce the number of branches required
        double orientationDirection = Math.signum(orientationDelta);
        orientationDelta = Math.abs(orientationDelta);
        // keep within the range -Pi < x < Pi
        if (orientationDelta > Math.PI)
        {
            //Subtract 1 full rotation, essentially inverting the direction
            orientationDelta -= 2 * Math.PI;
            //It's theoretically possible for orientationDelta to be greater than 2*PI, in which case the direction won't necessarily flip. Probably redundant in this context though
            orientationDirection *= Math.signum(orientationDelta);
            orientationDelta = Math.abs(orientationDelta);
        }
        if (orientationDelta > 0.001)
        {
            orientation += timeAdjustment.multiply(BigDecimal.valueOf(MAXROTATIONSPEED))
                                            .min(BigDecimal.valueOf(orientationDelta))
                                            .multiply(BigDecimal.valueOf(orientationDirection)).doubleValue();
            return false;
        }
        else
        {
            return true;
        }
    }
    
    /**
     * Move to the target location stored in actionInProgress' data field,
     *  where data[0]==targetX; data[1]==targetY;
     * @param timeAdjustment The fraction by which all motion should be adjusted to account for the current frame rate
     * @return true if there is no further movement needed for this action
     */
    private boolean updatePosition(BigDecimal timeAdjustment)
    {
        double targetX = (double) actionInProgress.getData()[0];
        double targetY = (double) actionInProgress.getData()[1];
        
        // if we are not where we were told to be
        if ((x != targetX) || (y != targetY))
        {
            // TODO: acceleration and deceleration. ideally rewrite motion to use a momentum vector (that can be externally modified by things like explosions)
            double xDiff = targetX - x;
            double yDiff = targetY - y;
            double pythagorasPart = Math.sqrt((xDiff * xDiff) + (yDiff * yDiff));
            if (pythagorasPart < MAXTRAVELSPEED)
            {
                x = targetX;
                //x+=xDiff;
                y = targetY;
                //y+=yDiff;
            }
            else
            {
                x += timeAdjustment.multiply(BigDecimal.valueOf((MAXTRAVELSPEED/pythagorasPart) * xDiff)).doubleValue();
                y += timeAdjustment.multiply(BigDecimal.valueOf((MAXTRAVELSPEED/pythagorasPart) * yDiff)).doubleValue();
            }
            return false;
        }
        else
        {
            return true;
        }
    }
    
    @Override
    public void doWork(BigDecimal timeAdjustment, boolean paused)
    {
        super.doWork(timeAdjustment, paused);
        if ( !paused)
        {
            synchronized(actionInProgressLock)
            {
                if(actionInProgress!=null && actionInProgress.isExecuting())
                {
                    boolean finishedRotation, finishedMoving;

                    finishedRotation = updateOrientation(timeAdjustment);
                    finishedMoving = updatePosition(timeAdjustment);

                    if(finishedRotation && finishedMoving)
                    {
                        // TODO: allow drag for target orientation
                        actionInProgress = null;
                    }
                }
            }
        }
    }
    
    @Override
    public boolean canAcceptAction(ActionType action)
    {
        switch (action)
        {
            case MOVE:
            case PATROL:
            case GUARD:
            case SUICIDE:
                return true;
            default:
                return false;
        }
    }

    @Override
    protected void startActionFromQueue(Action nextActionFromQueue)
    {
        if(nextActionFromQueue.type==Action.ActionType.MOVE)
        {
            double x = (double) nextActionFromQueue.getData()[0];
            double y = (double) nextActionFromQueue.getData()[1];
            //offset the target location to be relative to the image center, instead of the corner
            x -= getImageID().getImage().getWidth(null) / 2;
            y -= getImageID().getImage().getHeight(null) / 2;
            
            double targetX = x;
            double targetY = y;
            //get the target position relative to the current location of the ship, for calculating the direction
            x -= this.x;
            y -= this.y;
            double orientationTarget = Math.atan2(y, x);
            Logger.log(LogLevel.debug, new String[] {"Right Click Command -> Ship:" + this, "\tx:" + x
                    + " y:" + y + " orientationTarget:" + orientationTarget
                    + " orientationDelta:" + (orientationTarget - orientation)});
            synchronized(actionInProgressLock)
            {
                actionInProgress = new Action(nextActionFromQueue.type, new Object[]{targetX, targetY, orientationTarget}, true);
            }
        }
    }
}
