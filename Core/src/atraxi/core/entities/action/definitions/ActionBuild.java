package atraxi.core.entities.action.definitions;

import atraxi.core.Player;
import atraxi.core.entities.Entity;

/**
 * Created by Atraxi on 2/05/2016.
 */
public abstract class ActionBuild extends Action
{
    protected ActionBuild(Entity source, Player player)
    {
        super(source, player);
    }
}
