package atraxi.client.ui;

import atraxi.client.util.CheckedRender;
import atraxi.client.util.ResourceManager.ImageID;

import java.awt.Color;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

/**
 * Created by Atraxi on 7/02/2016.
 */
public class ScrollBar implements UIElement
{
    UIElement parent;
    private Rectangle dim;
    private int position, length;
    private boolean isVertical;
    private Button buttonA, buttonB, drag;
    private Color backgroundColor;

    /**
     *
     * @param length The length of this scroll bar, EXCLUDING the size of the buttons at each end. Used with the button dimensions to determine the overall size of the scroll bar.
     * @param backgroundColor
     * @param buttonDefault
     * @param buttonHover
     * @param buttonPressed
     */
    public ScrollBar(int length, Color backgroundColor, boolean isVertical, int x, int y, UIElement parent, ImageID buttonDefault, ImageID buttonHover, ImageID buttonPressed)
    {
        this.parent = parent;
        this.length = length;
        dim = new Rectangle(length, buttonDefault.getImage().getHeight());
        this.backgroundColor = backgroundColor;
        this.isVertical = isVertical;
        buttonA = new Button(buttonDefault, buttonHover, buttonPressed, x, y, "", (scrollBar) -> {
            ((ScrollBar)scrollBar).moveLeft();
            return null;
        });
        if(isVertical)
        {
            buttonB = new Button(buttonDefault, buttonHover, buttonPressed, x, y+length, "", (scrollBar) -> {
                ((ScrollBar)scrollBar).moveRight();
                return null;
            });
        }
    }

    private void moveRight()
    {
        position++;
    }

    private void moveLeft()
    {
        position--;
    }

    @Override
    public UIElement mousePressed(MouseEvent paramMouseEvent)
    {
        UIElement resultElement = buttonA.mousePressed(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = buttonB.mousePressed(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = drag.mousePressed(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }

        if (dim.contains(paramMouseEvent.getPoint()))
        {
            return this;
        }
        else
        {
            return null;
        }
    }

    @Override
    public UIElement mouseReleased(MouseEvent paramMouseEvent)
    {
        UIElement resultElement = buttonA.mouseReleased(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = buttonB.mouseReleased(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = drag.mouseReleased(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        return null;
    }

    @Override
    public UIElement mouseDragged(MouseEvent paramMouseEvent)
    {
        UIElement resultElement = buttonA.mouseDragged(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = buttonB.mouseDragged(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = drag.mouseDragged(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        return null;
    }

    @Override
    public UIElement mouseMoved(MouseEvent paramMouseEvent)
    {
        UIElement resultElement = buttonA.mouseMoved(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = buttonB.mouseMoved(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = drag.mouseMoved(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        return null;
    }

    @Override
    public UIElement mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent)
    {
        UIElement resultElement = buttonA.mouseWheelMoved(paramMouseWheelEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = buttonB.mouseWheelMoved(paramMouseWheelEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = drag.mouseWheelMoved(paramMouseWheelEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        return null;
    }

    @Override
    public void paint(CheckedRender render)
    {
        render.fill(Color.GRAY, dim);
        buttonA.paint(render);
        buttonB.paint(render);
        drag.paint(render);
    }
}
