package atraxi.client.ui

import atraxi.client.util.RenderUtil
import atraxi.client.util.ResourceManager
import atraxi.core.util.Globals

import java.awt.Rectangle
import java.awt.event.MouseEvent
import java.awt.event.MouseWheelEvent

class Button
/**

 * @param imageIDDefault
 * *
 * @param imageIDHover
 * *
 * @param imageIDPressed
 * *
 * @param x Position
 * *
 * @param y Position
 * *
 * @param buttonText
 * *
 * @param action Executed when pressed
 */
(private val imageIDDefault: Globals.Identifiers, private val imageIDHover: Globals.Identifiers, private val imageIDPressed: Globals.Identifiers, x: Int, y: Int, private val buttonText: String, private val action: CustomCallable<UIStackNode, Void>) : UIElement {
    //protected int x, y;
    var dim: Rectangle
    protected var state = ButtonState.DEFAULT
    var parent: UIStackNode? = null

    init {

        val width = ResourceManager.getImage(imageIDDefault).getWidth(null)
        val height = ResourceManager.getImage(imageIDDefault).getHeight(null)

        dim = Rectangle(x, y, width, height)
    }

    val imageID: Globals.Identifiers
        get() {
            when (state) {
                Button.ButtonState.HOVER -> return imageIDHover
                Button.ButtonState.PRESSED -> return imageIDPressed
                else -> return imageIDDefault
            }
        }

    protected fun executeAction() {
        action.call(parent)
    }

    override fun mousePressed(paramMouseEvent: MouseEvent): UIElement? {
        if (dim.contains(paramMouseEvent.point)) {
            state = ButtonState.PRESSED
            return this
        }
        return null
    }

    override fun mouseReleased(paramMouseEvent: MouseEvent): UIElement? {
        if (state == ButtonState.PRESSED) {
            if (dim.contains(paramMouseEvent.point)) {
                executeAction()
                state = ButtonState.HOVER
                return this
            } else {
                state = ButtonState.DEFAULT
            }
        }
        return null
    }

    override fun mouseDragged(paramMouseEvent: MouseEvent): UIElement? {
        return null
    }

    override fun mouseMoved(paramMouseEvent: MouseEvent): UIElement? {
        if (dim.contains(paramMouseEvent.point) && state == ButtonState.DEFAULT) {
            state = ButtonState.HOVER
            return this
        } else if (!dim.contains(paramMouseEvent.point) && state == ButtonState.HOVER) {
            state = ButtonState.DEFAULT
        }
        return null
    }

    override fun mouseWheelMoved(paramMouseWheelEvent: MouseWheelEvent): UIElement? {
        return null
    }

    override fun paint(render: RenderUtil, hasTurnEnded: Boolean) {
        render.drawImage(imageID, dim.x, dim.y, dim)
        render.drawString(buttonText, dim)
    }

    protected enum class ButtonState {
        DEFAULT, HOVER, PRESSED
    }
}