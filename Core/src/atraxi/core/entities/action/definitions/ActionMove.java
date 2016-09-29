package atraxi.core.entities.action.definitions;

import atraxi.core.Player;
import atraxi.core.entities.Entity;
import atraxi.core.util.Globals;
import org.json.JSONObject;

import java.awt.Point;

/**
 * Created by Atraxi on 2/05/2016.
 */
public abstract class ActionMove extends Action
{
    protected Point target;
    public ActionMove(Entity source, Player player, Point target)
    {
        super(source, player);
        this.target = target;
    }

    @Override
    public boolean isValid()
    {
        return super.isValid() && source.turnCountToReachLocation(target) >= 0;
    }

    @Override
    public JSONObject serializeForPlayer(Player player)
    {
        JSONObject jsonObject = super.serializeForPlayer(player);
        jsonObject.put(Globals.JSON_KEY_ActionMove_TargetX, target.x);
        jsonObject.put(Globals.JSON_KEY_ActionMove_TargetY, target.y);
        return jsonObject;
    }

    @Override
    public ActionMove deserialize(JSONObject jsonObject)
    {
        target = new Point(jsonObject.getInt(Globals.JSON_KEY_ActionMove_TargetX),
                           jsonObject.getInt(Globals.JSON_KEY_ActionMove_TargetY));
        return this;
    }
}
