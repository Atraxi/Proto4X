package atraxi.core.entities.action;

import atraxi.core.entities.Entity;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.entities.action.definitions.ActionSetState;
import org.json.JSONObject;

/**
 * Created by Atraxi on 17/05/2016.
 */
public class ActionSetStateTestImpl extends ActionSetState
{
    public ActionSetStateTestImpl(Entity source)
    {
        super(source);
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
