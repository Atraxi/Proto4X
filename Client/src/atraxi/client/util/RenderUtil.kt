package atraxi.client.util

import atraxi.client.ui.UIElement
import atraxi.core.entities.Entity
import atraxi.core.util.Globals
import atraxi.core.util.Logger

import java.awt.Color
import java.awt.FontMetrics
import java.awt.Graphics2D
import java.awt.Rectangle
import java.awt.Shape
import java.awt.geom.AffineTransform
import java.awt.geom.Rectangle2D
import java.awt.image.ImageObserver
import java.util.ArrayList
import java.util.Arrays

/**
 * Created by Atraxi on 10/10/2015.
 */
class RenderUtil {
    /**
     * The current graphics context
     */
    private var g2d: Graphics2D? = null

    fun setG2d(g2d: Graphics2D) {
        this.g2d = g2d
    }

    /**
     * Draw the specified String to the screen centered within it's bounding area, checking if it is fully contained within it's provided bounding area, and logging a warning if not
     * @param stringToDraw
     * *
     * @param containingArea
     */
    fun drawString(stringToDraw: String, containingArea: Rectangle) {
        val fontMetrics = g2d!!.fontMetrics
        val stringDim = fontMetrics.getStringBounds(stringToDraw, g2d)
        drawString(stringToDraw, (containingArea.centerX - stringDim.width / 2).toInt(), (containingArea.centerY + stringDim.height / 2).toInt(), containingArea, stringDim)
    }

    /**
     * Draw the specified String to the screen at the given x, y co-ordinates, checking if it is fully contained within it's provided container area, and logging a warning if not
     * @param stringToDraw
     * *
     * @param x
     * *
     * @param y
     * *
     * @param containingArea
     */
    fun drawString(stringToDraw: String, x: Int, y: Int, containingArea: Rectangle) {
        val fontMetrics = g2d!!.fontMetrics
        val stringDim = fontMetrics.getStringBounds(stringToDraw, g2d)
        drawString(stringToDraw, x, y, containingArea, stringDim)
    }

    /**
     * Draw the specified String to the screen at the given x, y co-ordinates, checking if the String dimensions are fully contained within it's container bounding area, and logging a warning if not
     * @param stringToDraw
     * *
     * @param x
     * *
     * @param y
     * *
     * @param containingArea
     * *
     * @param stringDim
     */
    private fun drawString(stringToDraw: String, x: Int, y: Int, containingArea: Rectangle?, stringDim: Rectangle2D) {
        if (containingArea == null) {
            throw NullPointerException("containingArea cannot be null")
        }
        if (Globals.debug!!.detailedInfoLevel > 0 && (x < containingArea.x ||
                y < containingArea.y ||
                containingArea.getWidth() + containingArea.x < stringDim.width + x ||
                containingArea.getHeight() + containingArea.y < stringDim.height + y)) {//This is excessively detailed logging, but a string overflowing it's container is also a simple oversight that looks sloppy. This safeguards against that happening as
            // long as the logs are watched occasionally
            //throw new IllegalArgumentException("String overflowed container:\n" + stringToDraw);//Ideally this would be a hard crash (in debug only) but Swing handles the exception internally so it's just obscure console spam
            val stackTrace = ArrayList<String>()
            stackTrace.add("String overflowed container")
            stackTrace.add("String:\"$stringToDraw\" x:$x y:$y")
            Arrays.stream(Thread.currentThread().stackTrace).forEach { e ->
                if (e.toString().contains("atraxi")) {
                    stackTrace.add(e.toString())
                }
            }
            Logger.log(Logger.LogLevel.warning, stackTrace.toTypedArray())
        }

        g2d!!.drawString(stringToDraw, x, y)
        if (Globals.debug!!.detailedInfoLevel >= 4) {
            g2d!!.drawRect(x, (y - stringDim.height + g2d!!.fontMetrics.maxDescent).toInt(),
                    stringDim.width.toInt(),
                    stringDim.height.toInt())
        }
    }

    /**
     * Draw the specified [Image][java.awt.image.BufferedImage] to the screen at the given x, y co-ordinates in world space, checking if the Image dimensions are fully
     * contained within it's container bounding area, and logging a warning if not
     * @param imageID
     * *
     * @param x
     * *
     * @param y
     * *
     * @param containingArea
     */
    fun drawImage(imageID: Globals.Identifiers, x: Int, y: Int, containingArea: Rectangle?) {
        if (Globals.debug!!.detailedInfoLevel > 0 && containingArea != null && (x < containingArea.x ||
                y < containingArea.y ||
                x + ResourceManager.getImage(imageID).width > containingArea.x + containingArea.width ||
                y + ResourceManager.getImage(imageID).height > containingArea.y + containingArea.height)) {
            val stackTrace = ArrayList<String>()
            stackTrace.add("Image overflowed container")
            stackTrace.add("Image:\"" + imageID.name + "\" x:" + x + " y:" + y)
            Arrays.stream(Thread.currentThread().stackTrace).forEach { e ->
                if (e.toString().contains("atraxi")) {
                    stackTrace.add(e.toString())
                }
            }
            Logger.log(Logger.LogLevel.warning, stackTrace.toTypedArray())
        }
        g2d!!.drawImage(ResourceManager.getImage(imageID), x, y, null)
    }

    /**
     * Exposes [drawRect()][java.awt.Graphics2D.drawRect] from the underlying [Graphics2D] object.
     * @see
     * @param x
     * *
     * @param y
     * *
     * @param width
     * *
     * @param height
     */
    fun drawRect(x: Int, y: Int, width: Int, height: Int) {
        g2d!!.drawRect(x, y, width, height)
    }

    /**
     * Sets the [Color], and calls the [fill()][java.awt.Graphics2D.fill] method for the underlying [Graphics2D] object.
     * Then resets the color.
     * @param color
     * *
     * @param dim
     */
    fun fill(color: Color?, dim: Rectangle) {
        val originalColor = g2d!!.color
        if (color != null) {
            g2d!!.color = color
        }

        g2d!!.fill(dim)

        g2d!!.color = originalColor
    }

    /**
     * This calls [paint()][UIElement.paint] on the provided [UIElement], but sets the [Graphics2D] clip area set to the [Rectangle] dim.
     * The original clip area is stored internally and restored before this method returns.
     * @see Graphics2D.setClip
     * @see Graphics2D.getClip
     * @param uiElement
     * *
     * @param dim
     */
    fun paintWithinBounds(uiElement: UIElement, dim: Rectangle?, hasTurnEnded: Boolean) {
        val originalClip = g2d!!.clip
        if (dim != null) {
            g2d!!.clip = dim
        }

        uiElement.paint(this, hasTurnEnded)

        g2d!!.clip = originalClip
    }

    fun drawImage(imageID: Globals.Identifiers, transform: AffineTransform, observer: ImageObserver?) {
        g2d!!.drawImage(ResourceManager.getImage(imageID), transform, observer)
    }

    fun paintEntity(entity: Entity) {
        drawImage(entity.type,
                AffineTransform.getRotateInstance(entity.orientationInRadians,
                        (ResourceManager.getImage(entity.type).width / 2).toDouble(),
                        (ResourceManager.getImage(entity.type).height / 2).toDouble()),
                null)
    }
}
