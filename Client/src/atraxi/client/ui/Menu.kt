package atraxi.client.ui

import atraxi.client.util.RenderUtil
import atraxi.client.util.ResourceManager
import atraxi.core.util.Globals

import java.awt.Point
import java.awt.Rectangle
import java.awt.event.MouseEvent
import java.awt.event.MouseWheelEvent

class Menu(//private int x, y;
        private val backgroundID: Globals.Identifiers, x: Int, y: Int, //TODO: implement image scaling, try "image.getScaledInstance(width,height,algorithm(enum));"
        private val buttons: Array<Button>) : UIElement, UIStackNode {
    private val dim: Rectangle
    override var nextNode: UIStackNode? = null
    override var previousNode: UIStackNode? = null
    private var movePoint: Point? = null

    init {

        val width = ResourceManager.getImage(backgroundID).getWidth(null)
        val height = ResourceManager.getImage(backgroundID).getHeight(null)

        dim = Rectangle(x - width / 2, y - height / 2, width, height)

        for (button in buttons) {
            button.dim.setLocation(button.dim.x + dim.x, button.dim.y + dim.y)
            button.parent = this
        }
    }

    override fun paint(render: RenderUtil, hasTurnEnded: Boolean) {
        render.drawImage(backgroundID, dim.x, dim.y, dim)
        for (button in buttons) {
            render.paintWithinBounds(button, dim, hasTurnEnded)
        }
    }

    override fun mousePressed(paramMouseEvent: MouseEvent): UIElement? {
        for (button in buttons) {
            val resultElement = button.mousePressed(paramMouseEvent)
            if (resultElement != null) {
                return resultElement
            }
        }
        if (dim.contains(paramMouseEvent.point)) {
            if (paramMouseEvent.button == MouseEvent.BUTTON1) {
                movePoint = paramMouseEvent.point
            }
            return this
        }
        return null
    }

    override fun mouseReleased(paramMouseEvent: MouseEvent): UIElement? {
        if (movePoint != null) {
            dim.x -= (movePoint!!.getX() - paramMouseEvent.x).toInt()
            dim.y -= (movePoint!!.getY() - paramMouseEvent.y).toInt()

            for (button in buttons) {
                button.dim.x -= (movePoint!!.getX() - paramMouseEvent.x).toInt()
                button.dim.y -= (movePoint!!.getY() - paramMouseEvent.y).toInt()
            }
            movePoint = null
            return this
        }
        for (button in buttons) {
            val resultElement = button.mouseReleased(paramMouseEvent)
            if (resultElement != null) {
                return resultElement
            }
        }
        if (dim.contains(paramMouseEvent.point)) {
            return this
        }
        return null
    }

    override fun mouseDragged(paramMouseEvent: MouseEvent): UIElement? {
        if (movePoint != null) {
            dim.x -= (movePoint!!.getX() - paramMouseEvent.x).toInt()
            dim.y -= (movePoint!!.getY() - paramMouseEvent.y).toInt()

            for (button in buttons) {
                button.dim.x -= (movePoint!!.getX() - paramMouseEvent.x).toInt()
                button.dim.y -= (movePoint!!.getY() - paramMouseEvent.y).toInt()
            }
            movePoint = paramMouseEvent.point
            return this
        }
        for (button in buttons) {
            val resultElement = button.mouseDragged(paramMouseEvent)
            if (resultElement != null) {
                return resultElement
            }
        }
        return null
    }

    override fun mouseMoved(paramMouseEvent: MouseEvent): UIElement? {
        for (button in buttons) {
            val resultElement = button.mouseMoved(paramMouseEvent)
            if (resultElement != null) {
                return resultElement
            }
        }
        if (dim.contains(paramMouseEvent.point)) {
            return this
        }
        return null
    }

    override fun mouseWheelMoved(paramMouseWheelEvent: MouseWheelEvent): UIElement? {
        for (button in buttons) {
            val resultElement = button.mouseWheelMoved(paramMouseWheelEvent)
            if (resultElement != null) {
                return resultElement
            }
        }
        if (dim.contains(paramMouseWheelEvent.point)) {
            return this
        }
        return null
    }
}
