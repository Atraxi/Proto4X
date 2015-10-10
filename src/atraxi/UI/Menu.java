package atraxi.UI;

import atraxi.game.ResourceManager;
import atraxi.game.ResourceManager.ImageID;

import java.awt.Graphics2D;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

public class Menu implements UIElement, UIStackNode
{
    //TODO: implement image scaling, try "image.getScaledInstance(width,height,algorithm(enum));"
    private Button[] buttons;
    private int x, y;
    private ImageID backgroundID;
    private Rectangle dim;
    private UIStackNode nextNode = null, previousNode = null;
    private Point movePoint = null;

    public Menu(ImageID imageID, int x, int y, Button[] buttons)
    {
        this.backgroundID=imageID;
        this.buttons = buttons;


        int width = ResourceManager.getImage(backgroundID).getWidth(null);
        int height = ResourceManager.getImage(backgroundID).getHeight(null);
        if(width!=-1 && height!=-1)
        {
            this.x=x-(width/2);
            this.y=y-(height/2);
            dim=new Rectangle(this.x,this.y,width,height);

            for(Button button : buttons)
            {
                button.x+=this.x;
                button.y+=this.y;
                button.dim.setLocation(button.x,button.y);
                button.parentMenu = this;
            }
        }
        else
        {
            this.x=x;
            this.y=y;
            dim=new Rectangle(this.x,this.y,0,0);

            for(Button button : buttons)
            {
                button.x+=this.x;
                button.y+=this.y;
                button.dim.setLocation(button.x,button.y);
            }
        }
    }

    public void paint (Graphics2D g2d)
    {
        g2d.drawImage(ResourceManager.getImage(backgroundID), x, y, null);
        for(Button button : buttons)
        {
            button.paint(g2d);
        }
    }

    @Override
    public UIElement mouseEntered (MouseEvent paramMouseEvent)
    {
        for(Button button : buttons)
        {
            UIElement resultElement = button.mouseEntered(paramMouseEvent);
            if(resultElement!=null)
            {
                return resultElement;
            }
        }
        return null;
    }

    @Override
    public UIElement mouseExited (MouseEvent paramMouseEvent)
    {
        for(Button button : buttons)
        {
            UIElement resultElement = button.mouseExited(paramMouseEvent);
            if(resultElement!=null)
            {
                return resultElement;
            }
        }
        return null;
    }

    @Override
    public UIElement mousePressed (MouseEvent paramMouseEvent)
    {
        for(Button button : buttons)
        {
            UIElement resultElement = button.mousePressed(paramMouseEvent);
            if(resultElement!=null)
            {
                return resultElement;
            }
        }
        if(dim.contains(paramMouseEvent.getPoint()))
        {
            if(paramMouseEvent.getButton() == MouseEvent.BUTTON1)
            {
                movePoint = paramMouseEvent.getPoint();
            }
            return this;
        }
        return null;
    }

    @Override
    public UIElement mouseReleased (MouseEvent paramMouseEvent)
    {
        if(movePoint != null)
        {
            this.x-=(movePoint.getX()-paramMouseEvent.getX());
            this.y-=(movePoint.getY()-paramMouseEvent.getY());
            dim.setLocation(this.x,this.y);

            for(Button button : buttons)
            {
                button.x-=(movePoint.getX()-paramMouseEvent.getX());
                button.y-=(movePoint.getY()-paramMouseEvent.getY());
                button.dim.setLocation(button.x,button.y);
            }
            movePoint=null;
            return this;
        }
        for(Button button : buttons)
        {
            UIElement resultElement = button.mouseReleased(paramMouseEvent);
            if(resultElement!=null)
            {
                return resultElement;
            }
        }
        if(dim.contains(paramMouseEvent.getPoint()))
        {
            return this;
        }
        return null;
    }

    @Override
    public UIElement mouseDragged (MouseEvent paramMouseEvent)
    {
        if(movePoint != null)
        {
            this.x-=(movePoint.getX()-paramMouseEvent.getX());
            this.y-=(movePoint.getY()-paramMouseEvent.getY());
            dim.setLocation(this.x,this.y);

            for(Button button : buttons)
            {
                button.x-=(movePoint.getX()-paramMouseEvent.getX());
                button.y-=(movePoint.getY()-paramMouseEvent.getY());
                button.dim.setLocation(button.x,button.y);
            }
            movePoint = paramMouseEvent.getPoint();
            return this;
        }
        for(Button button : buttons)
        {
            UIElement resultElement = button.mouseDragged(paramMouseEvent);
            if(resultElement!=null)
            {
                return resultElement;
            }
        }
        return null;
    }

    @Override
    public UIElement mouseMoved (MouseEvent paramMouseEvent)
    {
        for(Button button : buttons)
        {
            UIElement resultElement = button.mouseMoved(paramMouseEvent);
            if(resultElement!=null)
            {
                return resultElement;
            }
        }
        if(dim.contains(paramMouseEvent.getPoint()))
        {
            return this;
        }
        return null;
    }

    @Override
    public UIElement mouseWheelMoved (MouseWheelEvent paramMouseWheelEvent)
    {
        for(Button button : buttons)
        {
            UIElement resultElement = button.mouseWheelMoved(paramMouseWheelEvent);
            if(resultElement!=null)
            {
                return resultElement;
            }
        }
        if(dim.contains(paramMouseWheelEvent.getPoint()))
        {
            return this;
        }
        return null;
    }

    @Override
    public UIStackNode getNextNode ()
    {
        return nextNode;
    }

    @Override
    public UIStackNode getPreviousNode ()
    {
        return previousNode;
    }

    @Override
    public void setNextNode (UIStackNode element)
    {
        nextNode = element;
    }

    @Override
    public void setPreviousNode (UIStackNode element)
    {
        previousNode = element;
    }
}
