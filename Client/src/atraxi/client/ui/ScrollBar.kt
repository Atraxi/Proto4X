package atraxi.client.ui

import atraxi.client.util.RenderUtil
import atraxi.client.util.ResourceManager
import atraxi.core.util.Globals

import java.awt.Color
import java.awt.Rectangle
import java.awt.event.MouseEvent
import java.awt.event.MouseWheelEvent

/**
 * Created by Atraxi on 7/02/2016.
 */
class ScrollBar
/**

 * @param length The length of this scroll bar, EXCLUDING the size of the buttons at each end. Used with the button dimensions to determine the overall size of the scroll bar.
 * *
 * @param backgroundColor
 * *
 * @param buttonDefault
 * *
 * @param buttonHover
 * *
 * @param buttonPressed
 */
(private val length: Int, private val backgroundColor: Color, private val isVertical: Boolean, x: Int, y: Int, internal var parent: UIElement, buttonDefault: Globals.Identifiers, buttonHover: Globals.Identifiers, buttonPressed: Globals.Identifiers) : UIElement {
    private val dim: Rectangle
    private var position: Int = 0
    private val buttonA: Button
    private var buttonB: Button? = null
    private val drag: Button? = null

    init {
        dim = Rectangle(length, ResourceManager.getImage(buttonDefault).height)
        buttonA = Button(buttonDefault, buttonHover, buttonPressed, x, y, "") { scrollBar ->
            (scrollBar as ScrollBar).moveLeft()
            null
        }
        if (isVertical) {
            buttonB = Button(buttonDefault, buttonHover, buttonPressed, x, y + length, "") { scrollBar ->
                (scrollBar as ScrollBar).moveRight()
                null
            }
        }
    }

    private fun moveRight() {
        position++
    }

    private fun moveLeft() {
        position--
    }

    override fun mousePressed(paramMouseEvent: MouseEvent): UIElement? {
        var resultElement = buttonA.mousePressed(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }
        resultElement = buttonB!!.mousePressed(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }
        resultElement = drag!!.mousePressed(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }

        if (dim.contains(paramMouseEvent.point)) {
            return this
        } else {
            return null
        }
    }

    override fun mouseReleased(paramMouseEvent: MouseEvent): UIElement? {
        var resultElement = buttonA.mouseReleased(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }
        resultElement = buttonB!!.mouseReleased(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }
        resultElement = drag!!.mouseReleased(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }
        return null
    }

    override fun mouseDragged(paramMouseEvent: MouseEvent): UIElement? {
        var resultElement = buttonA.mouseDragged(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }
        resultElement = buttonB!!.mouseDragged(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }
        resultElement = drag!!.mouseDragged(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }
        return null
    }

    override fun mouseMoved(paramMouseEvent: MouseEvent): UIElement? {
        var resultElement = buttonA.mouseMoved(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }
        resultElement = buttonB!!.mouseMoved(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }
        resultElement = drag!!.mouseMoved(paramMouseEvent)
        if (resultElement != null) {
            return resultElement
        }
        return null
    }

    override fun mouseWheelMoved(paramMouseWheelEvent: MouseWheelEvent): UIElement? {
        var resultElement = buttonA.mouseWheelMoved(paramMouseWheelEvent)
        if (resultElement != null) {
            return resultElement
        }
        resultElement = buttonB!!.mouseWheelMoved(paramMouseWheelEvent)
        if (resultElement != null) {
            return resultElement
        }
        resultElement = drag!!.mouseWheelMoved(paramMouseWheelEvent)
        if (resultElement != null) {
            return resultElement
        }
        return null
    }

    override fun paint(render: RenderUtil, hasTurnEnded: Boolean) {
        render.fill(Color.GRAY, dim)
        buttonA.paint(render, hasTurnEnded)
        buttonB!!.paint(render, hasTurnEnded)
        drag!!.paint(render, hasTurnEnded)
    }
}
