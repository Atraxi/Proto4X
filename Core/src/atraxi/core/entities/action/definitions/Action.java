package atraxi.core.entities.action.definitions;

import atraxi.core.Player;
import atraxi.core.entities.Entity;
import org.json.JSONObject;

/**
 * Created by Atraxi on 2/05/2016.
 */
public abstract class Action
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
    public  boolean isValid()
    {
        return source != null &&
               player != null &&
               source.getOwner() == player;
    }

    public abstract Action fromJSON(JSONObject jsonObject);

    public abstract JSONObject toJSON();
}
