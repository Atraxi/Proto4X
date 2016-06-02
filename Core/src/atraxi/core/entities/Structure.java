package atraxi.core.entities;

import atraxi.core.Player;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.util.Globals;
import atraxi.core.world.GridTile;

import java.math.BigDecimal;

public class Structure extends Entity
{
    public Structure(Globals.Identifiers type, Player owner, GridTile worldTile)
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
    }

    @Override
    protected void startActionFromQueue(Action action)
    {

    }
}
