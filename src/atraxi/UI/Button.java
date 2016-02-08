package atraxi.ui;

import atraxi.util.ResourceManager.ImageID;
import atraxi.util.CheckedRender;

import java.awt.Graphics2D;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

public class Button implements UIElement
{
    private ImageID imageIDDefault, imageIDHover, imageIDPressed;
    //protected int x, y;
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
        this.buttonText = buttonText;

        int width = imageIDDefault.getImage().getWidth(null);
        int height = imageIDDefault.getImage().getHeight(null);

        dim=new Rectangle(x,y,width,height);
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
    public void paint (CheckedRender render)
    {
        render.drawImage(getImageID(), dim.x, dim.y, dim);
        render.drawString(buttonText, dim);
    }

    protected enum ButtonState
    {
        DEFAULT, HOVER, PRESSED
    }
}