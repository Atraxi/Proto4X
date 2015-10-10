package atraxi.ui;

import atraxi.util.ResourceManager;
import atraxi.util.ResourceManager.ImageID;
import atraxi.util.Util;

import java.awt.Graphics2D;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

public class Button implements UIElement
{
    private ImageID imageIDDefault, imageIDHover, imageIDPressed;
    protected int x, y;
    protected Rectangle dim;
    protected ButtonState state = ButtonState.DEFAULT;
    protected Menu parentMenu;
    private CustomCallable<Menu,Void> action;
    private String buttonText;

    public Button(ImageID imageIDDefault, ImageID imageIDHover, ImageID imageIDPressed, int x, int y, String buttonText, CustomCallable<Menu,Void> action)
    {
        this.imageIDDefault = imageIDDefault;
        this.imageIDHover = imageIDHover;
        this.imageIDPressed = imageIDPressed;
        this.action=action;
        this.x=x;
        this.y=y;
        this.buttonText = buttonText;

        int width = ResourceManager.getImage(imageIDDefault).getWidth(null);
        int height = ResourceManager.getImage(imageIDDefault).getHeight(null);
        if(width!=-1 && height!=-1)
        {
            dim=new Rectangle(this.x,this.y,width,height);
        }
        else
        {
            dim=new Rectangle(this.x,this.y,0,0);
        }
    }

    public ImageID getImageID ()
    {
        switch (state)
        {
            case HOVER:
                return imageIDHover;
            case PRESSED:
                return imageIDPressed;
            default:
                return imageIDDefault;
        }
    }

    protected void executeAction()
    {
        action.call(parentMenu);
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
        graphics.drawImage(ResourceManager.getImage(getImageID()), x, y, null);
        Util.drawString(buttonText, dim, graphics);
    }

    protected enum ButtonState
    {
        DEFAULT, HOVER, PRESSED
    }
}