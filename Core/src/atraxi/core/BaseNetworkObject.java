package atraxi.core;

import atraxi.core.util.Globals;
import org.json.JSONObject;

/**
 * Created by Atraxi on 29/09/2016.
 */
public abstract class BaseNetworkObject
{
    private long id;

    public BaseNetworkObject(long id)
    {
        this.id = id;
    }

    protected BaseNetworkObject()
    {}

    public long getID()
    {
        return id;
    }

    public JSONObject serializeForPlayer(Player player)
    {
        JSONObject jsonObject = new JSONObject();
        jsonObject.put(Globals.JSON_KEY_BaseNetworkObject_ID, id);
        return jsonObject;
    }

    public BaseNetworkObject deserialize(JSONObject jsonObject)
    {
        id = jsonObject.getLong(Globals.JSON_KEY_BaseNetworkObject_ID);
        return this;
    }
}
