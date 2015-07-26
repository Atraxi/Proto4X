package entities;

import java.awt.Image;
import java.awt.Rectangle;
import java.awt.geom.AffineTransform;

import javax.swing.ImageIcon;

public abstract class Entity
{
    private Image image;
    private String type;
    protected double x, y, velocity, orientation;
    private int boundsXOffset, boundsXUpper, boundsYOffset, boundsYUpper;
    
    public Entity(String type, double x, double y, int boundsXOffset, int boundsYOffset, int boundsXUpper, int boundsYUpper)
    {
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
    
    public Entity(String type, double x, double y)
    {
        this(type, x, y, 0, 0, 0, 0);
    }
    
    public void rightClickCommand(double d, double e){}
    
    public void doWork(long timeDiff, boolean paused){}
    
    public Image getImage()
    {
        return image;
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
    
    private Rectangle getBounds()
    {
        return new Rectangle((int)(x+boundsXOffset), (int)(y+boundsYOffset), boundsXUpper, boundsYUpper);
    }
    
    public AffineTransform getTransform()
    {
        AffineTransform at = AffineTransform.getTranslateInstance(x+image.getWidth(null)/2, y+image.getHeight(null)/2);
        //at.rotate(orientation);
        //hopefully bypass the snap to 90degrees 'feature', although I'm not sure it's actually needed
        at.concatenate(new AffineTransform(Math.cos(orientation), Math.sin(orientation), -Math.sin(orientation), Math.cos(orientation), 0, 0));
        at.translate(-image.getWidth(null)/2, -image.getHeight(null)/2);
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
}
