package entities;

import java.math.BigDecimal;

import atraxi.game.World;
import entities.actionQueue.Action;
import entities.actionQueue.Action.ActionType;
import atraxi.game.Player;

public class Structure extends Entity
{
    
    public Structure(String type, Player owner, int x, int y)
    {
        super(type, x, y, owner);
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
            System.out.println("Build progress:"+actionInProgress.getData()[0]);
            if((long)actionInProgress.getData()[0]>100000000000L)//How many nanoseconds construction should take
            {//TODO: rally point (move command)
                Entity newEntity = new Ship("baseShipClass", owner, x, y);
                newEntity.replaceQueue(new Action(Action.ActionType.MOVE, new Object[]{(double)x+300.0, (double)y+100}));
                World.addEntity(newEntity);
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
