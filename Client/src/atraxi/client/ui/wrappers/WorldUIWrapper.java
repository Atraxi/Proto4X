package atraxi.client.ui.wrappers;

import atraxi.client.ui.UIElement;
import atraxi.client.ui.UserInterfaceHandler;
import atraxi.client.util.RenderUtil;
import atraxi.client.util.ResourceManager;
import atraxi.core.entities.Entity;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;
import atraxi.core.world.GridTile;
import atraxi.core.world.World;

import java.awt.Point;
import java.awt.Polygon;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

/**
 * Created by Atraxi on 19/05/2016.
 */
public class WorldUIWrapper extends World implements UIElement
{
    // constants for standard y=mx+c line equation, used in getGridTileIndex()
    /** ImageID.hexagonDefault.getImage().getHeight() / 4 */
    private static final double rise = ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getHeight() / 4;
    private static final double run = ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getWidth() / 2;
    private static final double gradient = rise / run;

    //Tiles cannot be relied upon to update their state internally in all cases (especially reverting state), as it is not feasible to have every tile process every UI event
    // (which includes mouse movement)
    //We can mathematically determine newly focused tiles, and thus it can reliably set it's own state, but not implicitly know when to revert to default (as there is no 'out of
    // focus' event)
    //So we track tiles with special state and prompt them to check the conditions for reverting to default when relevant
    //This is effectively registering tiles to always receive specific events
    private UIElement tileSelected, tileHover, tilePressed;

    /** World local to allow different worlds to have different grid sizes, must be constant for all tiles of a given world however */
    private static Rectangle tileBounds;

    /**
     * Instantiates a new game world
     *
     * @param tileBounds   The bounding shape of each tile
     * @param tileDefault  The default image for each tile
     * @param tileHover    The image used to indicate mouseover of a tile
     * @param tileClick    The image used when the player clicks a tile, but has not released the click
     * @param tileSelected The image used for the currently selected tile
     */
    public WorldUIWrapper(World world, Polygon tileBounds, Globals.Identifiers tileDefault, Globals.Identifiers tileHover, Globals.Identifiers tileClick, Globals.Identifiers tileSelected)
    {
        super(world);
        WorldUIWrapper.tileBounds = tileBounds.getBounds();
        assert Math.abs(WorldUIWrapper.tileBounds.getHeight() - ResourceManager.getImage(tileDefault).getHeight()) < 0.001;
        assert Math.abs(WorldUIWrapper.tileBounds.getWidth() - ResourceManager.getImage(tileDefault).getWidth()) < 0.001;
        tiles = new GridTileUIWrapper[world.getSizeX()][world.getSizeY()];
        for(int x = 0; x < tiles.length; x++)
        {
            for(int y = 0; y < tiles[x].length; y++)
            {
                tiles[x][y] = new GridTileUIWrapper(world.getGridTileAtIndex(x, y), tileBounds, tileDefault, tileHover, tileClick, tileSelected);
            }
        }
    }

    public static Rectangle getTileBounds()
    {
        return tileBounds;
    }

    /**
     * @return The width of each tile
     */
    public int getTileWidth()
    {
        return tileBounds.width;
    }

    /**
     * @return The height of each tile
     */
    public int getTileHeight()
    {
        return tileBounds.height;
    }

    @Override
    public UIElement mousePressed(MouseEvent paramMouseEvent)
    {
        //index of the tile
        Point gridIndex = getGridTileIndex(paramMouseEvent.getX(), paramMouseEvent.getY(), tileBounds.width, tileBounds.height);
        int x = gridIndex.x;
        int y = gridIndex.y;
        //avoid index out of bounds errors
        if(x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            //state tracking
            //tile is always set to pressed, but cannot be assumed to receive mouseReleased() event.
            tilePressed = ((GridTileUIWrapper)tiles[x][y]).mousePressed(paramMouseEvent);
            return tilePressed;
        }
        else
        {
            return null;
        }
    }

