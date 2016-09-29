package atraxi.client.ui.wrappers;

import atraxi.client.util.RenderUtil;
import atraxi.client.util.ResourceManager;
import atraxi.core.util.Globals;
import atraxi.core.world.World;

import java.awt.Point;
import java.awt.Polygon;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

/**
 * Created by Atraxi on 19/05/2016.
 */
public class WorldUIWrapper
{
    // constants for standard y=mx+c line equation, used in getTileAxialIndexFromPixelLocation()
    /**
     * <code>ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getHeight() / 4</code>
     */
    private static final double rise = ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getHeight() / 4.0;
    /**
     * <code>ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getWidth() / 2</code>
     */
    private static final double run = ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getWidth() / 2.0;
    /**
     * {@link #rise} / {@link #run}
     */
    private static final double gradient = rise / run;

    private Point tileSelected, tileHover, tilePressedLeftClick, tilePressedRightClick;

    private Globals.Identifiers imageDefault, imageHover, imageClick, imageSelected;

    /**
     * World local to allow different worlds to have different grid sizes, must be constant for all tiles of a given world however
     */
    private Polygon tileBounds;

    private World world;

    /**
     * Instantiates a new game world
     *
     * @param tileBounds
     * @param imageDefault  The default image for each tile
     * @param imageHover    The image used to indicate mouseover of a tile
     * @param imageClick    The image used when the player clicks a tile, but has not released the click
     * @param imageSelected The image used for the currently selected tile
     */
    public WorldUIWrapper(World world, Polygon tileBounds, Globals.Identifiers imageDefault, Globals.Identifiers imageHover, Globals.Identifiers imageClick,
                          Globals.Identifiers imageSelected)
    {
        this.world = world;
        this.tileBounds = tileBounds;
        assert Math.abs(this.tileBounds.getBounds().getHeight() - ResourceManager.getImage(imageDefault).getHeight()) < 0.001;
        assert Math.abs(this.tileBounds.getBounds().getWidth() - ResourceManager.getImage(imageDefault).getWidth()) < 0.001;

        this.imageDefault = imageDefault;
        this.imageHover = imageHover;
        this.imageClick = imageClick;
        this.imageSelected = imageSelected;

        tileHover = new Point(-1, -1);
        tilePressedLeftClick = new Point(-1, -1);
        tilePressedRightClick = new Point(-1, -1);
        tileSelected = new Point(-1, -1);
    }

    public Polygon getTileBounds()
    {
        return tileBounds;
    }

    /**
     * @return The width of each tile
     */
    public int getTileWidth()
    {
        return tileBounds.getBounds().width;
    }

    /**
     * @return The height of each tile
     */
    public int getTileHeight()
    {
        return tileBounds.getBounds().height;
    }

    public Point getTileAxialIndexFromPixelLocation(int mouseX, int mouseY)
    {
        //start with a re-arranged version of the equation from Get{X,Y}Coord()
        //used here to convert pixel location back to a tile/Entity index
        //  not a perfect solution initially due to working with rectangle bounding box containing a hexagon

        //a useful estimate however with inaccuracies between top left/right corners of
        //  hex/rect bounds (top of hex/rect evaluates to the previous row by this approximation)
        int y = (int) Math.floor(mouseY / ((3 * (double) getTileHeight()) / 4));
        int x = (int) Math.floor((mouseX - (y * (double) getTileWidth()) / 2) / getTileWidth());

        //use the estimate to offset our coordinates to [0,0]
        //  in order to simplify math with gradients to separate overlapping hex bounds
        int translatedX = mouseX - getXCoord(x, y);
        int translatedY = mouseY - getYCoord(y);

        //using pre-calculated constants for y=mx+c equation
        //  determine which corner we are in, if any, and offset index
        //  as needed to compensate for above mentioned inaccuracies with initial approximation
        if(translatedY < (-gradient * translatedX + rise))
        {//cursor is actually in the top right corner of the rectangle
            //move up left
            y--;
        }
        else if(translatedY < gradient * translatedX - rise)
        {//cursor is actually in the top right corner of the rectangle
            //move up right
            x++;
            y--;
        }
        return new Point(x, y);
    }

    public Point mousePressed(MouseEvent paramMouseEvent)
    {
        //index of the tile
        Point gridIndex = getTileAxialIndexFromPixelLocation(paramMouseEvent.getX(), paramMouseEvent.getY());
        //avoid index out of bounds errors
        if(isWithinPlayableArea(gridIndex))
        {
            switch(paramMouseEvent.getButton())
            {
                case MouseEvent.BUTTON1:
                    tilePressedLeftClick = gridIndex;
                    if(tileHover.equals(tilePressedLeftClick))
                    {
                        tileHover.setLocation(-1, -1);
                    }
                    return tilePressedLeftClick;
                case MouseEvent.BUTTON3:
                    tilePressedRightClick = gridIndex;
                    return tilePressedRightClick;
            }
        }
        return null;
    }

