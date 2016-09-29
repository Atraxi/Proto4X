package atraxi.core.entities.action;

import atraxi.core.Player;
import atraxi.core.entities.Entity;
import atraxi.core.entities.action.definitions.ActionSetState;
import org.json.JSONObject;

/**
 * Created by Atraxi on 17/05/2016.
 */
public class ActionSetStateTestImpl extends ActionSetState
{
    public ActionSetStateTestImpl(Entity source, Player player)
    {
        super(source, player);
    }

    @Override
    public void execute()
    {

    }

    @Override
    public boolean isValid()
    {
        return false;
    }

    @Override
    public ActionSetStateTestImpl deserialize(JSONObject jsonObject)
    {
        return null;
    }

    @Override
    public JSONObject serializeForPlayer(Player player)
    {
        return null;
    }
}
