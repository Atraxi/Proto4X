package atraxi.entities;

import java.awt.Graphics2D;
import java.awt.Rectangle;
import java.awt.geom.AffineTransform;
import java.math.BigDecimal;

import atraxi.util.ResourceManager.ImageID;
import atraxi.game.world.World;
import atraxi.entities.actionQueue.Action;
import atraxi.entities.actionQueue.Action.ActionType;
import atraxi.entities.actionQueue.ActionQueue;
import atraxi.game.Player;

public abstract class Entity
{
    private ImageID imageID;
    private String type;
    protected World world;
    protected double x, y, velocity, orientation;
    //TODO: combine these into a Path2D instance
    private int boundsXOffset, boundsXUpper, boundsYOffset, boundsYUpper;
    protected ActionQueue actionQueue = new ActionQueue();
    protected Action actionInProgress = null;
    protected Player owner;
    //TODO: Temporary solution for locking on a potentially null object, as of writing there is no reason not to use 'this' although that could change later
    protected final Object actionInProgressLock = new Object();
    
    public Entity(String type, double x, double y, int boundsXOffset, int boundsYOffset, int boundsXUpper, int boundsYUpper, Player owner, World world)
    {
        this.owner = owner;
        this.type = type;
        this.world = world;
        imageID = ImageID.valueOf(this.type);
        this.x=x;
        this.y=y;
        velocity = 0;
        this.boundsXOffset = boundsXOffset;
        this.boundsYOffset = boundsYOffset;
        orientation = 0;
        if(boundsXUpper==0 && boundsYUpper==0)
        {
            this.boundsXUpper = imageID.getImage().getWidth(null);
            this.boundsYUpper = imageID.getImage().getHeight(null);
        }
        else
        {
            this.boundsXUpper = boundsXUpper;
            this.boundsYUpper = boundsYUpper;
        }
    }
    
    public Entity(String type, double x, double y, Player owner, World world)
    {
        this(type, x, y, 0, 0, 0, 0, owner, world);
    }
    
    public abstract boolean canAcceptAction(ActionType action);
    protected abstract void startActionFromQueue(Action action);
    
    
    public void doWork(BigDecimal timeAdjustment, boolean paused)
    {
        if(actionInProgress == null && !actionQueue.isEmpty())
        {
            Action action = actionQueue.popAction();
            startActionFromQueue(action);
        }
    }
    
    @Override
    public String toString()
    {
        return "["+type + " Pos:"+x+","+y+" Orientation:"+orientation+"]";
    }
    
    public boolean boundsTest(Rectangle selectionArea)
    {
        return getBounds().intersects(selectionArea);
    }
    
    public AffineTransform getTransform()
    {
        AffineTransform at = AffineTransform.getTranslateInstance(x+imageID.getImage().getWidth(null)/2, y+
                imageID.getImage().getHeight(null)/2);
        //at.rotate(orientation);
        //hopefully bypass the snap to 90degrees 'feature' (not always desired, but impossible to disable), although I'm not sure it will make a noticeable difference
        at.concatenate(new AffineTransform(Math.cos(orientation), Math.sin(orientation), -Math.sin(orientation), Math.cos(orientation), 0, 0));
        at.translate(-imageID.getImage().getWidth(null)/2, -imageID.getImage().getHeight(null)/2);
        return at;
    }

    public double getX()
    {
        return x;
    }

    public double getY()
    {
        return y;
    }

    public ImageID getImageID ()
    {
        return imageID;
    }

    private Rectangle getBounds()
    {
        return new Rectangle((int)(x+boundsXOffset), (int)(y+boundsYOffset), boundsXUpper, boundsYUpper);
    }

    public void queueAction(Action action)
    {
        actionQueue.queueAction(action);
    }

    public void replaceQueue(Action action)
    {
        actionQueue.replaceQueue(action);
        synchronized(actionInProgressLock)
        {
            actionInProgress = null;
        }
    }

    public void paint(Graphics2D g2d)
    {
        g2d.drawImage(imageID.getImage(), getTransform(), null);
    }
}