    public Point mouseReleased(MouseEvent paramMouseEvent)
    {//TODO: separate state tracking for left and right click
        //index of the tile that this event fired over
        Point gridIndex = getTileAxialIndexFromPixelLocation(paramMouseEvent.getX(), paramMouseEvent.getY());

        //avoid index out of bounds errors
        if (isWithinPlayableArea(gridIndex))
        {
            switch(paramMouseEvent.getButton())
            {
                case MouseEvent.BUTTON1:
                    if(tilePressedLeftClick.equals(gridIndex))
                    {
                        tileSelected = gridIndex;
                    }
                    tilePressedLeftClick.setLocation(-1, -1);

                    //we did something with this event
                    return tileSelected;
                case MouseEvent.BUTTON3:
                    if(tilePressedRightClick.equals(gridIndex))
                    {
                        tilePressedRightClick.setLocation(-1, -1);
                        return gridIndex;
                    }
                    tilePressedRightClick.setLocation(-1, -1);
            }
        }
        switch(paramMouseEvent.getButton())
        {
            case MouseEvent.BUTTON1:
                tilePressedLeftClick.setLocation(-1, -1);
                break;
            case MouseEvent.BUTTON3:
                tilePressedRightClick.setLocation(-1, -1);
        }
        return null;
    }

    public Point mouseDragged(MouseEvent paramMouseEvent)
    {
//        //index of the tile
//        Point gridIndex = getTileAxialIndexFromPixelLocation(paramMouseEvent.getX(), paramMouseEvent.getY());
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
        return null;
    }

    public Point mouseMoved(MouseEvent paramMouseEvent)
    {
        //index of the tile
        Point gridIndex = getTileAxialIndexFromPixelLocation(paramMouseEvent.getX(), paramMouseEvent.getY());
        //avoid index out of bounds errors
        if(isWithinPlayableArea(gridIndex))
        {
            tileHover = gridIndex;

            return tileHover;
        }
        else
        {
            return null;
        }
    }

    public Point mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent)
    {
//        //index of the tile
//        Point gridIndex = getTileAxialIndexFromPixelLocation(paramMouseWheelEvent.getX(), paramMouseWheelEvent.getY());
//        //avoid index out of bounds errors
//        if(isWithinPlayableArea(gridIndex))
//        {
//            //do something
//        }
//        else
//        {
//            return null;
//        }
        return null;
    }

    private boolean isWithinPlayableArea(Point point)
    {
        Point offset = World.convertAxialToOffset(point);
        return offset.x >= 0 && offset.y >= 0 && offset.x < world.getSizeX() && offset.y < world.getSizeY();
    }

    /**
     * Draw all entities within the region formed by the points from and to
     * @param render
     * @param from
     * @param to
     */
    public void paintEntities(RenderUtil render, Point from, Point to)
    {
        from = World.convertAxialToOffset(from);
        to = World.convertAxialToOffset(to);
        world.getEntitiesInRegion(from, to).values().forEach((entity) -> render.paintEntity(entity, this));
    }

    public void paintTile(RenderUtil render, int x, int y)
    {
        if(Globals.debug.getDetailedInfoLevel() > 3)
        {
            Rectangle bounds = tileBounds.getBounds();
            bounds.translate(getXCoord(x, y), getYCoord(y));
            render.drawString("Ax:" + x, getXCoord(x, y) + 50, getYCoord(y) + 50, bounds);
            render.drawString("Ay:" + y, getXCoord(x, y) + 50, getYCoord(y) + 65, bounds);
            Point offset = World.convertAxialToOffset(x, y);
            render.drawString("Ox:" + offset.x, getXCoord(x, y) + 50, getYCoord(y) + 80, bounds);
            render.drawString("Oy:" + offset.y, getXCoord(x, y) + 50, getYCoord(y) + 95, bounds);
        }

        if(tilePressedLeftClick.x == x && tilePressedLeftClick.y == y)
        {
            render.drawImage(imageClick, getXCoord(x, y), getYCoord(y), null);
        }
        else if(tileSelected.x == x && tileSelected.y == y)
        {
            render.drawImage(imageSelected, getXCoord(x, y), getYCoord(y), null);
        }
        else if(tileHover.x == x && tileHover.y == y)
        {
            render.drawImage(imageHover, getXCoord(x, y), getYCoord(y), null);
        }
        else
        {
            render.drawImage(imageDefault, getXCoord(x, y), getYCoord(y), null);
        }
    }

    public int getXCoord(int xIndex, int yIndex)
    {
        //Axial co-ordinate system
        //horizontally is the same as rectangles
        return xIndex * getTileWidth()
                        //except y-axis is at a 30degree angle, offsetting the X co-ordinate
                        //xIndex is offset by yIndex * half the width of a tile
                        + (yIndex * getTileWidth() / 2);
    }

    public int getYCoord(int yIndex)
    {
        //Hexagonal grids tile with a spacing of 3/4 the height of a hexagon, otherwise the same as rectangles
        return yIndex * (3 * getTileHeight() / 4);
    }

    public void paintTile(RenderUtil renderUtil, Point point)
    {
        paintTile(renderUtil, point.x, point.y);
    }

    public World getWorld()
    {
        return world;
    }
}