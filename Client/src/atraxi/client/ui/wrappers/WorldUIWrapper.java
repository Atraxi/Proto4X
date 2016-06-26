package atraxi.client.ui.wrappers;

import atraxi.client.util.RenderUtil;
import atraxi.client.util.ResourceManager;
import atraxi.core.entities.Entity;
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
public class WorldUIWrapper extends World
{
    // constants for standard y=mx+c line equation, used in getGridTileIndexFromPixelLocation()
    /**
     * <code>ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getHeight() / 4</code>
     */
    private static final double rise = ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getHeight() / 4;
    /**
     * <code>ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getWidth() / 2</code>
     */
    private static final double run = ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getWidth() / 2;
    /**
     * {@link #rise} / {@link #run}
     */
    private static final double gradient = rise / run;

    //Tiles cannot be relied upon to update their state internally in all cases (especially reverting state), as it is not feasible to have every tile process every UI event
    // (which includes mouse movement)
    //We can mathematically determine newly focused tiles, and thus it can reliably set it's own state, but not implicitly know when to revert to default (as there is no 'out of
    // focus' event)
    //So we track tiles with special state and prompt them to check the conditions for reverting to default when relevant
    //This is effectively registering tiles to always receive specific events
    private Point tileSelected, tileHover, tilePressed;

    private Globals.Identifiers imageDefault, imageHover, imageClick, imageSelected;

    /**
     * World local to allow different worlds to have different grid sizes, must be constant for all tiles of a given world however
     */
    private Polygon tileBounds;

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
        super(world);
        this.tileBounds = tileBounds;
        assert Math.abs(this.tileBounds.getBounds().getHeight() - ResourceManager.getImage(imageDefault).getHeight()) < 0.001;
        assert Math.abs(this.tileBounds.getBounds().getWidth() - ResourceManager.getImage(imageDefault).getWidth()) < 0.001;

        this.imageDefault = imageDefault;
        this.imageHover = imageHover;
        this.imageClick = imageClick;
        this.imageSelected = imageSelected;

        tileHover = new Point(-1, -1);
        tilePressed = new Point(-1, -1);
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

    public static Point getGridTileIndexFromPixelLocation(int mouseX, int mouseY, WorldUIWrapper world)
    {
        int x, y;
        //start with a re-arranged version of the code from GridTile constructor
        //used here to convert pixel location back to GridTile index
        //not a perfect solution yet due to working with rectangle bounding box containing a hexagon

        //a useful estimate however with inaccuracies between top left/right corners of
        //hex/rect bounds (bottom of hex/rect is the next row by this approximation)
        y = (int) Math.floor(mouseY / ((3.0 * (double) world.getTileHeight()) / 4.0));
        x = (int) Math.floor((mouseX - (y * (double) world.getTileWidth()) / 2.0) / (double) world.getTileWidth());

        //use the estimate to offset our coordinates to [0,0]
        //in order to simplify math with gradients to separate overlapping hex bounds
        int translatedX;
        int translatedY;
        //We just re-use the calculation from the GridTile constructor
        //This is also the original calculation for the re-arranged version a few lines above
        //The re-use here is basically equivalent to rounding, to get the top-left corner of the GridTile the mouse is currently over
        //--------------------------------------------------------------
        //horizontally is the same as rectangles
        int xCoord = x * world.getTileWidth()
                     //except offset by half the width of a hex every second row
                     //if xIndex is even, then add 0, else add dimBounds.width/2
                     + (y * world.getTileWidth() / 2);
        int yCoord = y * (3 * world.getTileHeight() / 4);
        //--------------------------------------------------------------
        translatedX = mouseX - xCoord;
        translatedY = mouseY - yCoord;

        //using pre-calculated constants for y=mx+c equation
        //determine which corner we are in, if any, and offset index
        //as needed to compensate for above mentioned inaccuracies with initial approximation
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
        Point gridIndex = getGridTileIndexFromPixelLocation(paramMouseEvent.getX(), paramMouseEvent.getY(), this);
        //avoid index out of bounds errors
        if(gridIndex.x >= 0 && gridIndex.y >= 0 && gridIndex.x < entities.length && gridIndex.y < entities[0].length)
        {
            tilePressed = gridIndex;
            if(tileHover.equals(tilePressed))
            {
                tileHover.setLocation(-1, -1);
            }
            return tilePressed;
        }
        else
        {
            return null;
        }
    }
    public Point mouseReleased(MouseEvent paramMouseEvent)
    {//TODO: separate state tracking for left and right click
        //index of the tile that this event fired over
        Point gridIndex = getGridTileIndexFromPixelLocation(paramMouseEvent.getX(), paramMouseEvent.getY(), this);

        //avoid index out of bounds errors
        if (gridIndex.x >= 0 && gridIndex.y >= 0 && gridIndex.x < entities.length && gridIndex.y < entities[0].length)
        {
            //if the mousePressed() event was within the playable area. if not the linked mouseReleased() is irrelevant
            if(tilePressed.x >= 0 && tilePressed.y >= 0)
            {
                if(tilePressed.equals(gridIndex))
                {
                    tileSelected = gridIndex;
                }
                tilePressed.setLocation(-1, -1);

                //we did something with this event
                return tileSelected;
            }
        }
        tilePressed.setLocation(-1, -1);
        return null;
    }