    Point getGridTileIndex(int mouseX, int mouseY, int tileWidth, int tileHeight)
    {
        int x, y;
        //start with a re-arranged version of the code from GridTile constructor
        //used here to convert pixel location back to GridTile index
        //not a perfect solution yet due to working with rectangle bounding box containing a hexagon

        //a useful estimate however with inaccuracies between top left/right corners of
        //hex/rect bounds (bottom of hex/rect is the next row by this approximation)
        y = (int) Math.floor(mouseY / ((3.0 * tileHeight) / 4));
        x = (int) Math.floor((mouseX - ((y % 2) * (double)tileWidth) / 2) / tileWidth);

        if(x >= -1 && y >= -1 && x <= tiles.length && y <= tiles[0].length)
        {
            //use the estimate to offset our coordinates to [0,0]
            //in order to simplify math with gradients to divide overlapping hex bounds
            int translatedX;
            int translatedY;
            //We have the distance to offset for each GridTile stored in each GridTile (otherwise known as the GridTile's position)
            if(x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
            {
                translatedX = mouseX - ((GridTileUIWrapper)tiles[x][y]).getXCoord();
                translatedY = mouseY - ((GridTileUIWrapper)tiles[x][y]).getYCoord();
            }
            //Except around the edges of the world, where we have to deal with locations outside the edge of the map, and thus have no pre-calculated values to use
            else
            {
                //So we just re-use the calculation from the GridTile constructor
                //This is also the original calculation for the re-arranged version a few lines above
                //The re-use here is sort-of an abuse of rounding, to get the top-left corner of the GridTile the mouse is currently over
                //horizontally is the same as rectangles
                int xCoord = x * tileWidth
                             //except offset by half the width of a hex every second row
                             //if xIndex is even, then add 0, else add dimBounds.width/2
                             + ((y % 2) * tileWidth / 2);
                int yCoord = y * (3 * tileHeight / 4);
                translatedX = mouseX - xCoord;
                translatedY = mouseY - yCoord;
            }

            //using pre-calculated constants for y=mx+c equation
            //determine which corner we are in, if any, and offset index
            //as needed to compensate for above mentioned inaccuracies with initial approximation
            if(translatedY < (-gradient * translatedX + rise))
            {
                //move up a row
                y--;
                //due to the coordinate system chosen and the general oddities of a hexagon grid
                //this situation does not always require incrementing horizontally, rather it depends
                //on the parity of the row we are on in reality
                x -= y % 2;
            }
            //cursor is actually in the top right corner of the rectangle
            else if(translatedY < gradient * translatedX - rise)
            {
                //due to the coordinate system chosen and the general oddities of a hexagon grid
                //this situation does not always require incrementing horizontally, rather it depends
                //on the parity of the row we thought we were on
                x += y % 2;
                //move up a row
                y--;
            }
            return new Point(x, y);
        }
        else
        {
            return new Point(-1, -1);
        }
    }

    @Override
    public UIElement mouseReleased(MouseEvent paramMouseEvent)
    {
        //index of the tile
        /* //mouseReleased doesn't care about the location, it is instead a trigger to finish or cancel what mousePressed() started
        Point gridIndex = getGridTileIndex(paramMouseEvent.getX(), paramMouseEvent.getY(), tileBounds.height, tileBounds.width);
        int x = gridIndex.x;
        int y = gridIndex.y;

        //avoid index out of bounds errors
        if (xIndex >= 0 && yIndex >= 0 && xIndex < tiles.length && yIndex < tiles[0].length)
        {*/
        //if the mousePressed() event was within the playable area. if not the linked mouseReleased() is irrelevant
        if(tilePressed != null)
        {
            //pass this event to the tile that actually cares about it
            tilePressed = tilePressed.mouseReleased(paramMouseEvent);
            //if the pressed tile was 'clicked' (mouseDown+Up in the same region) the tile will return itself
            //a clicked tile is the 'select' action, thus we need to clear the previous selection
            if(tilePressed != null)
            {
                //if there is a tile already selected, and is not the same as the 'newly' selected tile
                if(tileSelected != null && tilePressed != tileSelected)
                {
                    //mouseReleased() always fires on a previously pressed tile (i.e. mousePressed())
                    //therefore mouseReleased() on a 'selected' tile can be used to tell us to clear selection
                    tileSelected.mouseReleased(paramMouseEvent);
                }
                //update the selected tile the the new selected tile
                tileSelected = tilePressed;
            }
            //the mouse click has been fully handled, clear the mousePressed() state tracking
            tilePressed = null;

            //we did something with this event
            return tileSelected;
        }
        /*}*/
        return null;
    }

    @Override
    public UIElement mouseDragged(MouseEvent paramMouseEvent)
    {
        //index of the tile
        Point gridIndex = getGridTileIndex(paramMouseEvent.getX(), paramMouseEvent.getY(), tileBounds.width, tileBounds.height);
        int x = gridIndex.x;
        int y = gridIndex.y;
        //avoid index out of bounds errors
        if(x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            if(tileHover != null)
            {
                tileHover.mouseDragged(paramMouseEvent);
            }
            tileHover = ((GridTileUIWrapper)tiles[x][y]).mouseDragged(paramMouseEvent);

            return tileHover;
        }
        else
        {
            return null;
        }
    }

    @Override
    public UIElement mouseMoved(MouseEvent paramMouseEvent)
    {
        //index of the tile
        Point gridIndex = getGridTileIndex(paramMouseEvent.getX(), paramMouseEvent.getY(), tileBounds.width, tileBounds.height);
        int x = gridIndex.x;
        int y = gridIndex.y;
        //avoid index out of bounds errors
        if(x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            if(tileHover != null)
            {
                tileHover.mouseMoved(paramMouseEvent);
            }
            tileHover = ((GridTileUIWrapper)tiles[x][y]).mouseMoved(paramMouseEvent);

            return tileHover;
        }
        else
        {
            return null;
        }
    }

    @Override
    public UIElement mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent)
    {
        //index of the tile
        Point gridIndex = getGridTileIndex(paramMouseWheelEvent.getX(), paramMouseWheelEvent.getY(), tileBounds.width, tileBounds.height);
        int x = gridIndex.x;
        int y = gridIndex.y;
        //avoid index out of bounds errors
        if(x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            return ((GridTileUIWrapper)tiles[x][y]).mouseWheelMoved(paramMouseWheelEvent);
        }
        else
        {
            return null;
        }
    }

