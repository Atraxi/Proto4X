package atraxi.core.entities;

import atraxi.core.Player;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.world.GridTile;

import java.math.BigDecimal;

public class Structure extends Entity
{
    public Structure(String type, Player owner, int x, int y, GridTile worldTile)
    {
        super(type, owner, worldTile);
    }
    
    @Override
    public boolean canAcceptAction(Action action)
    {
//        switch (action)
//        {
//            case BUILD:
//            case SUICIDE:
//                return true;
//            default:
//                return false;
//        }
        return true;
    }

    @Override
    public void doWork(BigDecimal timeDiff, boolean paused)
    {
        super.doWork(timeDiff, paused);
//        if(actionInProgress != null && actionInProgress.isExecuting())
//        {
//            actionInProgress = new Action(actionInProgress.type, new Object[]{((long)actionInProgress.getData()[0]) + timeDiff.movePointRight(timeDiff.scale()+1).longValue()}, true);
//            if((long)actionInProgress.getData()[0]>100000000000L)//How many nanoseconds construction should take
//            {//TODO: rally point (move command)
//                Entity newEntity = new Ship("entityShipDefault", owner, x, y, worldTile);
//                newEntity.replaceQueue(new Action(Action.ActionType.MOVE, new Object[]{x + 300.0, y + 100}));
//                worldTile.addEntity(newEntity);
//                actionInProgress = null;
//            }
//        }
    }

    @Override
    protected void startActionFromQueue(Action action)
    {
//        if(action.type == Action.ActionType.BUILD)
//        {
//            actionInProgress = new Action(action.type, new Object[]{0L}, true);
//        }
    }
}
