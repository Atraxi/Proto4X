package atraxi.core.entities.action;

import atraxi.core.Player;
import atraxi.core.entities.Entity;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.entities.action.definitions.ActionMove;
import org.json.JSONObject;

import java.awt.Point;

/**
 * Created by Atraxi on 17/05/2016.
 */
public class ActionMoveTestImpl extends ActionMove
{
    public ActionMoveTestImpl(Entity source, Player player, Point target)
    {
        super(source, player, target);
    }

    @Override
    public void execute()
    {

    }

    @Override
    public boolean isValid()
    {
        return super.isValid() && source.turnCountToReachLocation(target) >= 0;
    }

    @Override
    public Action fromJSON(JSONObject jsonObject)
    {
        return null;
    }

    @Override
    public JSONObject toJSON()
    {
        return null;
    }
}
