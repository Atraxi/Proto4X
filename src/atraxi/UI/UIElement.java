package atraxi.UI;

import java.awt.Graphics2D;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

public interface UIElement
{
    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseListener#mousePressed(MouseEvent)
     */
    public UIElement mousePressed (MouseEvent paramMouseEvent);

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseListener#mouseReleased(MouseEvent)
     */
    public UIElement mouseReleased (MouseEvent paramMouseEvent);

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseListener#mouseEntered(MouseEvent)
     */
    public UIElement mouseEntered (MouseEvent paramMouseEvent);

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseListener#mouseExited(MouseEvent)
     */
    public UIElement mouseExited (MouseEvent paramMouseEvent);

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseMotionListener#mouseDragged(MouseEvent)
     */
    public UIElement mouseDragged (MouseEvent paramMouseEvent);

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseMotionListener#mouseMoved(MouseEvent)
     */
    public UIElement mouseMoved (MouseEvent paramMouseEvent);

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * @see java.awt.event.MouseWheelListener#mouseWheelMoved(MouseWheelEvent)
     */
    public UIElement mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent);

    public void paint(Graphics2D graphics);
}
