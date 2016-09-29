package atraxi.core.entities;

import atraxi.core.Player;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.util.Globals;
import org.json.JSONObject;

import java.awt.Point;
import java.math.BigDecimal;

public class Structure extends Entity
{
    public Structure(Globals.Identifiers type, Player owner, Point location, int moveSpeed, int visionRange)
    {
        super(type, owner, location, moveSpeed, visionRange);
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
    public JSONObject serializeForPlayer(Player player)
    {
        JSONObject jsonObject = super.serializeForPlayer(player);
        //TODO: add additional fields
        //.append(KEY,VALUE);
        return jsonObject;
    }

    @Override
    public Structure deserialize(JSONObject entityJSON)
    {
        super.deserialize(entityJSON);
        return this;
    }

    @Override
    protected void startActionFromQueue(Action action)
    {

    }
}
