package atraxi.client.ui

import atraxi.client.util.RenderUtil
import atraxi.client.util.ResourceManager
import atraxi.core.util.Globals

import java.awt.Rectangle
import java.awt.event.MouseEvent
import java.awt.event.MouseWheelEvent
import java.awt.image.BufferedImage

/**
 * Created by Atraxi on 6/10/2015.
 */
class InfoPanel(private val dim: Rectangle, private val background: Globals.Identifiers, private val icon: Globals.Identifiers, private val health: Int, private val supply: Int, private val ammunition: Int,
                private val specialModules: Array<String>) : UIElement {


    constructor(dim: Rectangle, icon: Globals.Identifiers, health: Int, supply: Int, ammunition: Int) : this(dim, Globals.Identifiers.infoPanelDefault, icon, health, supply, ammunition, arrayOfNulls<String>(0)) {
    }

    override fun mousePressed(paramMouseEvent: MouseEvent): UIElement? {
        return null
    }

    override fun mouseReleased(paramMouseEvent: MouseEvent): UIElement? {
        return null
    }

    override fun mouseDragged(paramMouseEvent: MouseEvent): UIElement? {
        return null
    }

    override fun mouseMoved(paramMouseEvent: MouseEvent): UIElement? {
        return null
    }

    override fun mouseWheelMoved(paramMouseWheelEvent: MouseWheelEvent): UIElement? {
        return null
    }

    override fun paint(render: RenderUtil, hasTurnEnded: Boolean) {
        //TODO RenderUtil with a single public method taking a lambda equivalent of this paint method, and a Rectangle to set Graphics2D clip area
        render.drawImage(background, dim.x, dim.y, dim)
        render.drawImage(icon, dim.x + 5, dim.y + 5, dim)
        val icon = ResourceManager.getImage(this.icon)
        for (module in specialModules) {
            render.drawString(module, icon.width + 10 + dim.x, 5 + dim.y, dim)
        }
        render.drawString(Integer.toString(health), 5 + dim.x, icon.getHeight(null) + 10 + dim.y, dim)
        render.drawString(Integer.toString(supply), 5 + dim.x, icon.getHeight(null) + 20 + dim.y, dim)
        render.drawString(Integer.toString(ammunition), 5 + dim.x, icon.getHeight(null) + 30 + dim.y, dim)
        if (Globals.debug!!.detailedInfoLevel > 4) {
            render.drawRect(dim.x, dim.y, dim.width, dim.height)
        }
    }
}
