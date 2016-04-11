package atraxi.game.world;

import atraxi.entities.Entity;
import atraxi.entities.actionQueue.Action;
import atraxi.ui.UIElement;
import atraxi.ui.UserInterfaceHandler;
import atraxi.util.CheckedRender;
import atraxi.util.Logger;
import atraxi.util.ResourceManager.ImageID;

import java.awt.Polygon;
import java.awt.Rectangle;
import java.awt.Shape;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;
import java.util.LinkedList;

public class World implements UIElement
{
    public final long seed;
    private GridTile[][] tiles;
    private Rectangle tileBounds;
    private Shape tileTemp;
    private int sizeX, sizeY;

    //Tiles cannot be relied upon to update their state internally in all cases (especially reverting state), as it is not feasible to have every tile process every UI event
    // (which includes mouse movement)
    //We can mathematically determine newly focused tiles, and thus it can reliably set it's own state, but not implicitly know when to revert to default (as there is no 'out of
    // focus' event)
    //So we track tiles with special state and prompt them to check the conditions for reverting to default when relevant

    //This is effectively registering tiles to always receive specific events
    private UIElement tileSelected, tileHover, tilePressed;

    /**
     * Instantiates a new game world
     *
     * @param tileBounds  The dimensions of each tile
     * @param sizeX       The number of tiles wide
     * @param sizeY       The number of tiles tall
     * @param tileDefault The default image for each tile
     * @param tileHover   The image used to indicate mouseover of a tile
     * @param tileClick   The image used when the player clicks a tile
     */
    public World(long seed, Rectangle tileBounds, int sizeX, int sizeY, ImageID tileDefault, ImageID tileHover, ImageID tileClick, ImageID tileSelected)
    {
        this.seed = seed;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.tileBounds = tileBounds;
        tiles = new GridTile[sizeX][sizeY];
        for (int x = 0; x < tiles.length; x++)
        {
            for (int y = 0; y < tiles[x].length; y++)
            {
                tiles[x][y] = new GridTile(x, y, tileBounds, tileDefault, tileHover, tileClick, tileSelected);
            }
        }
    }

    /**
     * Instantiates a new game world
     *
     * @param tileBounds  The bounding shape of each tile
     * @param sizeX       The number of tiles wide
     * @param sizeY       The number of tiles tall
     * @param tileDefault The default image for each tile
     * @param tileHover   The image used to indicate mouseover of a tile
     * @param tileClick   The image used when the player clicks a tile
     */
    public World(long seed, Polygon tileBounds, int sizeX, int sizeY, ImageID tileDefault, ImageID tileHover, ImageID tileClick, ImageID tileSelected)
    {
        this.seed = seed;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.tileBounds = tileBounds.getBounds();
        tiles = new GridTile[sizeX][sizeY];
        for (int x = 0; x < tiles.length; x++)
        {
            for (int y = 0; y < tiles[x].length; y++)
            {
                tiles[x][y] = new GridTile(x, y, tileBounds, tileDefault, tileHover, tileClick, tileSelected);
            }
        }
        tileTemp = tileBounds;
    }

    public int getSizeY()
    {
        return sizeY;
    }

    public int getSizeX()
    {
        return sizeX;
    }

    public int getGridSize()
    {
        return tileBounds.width;
    }

