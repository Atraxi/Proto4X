package entities;

import java.awt.Graphics2D;
import java.awt.Image;
import java.awt.Rectangle;
import java.awt.geom.AffineTransform;
import java.math.BigDecimal;

import javax.swing.ImageIcon;

import atraxi.game.UI.UserInterfaceHandler;
import entities.actionQueue.Action;
import entities.actionQueue.Action.ActionType;
import entities.actionQueue.ActionQueue;
import atraxi.game.Player;

public abstract class Entity
{
    protected Image image;
    private String type;
    protected double x, y, velocity, orientation;
    private int boundsXOffset, boundsXUpper, boundsYOffset, boundsYUpper;
    protected ActionQueue actionQueue = new ActionQueue();
    protected Action actionInProgress = null;
    protected Player owner;
    //TODO: Temporary solution for locking on a potentially null object
    protected final Object actionInProgressLock = new Object();
    
    public Entity(String type, double x, double y, int boundsXOffset, int boundsYOffset, int boundsXUpper, int boundsYUpper, Player owner)
    {
        this.owner = owner;
        this.type = type;
        image = new ImageIcon("resources/"+this.type+".png").getImage();
        this.x=x;
        this.y=y;
        velocity = 0;
        this.boundsXOffset = boundsXOffset;
        this.boundsYOffset = boundsYOffset;
        orientation = 0;
        if(boundsXUpper==0 && boundsYUpper==0)
        {
            this.boundsXUpper = image.getWidth(null);
            this.boundsYUpper = image.getHeight(null);
        }
        else
        {
            this.boundsXUpper = boundsXUpper;
            this.boundsYUpper = boundsYUpper;
        }
    }
    
    public Entity(String type, double x, double y, Player owner)
    {
        this(type, x, y, 0, 0, 0, 0, owner);
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
        AffineTransform at = AffineTransform.getTranslateInstance(x+image.getWidth(null)/2, y+image.getHeight(null)/2);
        //at.rotate(orientation);
        //hopefully bypass the snap to 90degrees 'feature' (not always desired, but impossible to disable), although I'm not sure it will make a noticeable difference
        at.concatenate(new AffineTransform(Math.cos(orientation), Math.sin(orientation), -Math.sin(orientation), Math.cos(orientation), 0, 0));
        at.translate((image.getWidth(null)/2)+ UserInterfaceHandler.getScreenLocationX(), (-image.getHeight(null)/2)+UserInterfaceHandler.getScreenLocationY());
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

    public void paint(Graphics2D graphics)
    {
        graphics.drawImage(image, getTransform(), null);
    }
}
