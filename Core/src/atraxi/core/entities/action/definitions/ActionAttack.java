package atraxi.core.entities.action.definitions;

import atraxi.core.Player;
import atraxi.core.entities.Entity;

/**
 * Created by Atraxi on 17/05/2016.
 */
public abstract class ActionAttack extends Action
{
    protected Entity target;

    public ActionAttack(Entity source, Player player, Entity target)
    {
        super(source, player);
        this.target = target;
    }
}
