package atraxi.core.entities.action.definitions;

import atraxi.core.BaseNetworkObject;
import atraxi.core.Player;
import atraxi.core.entities.Entity;
import atraxi.core.util.Globals;
import org.json.JSONObject;

/**
 * Created by Atraxi on 2/05/2016.
 */
public abstract class Action extends BaseNetworkObject
{
    protected Entity source;
    protected Player player;
    public Action nextAction;

    protected Action(Entity source, Player player)
    {
        this.source = source;
        this.player = player;
    }

    public abstract void execute();

    /**
     * Verifies {@link Action#execute()} can run without issue, however without modifying any game state
     * @return true if this action is fully valid for the specific data it contains
     */
    public boolean isValid()
    {
        return source != null &&
               player != null &&
               source.getOwner() == player;
    }

    @Override
    public Action deserialize(JSONObject jsonObject)
    {
//        source = Game.getEntityByID(jsonObject.getLong(Globals.JSON_KEY_Action_Source));
//        player = Game.getPlayerByID(jsonObject.getLong(Globals.JSON_KEY_Action_Player));
        return this;
    }

    @Override
    public JSONObject serializeForPlayer(Player player)
    {
        JSONObject jsonObject = new JSONObject();
        jsonObject.put(Globals.JSON_KEY_Action_Source, source.getID());
//        jsonObject.put(Globals.JSON_KEY_Action_Player, player.getName());
        return jsonObject;
    }
}