    @Override
    public void paint(RenderUtil render)
    {
        //TODO: Culling, maybe select tiles at top/left + bottom/right corners for relevant tile ranges?
        for(GridTile[] tileRow : tiles)
        {
            for(GridTile tile : tileRow)
            {
                ((GridTileUIWrapper)tile).paint(render);
            }
        }
    }

    private enum TileState
    {
        DEFAULT, HOVER, PRESSED, SELECTED
    }

    public class GridTileUIWrapper extends GridTile implements UIElement
    {
        private Globals.Identifiers imageDefault, imageHover, imageClick, imageSelected;
        //previousState is used to roll rollback a mouse press+drag
        private TileState state, previousState;
        private int xCoord, yCoord;
        private Polygon dim;

        private GridTileUIWrapper(GridTile gridTile, Globals.Identifiers imageDefault, Globals.Identifiers imageHover, Globals.Identifiers imageClick, Globals.Identifiers imageSelected)
        {
            super(gridTile);
            this.imageDefault = imageDefault;
            this.imageHover = imageHover;
            this.imageClick = imageClick;
            this.imageSelected = imageSelected;
            state = TileState.DEFAULT;
        }

        GridTileUIWrapper(GridTile gridTile, Polygon dim, Globals.Identifiers imageDefault, Globals.Identifiers imageHover, Globals.Identifiers imageClick, Globals.Identifiers imageSelected)
        {
            this(gridTile, imageDefault, imageHover, imageClick, imageSelected);
            Rectangle dimBounds = dim.getBounds();
            //horizontally is the same as rectangles
            xCoord = gridTile.getXIndex() * dimBounds.width
                     //except offset by half the width of a hex every second row
                     //if xIndex is even, then add 0, else add dimBounds.width/2
                     + ((gridTile.getYIndex() % 2) * dimBounds.width / 2);
            yCoord = gridTile.getYIndex() * (3 * dimBounds.height / 4);
            this.dim = new Polygon(dim.xpoints, dim.ypoints, dim.npoints);
            this.dim.translate(xCoord, yCoord);
        }

