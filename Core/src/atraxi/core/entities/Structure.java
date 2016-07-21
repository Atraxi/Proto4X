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
        JSONObject jsonObject = new JSONObject();
        jsonObject.put(Globals.JSON_KEY_Entity_Player, player.getName())
                  .put(Globals.JSON_KEY_Entity_PositionX, getLocation().x)
                  .put(Globals.JSON_KEY_Entity_PositionY, getLocation().y);
        return jsonObject;
    }

    @Override
    protected void startActionFromQueue(Action action)
    {

    }
}
