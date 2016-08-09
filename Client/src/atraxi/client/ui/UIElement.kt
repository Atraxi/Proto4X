package atraxi.client.ui

import atraxi.client.util.RenderUtil

import java.awt.event.MouseEvent
import java.awt.event.MouseWheelEvent

interface UIElement {
    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * *
     * @see java.awt.event.MouseListener.mousePressed
     */
    fun mousePressed(paramMouseEvent: MouseEvent): UIElement

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * *
     * @see java.awt.event.MouseListener.mouseReleased
     */
    fun mouseReleased(paramMouseEvent: MouseEvent): UIElement

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * *
     * @see java.awt.event.MouseMotionListener.mouseDragged
     */
    fun mouseDragged(paramMouseEvent: MouseEvent): UIElement

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * *
     * @see java.awt.event.MouseMotionListener.mouseMoved
     */
    fun mouseMoved(paramMouseEvent: MouseEvent): UIElement

    /**
     * @return if the event has been handled by this UIElement, then this, else null
     * *
     * @see java.awt.event.MouseWheelListener.mouseWheelMoved
     */
    fun mouseWheelMoved(paramMouseWheelEvent: MouseWheelEvent): UIElement

    fun paint(render: RenderUtil, hasTurnEnded: Boolean)
}
