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
        JSONObject jsonObject = new JSONObject();
        jsonObject.put(Globals.JSON_KEY_Entity_Player, player.getName())
                  .put(Globals.JSON_KEY_Entity_PositionX, getLocation().x)
                  .put(Globals.JSON_KEY_Entity_PositionY, getLocation().y);
        return jsonObject;
    }

    @Override
    public Entity deserialize(JSONObject entityJSON)
    {
        //TODO: implement deserialization
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