    @Override
    public UIElement mousePressed(MouseEvent paramMouseEvent)
    {
        //index of the tile
        int x = (int) Math.floor((paramMouseEvent.getX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseEvent.getY()) / tileBounds.getHeight());
        //avoid index out of bounds errors
        if (x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            //state tracking
            //tile is always set to pressed, but cannot be assumed to receive mouseReleased() event.
            tilePressed = tiles[x][y].mousePressed(paramMouseEvent);
            return tilePressed;
        }
        else
        {
            return null;
        }
    }
    @Override
    public UIElement mouseReleased(MouseEvent paramMouseEvent)
    {
        //index of the tile
        /* //mouseReleased doesn't care about the location, it is instead a trigger to finish or cancel what mousePressed() started
        int xIndex = (int) Math.floor((paramMouseEvent.getX()) / tileBounds.getWidth());
        int yIndex = (int) Math.floor((paramMouseEvent.getY()) / tileBounds.getHeight());

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
                if (tilePressed != null)
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
        int x = (int) Math.floor((paramMouseEvent.getX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseEvent.getY()) / tileBounds.getHeight());
        //avoid index out of bounds errors
        if (x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            if(tileHover != null )
            {
                tileHover.mouseDragged(paramMouseEvent);
            }
            tileHover = tiles[x][y].mouseDragged(paramMouseEvent);

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
        int x = (int) Math.floor((paramMouseEvent.getX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseEvent.getY()) / tileBounds.getHeight());
        //avoid index out of bounds errors
        if (x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            if(tileHover != null )
            {
                tileHover.mouseMoved(paramMouseEvent);
            }
            tileHover = tiles[x][y].mouseMoved(paramMouseEvent);

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
        int x = (int) Math.floor((paramMouseWheelEvent.getX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseWheelEvent.getY()) / tileBounds.getHeight());
        //avoid index out of bounds errors
        if (x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            return tiles[x][y].mouseWheelMoved(paramMouseWheelEvent);
        }
        else
        {
            return null;
        }
    }

    @Override
    public void paint(CheckedRender render)
    {
        //TODO: Culling, maybe select tiles at top/left + bottom/right corners for relevant tile ranges?
        for (GridTile[] tileRow : tiles)
        {
            for (GridTile tile : tileRow)
            {
                tile.paint(render);
            }
        }
    }

    private enum TileState
    {
        DEFAULT, HOVER, PRESSED, SELECTED
    }

    public static class GridTile implements UIElement
    {
        private ImageID imageDefault, imageHover, imageClick, imageSelected;
        //previousState is used to roll rollback a mouse press+drag
        private TileState state, previousState;
        private int xIndex, yIndex, xCoord, yCoord;
        private Shape dim;
        private LinkedList<Entity> entities;

        public GridTile(int xIndex, int yIndex, Rectangle dim, ImageID imageDefault, ImageID imageHover, ImageID imageClick, ImageID imageSelected)
        {
            this(xIndex, yIndex, imageDefault, imageHover, imageClick, imageSelected);
            xCoord = xIndex * dim.width;
            yCoord = yIndex * dim.height;
            this.dim = new Rectangle(xCoord, yCoord, dim.width, dim.height);
        }

        public GridTile(int xIndex, int yIndex, Polygon dim, ImageID imageDefault, ImageID imageHover, ImageID imageClick, ImageID imageSelected)
        {
            this(xIndex, yIndex, imageDefault, imageHover, imageClick, imageSelected);
            Rectangle dimBounds = dim.getBounds();
            //horizontally is the same as rectangles
            xCoord = xIndex * dimBounds.width
                     //except offset by half the width of a hex every second row
                     //if xIndex is even, then add 0, else add dimBounds.width/2
                     + ((yIndex % 2) * dimBounds.width/2);
            yCoord = yIndex * (3 * dimBounds.height / 4);
            this.dim = new Polygon(dim.xpoints, dim.ypoints, dim.npoints);
            ((Polygon)this.dim).translate(xCoord, yCoord);
        }

        private GridTile(int xIndex, int yIndex, ImageID imageDefault, ImageID imageHover, ImageID imageClick, ImageID imageSelected)
        {
            this.xIndex = xIndex;
            this.yIndex = yIndex;
            this.imageDefault = imageDefault;
            this.imageHover = imageHover;
            this.imageClick = imageClick;
            this.imageSelected = imageSelected;
            state = TileState.DEFAULT;
            entities = new LinkedList<>();
        }

        public void addEntity(Entity newEntity)
        {
            entities.add(newEntity);
        }

        public void queueAction(Action action)
        {
            for (Entity entity : entities)
            {
                entity.queueAction(action);
            }
        }

        public void replaceQueue(Action action)
        {
            for (Entity entity : entities)
            {
                entity.queueAction(action);
            }
        }

        @Override
        public UIElement mousePressed(MouseEvent paramMouseEvent)
        {
            if(dim.contains(paramMouseEvent.getX(), paramMouseEvent.getY()))
            {
                //store previous tile state in-case the click is cancelled
                //hover is not a meaningful state for us to revert to, so just set to default for that case
                if (state == TileState.HOVER)
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
                Logger.log(Logger.LogLevel.debug, new String[] {"World.GridTile mousePressed called without mouse in bounds. Should never happen. Indicates an error/edge case in" +
                                                                " click coordinates -> tile index math"});
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
                //if the start (assumed at this point since we are 'pressed') of the click and the end are both on this tile
                //i.e. we are clicked
                if(dim.contains(paramMouseEvent.getX(), paramMouseEvent.getY()))
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
            return  null;
        }

        @Override
        public UIElement mouseDragged(MouseEvent paramMouseEvent)
        {
            return null;
        }

        @Override
        public UIElement mouseMoved(MouseEvent paramMouseEvent)
        {
            if(dim.contains(paramMouseEvent.getX(), paramMouseEvent.getY()))
            {
                if(state == TileState.DEFAULT)
                {
                    state = TileState.HOVER;
                }
                return this;
            }
            else if(!dim.contains(paramMouseEvent.getX(), paramMouseEvent.getY()) && state == TileState.HOVER)
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
        public void paint(CheckedRender render)
        {
            switch (state)
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
            for (Entity entity : entities)
            {
                entity.paint(render);
            }
        }
    }
}
