package atraxi.core.entities.action.definitions;

import atraxi.core.entities.Entity;

/**
 * Created by Atraxi on 17/05/2016.
 */
public abstract class ActionAttack extends Action
{
    protected Entity target;

    public ActionAttack(Entity source, Entity target)
    {
        super(source);
        this.target = target;
    }
}
