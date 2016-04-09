package atraxi.game.world;

import atraxi.entities.Entity;
import atraxi.entities.actionQueue.Action;
import atraxi.ui.UIElement;
import atraxi.ui.UIStackNode;
import atraxi.ui.UserInterfaceHandler;
import atraxi.util.CheckedRender;
import atraxi.util.Logger;
import atraxi.util.ResourceManager.ImageID;

import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;
import java.util.LinkedList;

public class World implements UIElement, UIStackNode
{
    public final long seed;
    private UIStackNode nextNode = null, previousNode = null;
    private GridTile[][] tiles;
    private Rectangle tileBounds;
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

    @Override
    public UIStackNode getNextNode()
    {
        return nextNode;
    }

    @Override
    public UIStackNode getPreviousNode()
    {
        return previousNode;
    }

    @Override
    public void setPreviousNode(UIStackNode node)
    {
        previousNode = node;
    }

    @Override
    public void setNextNode(UIStackNode node)
    {
        nextNode = node;
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
        int x = (int) Math.floor((paramMouseEvent.getX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseEvent.getY()) / tileBounds.getHeight());
        if (x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
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
        int x = (int) Math.floor((paramMouseEvent.getX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseEvent.getY()) / tileBounds.getHeight());
        if (x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            if(tilePressed != null)
            {
                tilePressed = tilePressed.mouseReleased(paramMouseEvent);
                if (tilePressed != null)
                {
                    if(tileSelected != null && tilePressed != tileSelected)
                    {
                        tileSelected.mouseReleased(paramMouseEvent);
                    }
                    tileSelected = tilePressed;
                }
                tilePressed = null;

                return tileSelected;
            }
        }
        return null;
    }

    @Override
    public UIElement mouseDragged(MouseEvent paramMouseEvent)
    {
        int x = (int) Math.floor((paramMouseEvent.getX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseEvent.getY()) / tileBounds.getHeight());
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
        int x = (int) Math.floor((paramMouseEvent.getX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseEvent.getY()) / tileBounds.getHeight());
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
        int x = (int) Math.floor((paramMouseWheelEvent.getX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseWheelEvent.getY()) / tileBounds.getHeight());
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
        private int x, y;
        private Rectangle dim;
        private LinkedList<Entity> entities;

        public GridTile(int x, int y, Rectangle dim, ImageID imageDefault, ImageID imageHover, ImageID imageClick, ImageID imageSelected)
        {
            this.x = x;
            this.y = y;
            this.dim = new Rectangle(x * dim.width, y * dim.height, dim.width, dim.height);
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
                if(state != TileState.HOVER)
                {
                    previousState = state;
                }
                else
                {
                    previousState = TileState.DEFAULT;
                }
                state = TileState.PRESSED;
                return this;
            }
            else
            {
                Logger.log(Logger.LogLevel.debug, new String[] {"World.GridTile mousePressed called without mouse in bounds. Should never happen."});
                state = TileState.DEFAULT;
                return null;
            }
        }

        @Override
        public UIElement mouseReleased(MouseEvent paramMouseEvent)
        {
            if(state == TileState.PRESSED)
            {
                if(dim.contains(paramMouseEvent.getX(), paramMouseEvent.getY()))
                {
                    state = TileState.SELECTED;
                    UserInterfaceHandler.setSelectedTile(this);
                    return this;
                }
                else
                {
                    state = previousState;
                }
            }
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
                    render.drawImage(imageDefault, x * dim.width, y * dim.height, null);
                    break;
                case HOVER:
                    render.drawImage(imageHover, x * dim.width, y * dim.height, null);
                    break;
                case PRESSED:
                    render.drawImage(imageClick, x * dim.width, y * dim.height, null);
                    break;
                case SELECTED:
                    render.drawImage(imageSelected, x * dim.width, y * dim.height, null);
            }
            for (Entity entity : entities)
            {
                entity.paint(render);
            }
        }
    }
}