    public Point mouseDragged(MouseEvent paramMouseEvent)
    {
//        //index of the tile
//        Point gridIndex = getGridTileIndexFromPixelLocation(paramMouseEvent.getX(), paramMouseEvent.getY(), this);
//        //avoid index out of bounds errors
//        if(gridIndex.x >= 0 && gridIndex.y >= 0 && gridIndex.x < entities.length && gridIndex.y < entities[0].length)
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
        Point gridIndex = getGridTileIndexFromPixelLocation(paramMouseEvent.getX(), paramMouseEvent.getY(), this);
        //avoid index out of bounds errors
        if(gridIndex.x >= 0 && gridIndex.y >= 0 && gridIndex.x < entities.length && gridIndex.y < entities[0].length)
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
        //index of the tile
        Point gridIndex = getGridTileIndexFromPixelLocation(paramMouseWheelEvent.getX(), paramMouseWheelEvent.getY(), this);
        //avoid index out of bounds errors
//        if(gridIndex.x >= 0 && gridIndex.y >= 0 && gridIndex.x < entities.length && gridIndex.y < entities[0].length)
//        {
//            //do something
//        }
//        else
//        {
//            return null;
//        }
        return null;
    }

    public void paint(RenderUtil render, boolean hasTurnEnded)
    {
        //TODO: Culling, maybe select entities at top/left + bottom/right corners for relevant tile ranges?
        for(Entity[] entityRow : entities)
        {
            for(Entity entity : entityRow)
            {
                if(entity != null)
                {
                    render.paintEntity(entity);
                }
            }
        }
    }

    public void paintTile(RenderUtil render, boolean hasTurnEnded, int x, int y)
    {
        if(Globals.debug.getDetailedInfoLevel() > 3)
        {
            Rectangle bounds = tileBounds.getBounds();
            bounds.translate(getXCoord(x, y, this), getYCoord(y, this));
            render.drawString("x:" + x, getXCoord(x, y, this) + 50, getYCoord(y, this) + 50, bounds);
            render.drawString("y:" + y, getXCoord(x, y, this) + 50, getYCoord(y, this) + 60, bounds);
        }
        if(tilePressed.x == x && tilePressed.y == y)
        {
            render.drawImage(imageClick, getXCoord(x, y, this), getYCoord(y, this), null);
        }
        else if(tileSelected.x == x && tileSelected.y == y)
        {
            render.drawImage(imageSelected, getXCoord(x, y, this), getYCoord(y, this), null);
        }
        else if(tileHover.x == x && tileHover.y == y)
        {
            render.drawImage(imageHover, getXCoord(x, y, this), getYCoord(y, this), null);
        }
        else
        {
            render.drawImage(imageDefault, getXCoord(x, y, this), getYCoord(y, this), null);
        }
    }

    public static int getXCoord(int xIndex, int yIndex, WorldUIWrapper world)
    {
        //Axial co-ordinate system
        //horizontally is the same as rectangles
        return xIndex * world.getTileWidth()
                        //except y-axis is at a 30degree angle, offsetting (rectangular screen) x co-ordinate
                        //xIndex is offset by yIndex * dimBounds.width/2
                        + (yIndex * world.getTileWidth() / 2);
    }

    public static int getYCoord(int yIndex, WorldUIWrapper world)
    {
        return yIndex * (3 * world.getTileHeight() / 4);
    }
}