package atraxi.client.ui.wrappers

import atraxi.client.util.RenderUtil
import atraxi.client.util.ResourceManager
import atraxi.core.util.Globals
import atraxi.core.world.World

import java.awt.Point
import java.awt.Polygon
import java.awt.Rectangle
import java.awt.event.MouseEvent
import java.awt.event.MouseWheelEvent

/**
 * Created by Atraxi on 19/05/2016.
 */
class WorldUIWrapper
/**
 * Instantiates a new game world

 * @param tileBounds
 * *
 * @param imageDefault  The default image for each tile
 * *
 * @param imageHover    The image used to indicate mouseover of a tile
 * *
 * @param imageClick    The image used when the player clicks a tile, but has not released the click
 * *
 * @param imageSelected The image used for the currently selected tile
 */
(val world: World,
 /**
  * World local to allow different worlds to have different grid sizes, must be constant for all tiles of a given world however
  */
 val tileBounds: Polygon, private val imageDefault: Globals.Identifiers, private val imageHover: Globals.Identifiers, private val imageClick: Globals.Identifiers,
 private val imageSelected: Globals.Identifiers) {

    private var tileSelected: Point? = null
    private var tileHover: Point? = null
    private var tilePressed: Point? = null

    init {
        assert(Math.abs(this.tileBounds.bounds.getHeight() - ResourceManager.getImage(imageDefault).height) < 0.001)
        assert(Math.abs(this.tileBounds.bounds.getWidth() - ResourceManager.getImage(imageDefault).width) < 0.001)

        tileHover = Point(-1, -1)
        tilePressed = Point(-1, -1)
        tileSelected = Point(-1, -1)
    }

    /**
     * @return The width of each tile
     */
    val tileWidth: Int
        get() = tileBounds.bounds.width

    /**
     * @return The height of each tile
     */
    val tileHeight: Int
        get() = tileBounds.bounds.height

    fun mousePressed(paramMouseEvent: MouseEvent): Point? {
        //index of the tile
        val gridIndex = World.convertOffsetToAxial(getGridTileIndexFromPixelLocation(paramMouseEvent.x, paramMouseEvent.y, this))
        //avoid index out of bounds errors
        if (isWithinPlayableArea(gridIndex)) {
            tilePressed = gridIndex
            if (tileHover == tilePressed) {
                tileHover!!.setLocation(-1, -1)
            }
            return tilePressed
        } else {
            return null
        }
    }

    fun mouseReleased(paramMouseEvent: MouseEvent): Point? {//TODO: separate state tracking for left and right click
        //index of the tile that this event fired over
        val gridIndex = World.convertOffsetToAxial(getGridTileIndexFromPixelLocation(paramMouseEvent.x, paramMouseEvent.y, this))

        //avoid index out of bounds errors
        if (isWithinPlayableArea(gridIndex)) {
            //if the mousePressed() event was within the playable area. if not the linked mouseReleased() is irrelevant
            if (tilePressed!!.x >= 0 && tilePressed!!.y >= 0) {
                if (tilePressed == gridIndex) {
                    tileSelected = gridIndex
                }
                tilePressed!!.setLocation(-1, -1)

                //we did something with this event
                return tileSelected
            }
        }
        tilePressed!!.setLocation(-1, -1)
        return null
    }

    fun mouseDragged(paramMouseEvent: MouseEvent): Point? {
        //        //index of the tile
        //        Point gridIndex = convertOffsetToAxial(getGridTileIndexFromPixelLocation(paramMouseEvent.getX(), paramMouseEvent.getY(), this));
        //        //avoid index out of bounds errors
        //        if(isWithinPlayableArea(gridIndex))
        //        {
        //            tileHover = gridIndex;
        //
        //            return tileHover;
        //        }
        //        else
        //        {
        //            return null;
        //        }
        return null
    }

    fun mouseMoved(paramMouseEvent: MouseEvent): Point? {
        //index of the tile
        val gridIndex = World.convertOffsetToAxial(getGridTileIndexFromPixelLocation(paramMouseEvent.x, paramMouseEvent.y, this))
        //avoid index out of bounds errors
        if (isWithinPlayableArea(gridIndex)) {
            tileHover = gridIndex

            return tileHover
        } else {
            return null
        }
    }

    fun mouseWheelMoved(paramMouseWheelEvent: MouseWheelEvent): Point? {
        //        //index of the tile
        //        Point gridIndex = convertOffsetToAxial(getGridTileIndexFromPixelLocation(paramMouseWheelEvent.getX(), paramMouseWheelEvent.getY(), this));
        //        //avoid index out of bounds errors
        //        if(isWithinPlayableArea(gridIndex))
        //        {
        //            //do something
        //        }
        //        else
        //        {
        //            return null;
        //        }
        return null
    }

    private fun isWithinPlayableArea(point: Point): Boolean {
        val offset = World.convertAxialToOffset(point)
        return offset.x >= 0 && offset.y >= 0 && offset.x < world.sizeX && offset.y < world.sizeY
    }

    /**
     * Draw all entities within the region formed by the points from and to
     * @param render
     * *
     * @param from
     * *
     * @param to
     */
    fun paint(render: RenderUtil, from: Point, to: Point) {
        var from = from
        var to = to
        from = World.convertAxialToOffset(from)
        to = World.convertAxialToOffset(to)
        world.getEntitiesInRegion(from, to).values.forEach(Consumer<Entity> { render.paintEntity(it) })
    }

    fun paintTile(render: RenderUtil, x: Int, y: Int) {
        if (Globals.debug!!.detailedInfoLevel > 3) {
            val bounds = tileBounds.bounds
            bounds.translate(getXCoord(x, y, this), getYCoord(y, this))
            render.drawString("x:" + x, getXCoord(x, y, this) + 50, getYCoord(y, this) + 50, bounds)
            render.drawString("y:" + y, getXCoord(x, y, this) + 50, getYCoord(y, this) + 60, bounds)
        }
        if (tilePressed!!.x == x && tilePressed!!.y == y) {
            render.drawImage(imageClick, getXCoord(x, y, this), getYCoord(y, this), null)
        } else if (tileSelected!!.x == x && tileSelected!!.y == y) {
            render.drawImage(imageSelected, getXCoord(x, y, this), getYCoord(y, this), null)
        } else if (tileHover!!.x == x && tileHover!!.y == y) {
            render.drawImage(imageHover, getXCoord(x, y, this), getYCoord(y, this), null)
        } else {
            render.drawImage(imageDefault, getXCoord(x, y, this), getYCoord(y, this), null)
        }
    }

    fun paintTile(renderUtil: RenderUtil, point: Point) {
        paintTile(renderUtil, point.x, point.y)
    }

    companion object {
        // constants for standard y=mx+c line equation, used in getGridTileIndexFromPixelLocation()
        /**
         * `ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getHeight() / 4`
         */
        private val rise = (ResourceManager.getImage(Globals.Identifiers.hexagonDefault).height / 4).toDouble()
        /**
         * `ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getWidth() / 2`
         */
        private val run = (ResourceManager.getImage(Globals.Identifiers.hexagonDefault).width / 2).toDouble()
        /**
         * [.rise] / [.run]
         */
        private val gradient = rise / run

        fun getGridTileIndexFromPixelLocation(mouseX: Int, mouseY: Int, world: WorldUIWrapper): Point {
            //start with a re-arranged version of the equation from Get{X,Y}Coord()
            //used here to convert pixel location back to a tile/Entity index
            //  not a perfect solution initially due to working with rectangle bounding box containing a hexagon

            //a useful estimate however with inaccuracies between top left/right corners of
            //  hex/rect bounds (top of hex/rect is the previous row by this approximation)
            var y = Math.floor(mouseY / (3 * world.tileHeight.toDouble() / 4)).toInt()
            var x = Math.floor((mouseX - y * world.tileWidth.toDouble() / 2) / world.tileWidth).toInt()

            //use the estimate to offset our coordinates to [0,0]
            //  in order to simplify math with gradients to separate overlapping hex bounds
            val translatedX = mouseX - getXCoord(x, y, world)
            val translatedY = mouseY - getYCoord(y, world)

            //using pre-calculated constants for y=mx+c equation
            //  determine which corner we are in, if any, and offset index
            //  as needed to compensate for above mentioned inaccuracies with initial approximation
            if (translatedY < -gradient * translatedX + rise) {//cursor is actually in the top right corner of the rectangle
                //move up left
                y--
            } else if (translatedY < gradient * translatedX - rise) {//cursor is actually in the top right corner of the rectangle
                //move up right
                x++
                y--
            }
            return Point(x, y)
        }

        fun getXCoord(xIndex: Int, yIndex: Int, world: WorldUIWrapper): Int {
            //Axial co-ordinate system
            //horizontally is the same as rectangles
            return xIndex * world.tileWidth
            //except y-axis is at a 30degree angle, offsetting the X co-ordinate
            //xIndex is offset by yIndex * half the width of a tile
            +(yIndex * world.tileWidth / 2)
        }

        fun getYCoord(yIndex: Int, world: WorldUIWrapper): Int {
            //Hexagonal grids tile with a spacing of 3/4 the height of a hexagon, otherwise the same as rectangles
            return yIndex * (3 * world.tileHeight / 4)
        }
    }
}