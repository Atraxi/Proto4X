package atraxi.core.entities.action.definitions;

import atraxi.core.Player;
import atraxi.core.entities.Entity;

/**
 * Created by Atraxi on 17/05/2016.
 */
public abstract class ActionSetState extends Action
{
    public ActionSetState(Entity source, Player player)
    {
        super(source, player);
    }
}
