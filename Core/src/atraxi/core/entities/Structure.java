package atraxi.core.entities;

import atraxi.core.Player;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.util.Globals;

import java.awt.Point;
import java.math.BigDecimal;

public class Structure extends Entity
{
    public Structure(Globals.Identifiers type, Player owner, Point location)
    {
        super(type, owner, location);
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
