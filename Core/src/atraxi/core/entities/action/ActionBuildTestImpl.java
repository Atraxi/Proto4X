package atraxi.core.entities.action;

import atraxi.core.Player;
import atraxi.core.entities.Entity;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.entities.action.definitions.ActionBuild;
import org.json.JSONObject;

/**
 * Created by Atraxi on 17/05/2016.
 */
public class ActionBuildTestImpl extends ActionBuild
{
    protected ActionBuildTestImpl(Entity source, Player player)
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
    public Action deserialize(JSONObject jsonObject)
    {
        return null;
    }

    @Override
    public JSONObject serializeForPlayer(Player player)
    {
        return null;
    }
}
