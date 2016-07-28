package atraxi.core.entities;

import atraxi.core.Player;
import atraxi.core.entities.action.ActionQueue;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.util.Globals;
import atraxi.core.world.World;
import org.json.JSONObject;

import java.awt.Point;
import java.math.BigDecimal;

public abstract class Entity
{
    private Globals.Identifiers type;
    protected Point location;
    private int orientation;
    private Player owner;
    private int moveSpeed;
    protected final ActionQueue actionQueue;
    private int visionRange;
    protected long id;

    public Entity(Globals.Identifiers type, Player owner, Point location, int moveSpeed, int visionRange)
    {
        this.owner = owner;
        this.type = type;
        this.location = location;
        this.moveSpeed = moveSpeed;
        this.actionQueue = new ActionQueue();
        this.id = Globals.getNewID();
    }

    public abstract boolean canAcceptAction(Action action);
    protected abstract void startActionFromQueue(Action action);

    /**
     * Calculate how long it will take to reach a given location
     * @param destination The location to path towards
     * @return The distance to the target location, or -1 if the target cannot be reached
     */
    public double turnCountToReachLocation(Point destination)
    {//TODO: extend this to also try to pathfind through teleporters/ftl

        int distance = World.distanceBetween(this.location, destination);
        if(distance <= Globals.MAX_MOVEMENT_DISTANCE)
        {
            return distance/moveSpeed;
        }
        return -1;
    }

    public void doWork(BigDecimal timeAdjustment, boolean paused)
    {
        if(!actionQueue.isEmpty())
        {
            Action action = actionQueue.pullAction();
            startActionFromQueue(action);
        }
    }
    
    @Override
    public String toString()
    {
        return "[" + type + " Pos:" + location.x + "," + location.y + " Orientation:" + orientation + "]";
    }

    public void queueAction(Action action)
    {
        actionQueue.queueAction(action);
    }

    public void replaceQueue(Action action)
    {
        actionQueue.replaceQueue(action);
    }

    public Globals.Identifiers getType()
    {
        return type;
    }

    public Point getLocation()
    {
        return new Point(location);
    }

    public double getOrientationInRadians()
    {
        return orientation * Math.toRadians(60);
    }

    public Player getOwner()
    {
        return owner;
    }

    void setOwner(Player owner)
    {
        this.owner = owner;
    }

    public int getVisionRange()
    {
        return visionRange;
    }

    /**
     * Serialize this entity to be sent to the specified player. For cheat prevention this should only include information that the player is currently supposed to be able to view.
     *
     * It is safe to assume this entity is already visible by the given player by some means, therefore visibility checks are not required.
     * @param player
     * @return
     */
    public abstract JSONObject serializeForPlayer(Player player);

    public abstract Entity deserialize(JSONObject entityJSON);
}
