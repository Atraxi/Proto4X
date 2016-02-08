package atraxi.ui;

import atraxi.util.CheckedRender;

import java.awt.*;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

/**
 * Created by jdog- on 7/02/2016.
 */
public class ScrollBar implements UIElement
{

    private Rectangle dim;
    private int position;
    private boolean vertical;
    private Button left, right, drag;

    public ScrollBar(Rectangle dim)
    {
        this.dim = dim;
    }

    @Override
    public UIElement mousePressed(MouseEvent paramMouseEvent)
    {
        UIElement resultElement = left.mousePressed(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = right.mousePressed(paramMouseEvent);
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
        UIElement resultElement = left.mouseReleased(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = right.mouseReleased(paramMouseEvent);
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
        UIElement resultElement = left.mouseDragged(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = right.mouseDragged(paramMouseEvent);
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
        UIElement resultElement = left.mouseMoved(paramMouseEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = right.mouseMoved(paramMouseEvent);
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
        UIElement resultElement = left.mouseWheelMoved(paramMouseWheelEvent);
        if(resultElement!=null)
        {
            return resultElement;
        }
        resultElement = right.mouseWheelMoved(paramMouseWheelEvent);
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

    }
}
