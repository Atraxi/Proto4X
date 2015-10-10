package atraxi.entities;

import java.math.BigDecimal;

import atraxi.game.World;
import atraxi.entities.actionQueue.Action;
import atraxi.entities.actionQueue.Action.ActionType;
import atraxi.game.Player;

public class Structure extends Entity
{
    public Structure(String type, Player owner, int x, int y, World world)
    {
        super(type, x, y, owner, world);
    }
    
    @Override
    public boolean canAcceptAction(ActionType action)
    {
        switch (action)
        {
            case BUILD:
            case SUICIDE:
                return true;
            default:
                return false;
        }
    }

    @Override
    public void doWork(BigDecimal timeDiff, boolean paused)
    {
        super.doWork(timeDiff, paused);
        if(actionInProgress != null && actionInProgress.isExecuting())
        {
            actionInProgress = new Action(actionInProgress.type, new Object[]{((long)actionInProgress.getData()[0]) + timeDiff.movePointRight(timeDiff.scale()+1).longValue()}, true);
            if((long)actionInProgress.getData()[0]>100000000000L)//How many nanoseconds construction should take
            {//TODO: rally point (move command)
                Entity newEntity = new Ship("baseShipClass", owner, x, y, world);
                newEntity.replaceQueue(new Action(Action.ActionType.MOVE, new Object[]{(double)x+300.0, (double)y+100}));
                world.addEntity(newEntity);
                actionInProgress = null;
            }
        }
    }

    @Override
    protected void startActionFromQueue(Action action)
    {
        if(action.type == Action.ActionType.BUILD)
        {
            actionInProgress = new Action(action.type, new Object[]{0L}, true);
        }
    }
}
