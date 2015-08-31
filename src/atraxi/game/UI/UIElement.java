package atraxi.game.UI;

import java.awt.event.MouseEvent;

public interface UIElement
{

    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseListener#mousePressed(MouseEvent)
     */
    public boolean mousePressed (MouseEvent paramMouseEvent);

    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseListener#mouseReleased(MouseEvent)
     */
    public boolean mouseReleased (MouseEvent paramMouseEvent);

    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseListener#mouseEntered(MouseEvent)
     */
    public boolean mouseEntered (MouseEvent paramMouseEvent);

    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseListener#mouseExited(MouseEvent)
     */
    public boolean mouseExited (MouseEvent paramMouseEvent);

    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseMotionListener#mouseDragged(MouseEvent)
     */
    public boolean mouseDragged (MouseEvent paramMouseEvent);

    /**
     * @return true if the event occurred over this object
     * @see java.awt.event.MouseMotionListener#mouseMoved(MouseEvent)
     */
    public boolean mouseMoved (MouseEvent paramMouseEvent);

    public UIElement getNextNode();

    public void setNextNode(UIElement element);
}
