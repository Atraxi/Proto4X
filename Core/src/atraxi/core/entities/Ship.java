package atraxi.core.entities;

import atraxi.core.Player;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.util.Globals;
import org.json.JSONObject;

import java.awt.Point;
import java.math.BigDecimal;

public class Ship extends Entity
{
    public Ship(Globals.Identifiers type, Player owner, Point location, int moveSpeed, int viewRange)
    {
        super(type, owner, location, moveSpeed, viewRange);
    }
    public Ship(){super();}

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
    public JSONObject serializeForPlayer(Player player)
    {
        JSONObject jsonObject = super.serializeForPlayer(player);
        //TODO: add additional fields
        //.append(KEY,VALUE);
        return jsonObject;
    }

    @Override
    public Ship deserialize(JSONObject entityJSON)
    {
        super.deserialize(entityJSON);
        return this;
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
