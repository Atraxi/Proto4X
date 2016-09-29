package atraxi.core.entities.action;

import atraxi.core.Player;
import atraxi.core.entities.Entity;
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
        return super.isValid();
    }

    @Override
    public ActionMoveTestImpl deserialize(JSONObject jsonObject)
    {
        super.deserialize(jsonObject);
        return this;
    }

    @Override
    public JSONObject serializeForPlayer(Player player)
    {
        return super.serializeForPlayer(player);
    }
}