        @Override
        public UIElement mousePressed(MouseEvent paramMouseEvent)
        {
            boolean mouseInBounds = getGridTileIndex(paramMouseEvent.getX(), paramMouseEvent.getY(), WorldUIWrapper.getTileBounds().width, WorldUIWrapper
                    .getTileBounds().height)
                                                  .equals(new Point(getXIndex(), getYIndex()));
            if(mouseInBounds)
            {
                //store previous tile state in-case the click is cancelled
                //hover is not a meaningful state for us to revert to, so just set to default for that case
                if(state == TileState.HOVER)
                {
                    previousState = TileState.DEFAULT;
                }
                else
                {
                    previousState = state;
                }
                state = TileState.PRESSED;
                return this;
            }
            //should never happen unless math fails to determine the correct tile
            else
            {
                Logger.log(Logger.LogLevel.debug, new String[]{"World.GridTile mousePressed called without mouse in bounds. Should never happen. Indicates an error or edge case " +
                                                               "in click coordinates -> tile index math"});
                state = TileState.DEFAULT;
                return null;
            }
        }

        @Override
        public UIElement mouseReleased(MouseEvent paramMouseEvent)
        {
            //if we are finishing up from a mousePressed() event on this tile
            if(state == TileState.PRESSED)
            {
                boolean mouseInBounds = getGridTileIndex(paramMouseEvent.getX(), paramMouseEvent.getY(), tileBounds.width, tileBounds.height).equals(new Point(getXIndex(),
                                                                                                                                                               getYIndex()));
                //if the start (assumed at this point since we are 'pressed') of the click and the end are both on this tile
                //i.e. we are clicked
                if(mouseInBounds)
                {
                    //set the selected state
                    state = TileState.SELECTED;
                    UserInterfaceHandler.setSelectedTile(this);
                    return this;
                }
                //the click was cancelled
                else
                {
                    //revert the state
                    state = previousState;
                }
            }
            //mouseReleased() always fires on a previously pressed tile (i.e. mousePressed())
            //therefore mouseReleased() on a 'selected' tile can be used to tell us to clear selection
            else if(state == TileState.SELECTED)
            {
                state = TileState.DEFAULT;
                previousState = state;
            }
            return null;
        }

        @Override
        public UIElement mouseDragged(MouseEvent paramMouseEvent)
        {
            return null;
        }

        @Override
        public UIElement mouseMoved(MouseEvent paramMouseEvent)
        {
            boolean mouseInBounds = getGridTileIndex(paramMouseEvent.getX(), paramMouseEvent.getY(), tileBounds.width, tileBounds.height).equals(new Point(getXIndex(),
                                                                                                                                                           getYIndex()));
            if(mouseInBounds)
            {
                if(state == TileState.DEFAULT)
                {
                    state = TileState.HOVER;
                }
                return this;
            }
            else if(state == TileState.HOVER)
            {
                state = TileState.DEFAULT;
            }
            return null;
        }

        @Override
        public UIElement mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent)
        {
            return null;
        }

        @Override
        public void paint(RenderUtil render)
        {
            if(Globals.debug.getDetailedInfoLevel() > 3)
            {
                render.drawString("x:" + getXIndex(), xCoord + 50, yCoord + 50, dim.getBounds());
                render.drawString("y:" + getYIndex(), xCoord + 50, yCoord + 60, dim.getBounds());
            }
            switch(state)
            {
                case DEFAULT:
                    render.drawImage(imageDefault, xCoord, yCoord, null);
                    break;
                case HOVER:
                    render.drawImage(imageHover, xCoord, yCoord, null);
                    break;
                case PRESSED:
                    render.drawImage(imageClick, xCoord, yCoord, null);
                    break;
                case SELECTED:
                    render.drawImage(imageSelected, xCoord, yCoord, null);
            }
            if(getEntity() != null)
            {
                Entity entity = getEntity();
                render.paintEntity(entity);
            }
        }

        public int getXCoord()
        {
            return xCoord;
        }

        public int getYCoord()
        {
            return yCoord;
        }
    }
}
