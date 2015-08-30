package atraxi.game.UI;

import java.awt.*;
import java.awt.event.MouseEvent;
import java.awt.image.ImageObserver;
import java.util.concurrent.Callable;

public class Button implements UIElement, ImageObserver
{
    private Image image, imageHover, imagePressed;
    private int x, y;
    protected Rectangle dim;
    public ButtonState state = ButtonState.DEFAULT;
    private Callable action;

    public Button(Image image, Image imageHover, Image imagePressed,int x,int y, Callable action)
    {
        this.image = image;
        this.imageHover = imageHover;
        this.imagePressed = imagePressed;
        this.x=x;
        this.y=y;
        this.action=action;
        int width = image.getWidth(this);
        int height = image.getHeight(this);
        if(width!=-1 && height!=-1)
        {
            dim=new Rectangle(x,y,width,height);
        }
        else
        {
            dim=new Rectangle(x,y,0,0);
        }
    }

    public Button (Image image, int x, int y,Callable action)
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

    public int getX ()
    {
        return x;
    }

    public int getY ()
    {
        return y;
    }

    @Override
    public boolean mousePressed (MouseEvent paramMouseEvent)
    {
        if(dim.contains(paramMouseEvent.getLocationOnScreen()))
        {
            state = Button.ButtonState.PRESSED;
            return true;
        }
        else
        {
            return false;
        }
    }

    @Override
    public boolean mouseReleased (MouseEvent paramMouseEvent)
    {
        if(dim.contains(paramMouseEvent.getLocationOnScreen()) && state==ButtonState.PRESSED)
        {
            try
            {
                action.call();
            }
            catch(Exception e)
            {
                //TODO: This probably needs to be handled with a popup
                e.printStackTrace();
            }
            state = ButtonState.HOVER;
            return true;
        }
        else
        {
            return false;
        }
    }

    @Override
    public boolean mouseEntered (MouseEvent paramMouseEvent)
    {

        return false;
    }

    @Override
    public boolean mouseExited (MouseEvent paramMouseEvent)
    {
        return false;
    }

    @Override
    public boolean mouseDragged (MouseEvent paramMouseEvent)
    {
        return false;
    }

    @Override
    public boolean mouseMoved (MouseEvent paramMouseEvent)
    {
        if(dim.contains(paramMouseEvent.getPoint()))
        {
            state=ButtonState.HOVER;
            return true;
        }
        else
        {
            return false;
        }
    }

    @Override
    public boolean imageUpdate (Image img, int infoflags, int x, int y, int width, int height)
    {
        return false;
    }

    private enum ButtonState
    {
        DEFAULT, HOVER, PRESSED
    }
}