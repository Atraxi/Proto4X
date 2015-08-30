package atraxi.game.UI;

import java.awt.event.MouseEvent;

public interface UIElement
{
    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseListener#mousePressed(MouseEvent)
     */
    boolean mousePressed (MouseEvent paramMouseEvent);

    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseListener#mouseReleased(MouseEvent)
     */
    boolean mouseReleased (MouseEvent paramMouseEvent);

    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseListener#mouseEntered(MouseEvent)
     */
    boolean mouseEntered (MouseEvent paramMouseEvent);

    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseListener#mouseExited(MouseEvent)
     */
    boolean mouseExited (MouseEvent paramMouseEvent);

    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseMotionListener#mouseDragged(MouseEvent)
     */
    boolean mouseDragged (MouseEvent paramMouseEvent);

    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseMotionListener#mouseMoved(MouseEvent)
     */
    boolean mouseMoved (MouseEvent paramMouseEvent);
}
