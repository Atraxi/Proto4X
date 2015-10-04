package atraxi.game.UI;

import java.awt.Graphics2D;
import java.awt.Image;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;
import java.awt.image.ImageObserver;

public class Button implements UIElement, ImageObserver
{
    private Image image, imageHover, imagePressed;
    protected int x, y;
    protected Rectangle dim;
    protected ButtonState state = ButtonState.DEFAULT;
    protected Menu parentMenu;
    private CustomCallable<Menu,Void> action;

    public Button(Image image, Image imageHover, Image imagePressed, int x, int y, CustomCallable<Menu,Void> action)
    {
        this.image = image;
        this.imageHover = imageHover;
        this.imagePressed = imagePressed;
        this.action=action;
        this.x=x;
        this.y=y;

        int width = image.getWidth(this);
        int height = image.getHeight(this);
        if(width!=-1 && height!=-1)
        {
            dim=new Rectangle(this.x,this.y,width,height);
        }
        else
        {
            dim=new Rectangle(this.x,this.y,0,0);
        }
    }

    public Button (Image image, int x, int y,CustomCallable<Menu,Void> action)
    {
        this(image,image,image,x,y,action);
    }

    public Image getImage ()
    {
        switch (state)
        {
            case HOVER:
                return imageHover;
            case PRESSED:
                return imagePressed;
            default:
                return image;
        }
    }

    protected void executeAction()
    {
        action.call(parentMenu);
    }

    @Override
    public boolean imageUpdate (Image img, int infoFlags, int x, int y, int width, int height)
    {
        if(((ImageObserver.WIDTH | ImageObserver.HEIGHT) & infoFlags)  == (ImageObserver.WIDTH | ImageObserver.HEIGHT))
        {
            dim = new Rectangle(this.x,this.y,width,height);
            return true;
        }
        else
        {
            return false;
        }
    }

    @Override
    public UIElement mousePressed (MouseEvent paramMouseEvent)
    {
        if(dim.contains(paramMouseEvent.getPoint()))
        {
            state = ButtonState.PRESSED;
            return this;
        }
        return null;
    }

    @Override
    public UIElement mouseReleased (MouseEvent paramMouseEvent)
    {
        if(state == ButtonState.PRESSED)
        {
            if(dim.contains(paramMouseEvent.getPoint()))
            {
                executeAction();
                state = ButtonState.HOVER;
                return this;
            }
            else
            {
                state = ButtonState.DEFAULT;
            }
        }
        return  null;
    }

    @Override
    public UIElement mouseEntered (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseExited (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseDragged (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseMoved (MouseEvent paramMouseEvent)
    {
        if(dim.contains(paramMouseEvent.getPoint()) && state == ButtonState.DEFAULT)
        {
            state = ButtonState.HOVER;
            return this;
        }
        else if(!dim.contains(paramMouseEvent.getPoint()) && state == ButtonState.HOVER)
        {
            state = ButtonState.DEFAULT;
        }
        return null;
    }

    @Override
    public UIElement mouseWheelMoved (MouseWheelEvent paramMouseWheelEvent)
    {
        return null;
    }

    @Override
    public void paint (Graphics2D graphics)
    {
        graphics.drawImage(getImage(), x, y, null);
    }

    protected enum ButtonState
    {
        DEFAULT, HOVER, PRESSED
    }
}