package atraxi.client.ui;

import atraxi.client.util.RenderUtil;
import atraxi.client.util.ResourceManager;
import atraxi.core.util.Globals;

import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

public class Button implements UIElement
{
    private Globals.Identifiers imageIDDefault, imageIDHover, imageIDPressed;
    //protected int x, y;
    protected Rectangle dim;
    protected ButtonState state = ButtonState.DEFAULT;
    protected UIStackNode parent;
    private CustomCallable<UIStackNode, Void> action;
    private String buttonText;

    /**
     *
     * @param imageIDDefault
     * @param imageIDHover
     * @param imageIDPressed
     * @param x Position
     * @param y Position
     * @param buttonText
     * @param action Executed when pressed
     */
    public Button(Globals.Identifiers imageIDDefault, Globals.Identifiers imageIDHover, Globals.Identifiers imageIDPressed, int x, int y, String buttonText, CustomCallable<UIStackNode, Void> action)
    {
        this.imageIDDefault = imageIDDefault;
        this.imageIDHover = imageIDHover;
        this.imageIDPressed = imageIDPressed;
        this.action=action;
        this.buttonText = buttonText;

        int width = ResourceManager.getImage(imageIDDefault).getWidth(null);
        int height = ResourceManager.getImage(imageIDDefault).getHeight(null);

        dim=new Rectangle(x,y,width,height);
    }

    public Globals.Identifiers getImageID ()
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
        action.call(parent);
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
    public void paint(RenderUtil render)
    {
        render.drawImage(getImageID(), dim.x, dim.y, dim);
        render.drawString(buttonText, dim);
    }

    protected enum ButtonState
    {
        DEFAULT, HOVER, PRESSED
    }
}