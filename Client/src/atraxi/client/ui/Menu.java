package atraxi.client.ui;

import atraxi.client.util.RenderUtil;
import atraxi.client.util.ResourceManager;
import atraxi.core.util.Globals;

import java.awt.Point;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

public class Menu implements UIElement, UIStackNode
{
    //TODO: implement image scaling, try "image.getScaledInstance(width,height,algorithm(enum));"
    private Button[] buttons;
    //private int x, y;
    private Globals.Identifiers backgroundID;
    private Rectangle dim;
    private UIStackNode nextNode = null, previousNode = null;
    private Point movePoint = null;

    public Menu(Globals.Identifiers imageID, int x, int y, Button[] buttons)
    {
        this.backgroundID=imageID;
        this.buttons = buttons;

        int width = ResourceManager.getImage(backgroundID).getWidth(null);
        int height = ResourceManager.getImage(backgroundID).getHeight(null);

        dim = new Rectangle(x-(width/2),y-(height/2),width,height);

        for(Button button : buttons)
        {
            button.dim.setLocation(button.dim.x+dim.x,button.dim.y+dim.y);
            button.parent = this;
        }
    }

    @Override
    public void paint(RenderUtil render)
    {
        render.drawImage(backgroundID, dim.x, dim.y, dim);
        for(Button button : buttons)
        {
            button.paint(render);
        }
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
            dim.x-=(movePoint.getX()-paramMouseEvent.getX());
            dim.y-=(movePoint.getY()-paramMouseEvent.getY());

            for(Button button : buttons)
            {
                button.dim.x-=(movePoint.getX()-paramMouseEvent.getX());
                button.dim.y-=(movePoint.getY()-paramMouseEvent.getY());
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
            dim.x-=(movePoint.getX()-paramMouseEvent.getX());
            dim.y-=(movePoint.getY()-paramMouseEvent.getY());

            for(Button button : buttons)
            {
                button.dim.x-=(movePoint.getX()-paramMouseEvent.getX());
                button.dim.y-=(movePoint.getY()-paramMouseEvent.getY());
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
