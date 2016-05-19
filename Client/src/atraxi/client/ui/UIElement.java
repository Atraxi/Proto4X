package atraxi.client.ui;

import atraxi.client.util.CheckedRender;

import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

public interface UIElement
{
    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseListener#mousePressed(MouseEvent)
     */
    UIElement mousePressed(MouseEvent paramMouseEvent);

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseListener#mouseReleased(MouseEvent)
     */
    UIElement mouseReleased(MouseEvent paramMouseEvent);

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseMotionListener#mouseDragged(MouseEvent)
     */
    UIElement mouseDragged(MouseEvent paramMouseEvent);

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseMotionListener#mouseMoved(MouseEvent)
     */
    UIElement mouseMoved(MouseEvent paramMouseEvent);

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseWheelListener#mouseWheelMoved(MouseWheelEvent)
     */
    UIElement mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent);

    void paint(CheckedRender checkedRender);
}
