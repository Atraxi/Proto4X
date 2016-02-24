package atraxi.game.world;

import atraxi.entities.Entity;
import atraxi.entities.actionQueue.Action;
import atraxi.ui.UIElement;
import atraxi.ui.UIStackNode;
import atraxi.ui.UserInterfaceHandler;
import atraxi.util.CheckedRender;
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

    //Tiles cannot be relied upon to track this internally, as it is not feasible to have every tile update itself for every UI event (which includes mouse movement)
    //We can mathematically determine newly focused tiles, and thus it can reliably set it's own state, but not implicitly know when to revert to default
    //So we track tiles with special state and prompt them to check the conditions for reverting to default when relevant
    private UIElement tileSelected, tileHover;

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
        int x = (int) Math.floor((paramMouseEvent.getX() - UserInterfaceHandler.getScreenLocationX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseEvent.getY() - UserInterfaceHandler.getScreenLocationY()) / tileBounds.getHeight());
        if (x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            if(tileSelected != null )
            {
                tileSelected.mousePressed(paramMouseEvent);
            }
            tileSelected = tiles[x][y].mousePressed(paramMouseEvent);

            return tileSelected;
        }
        else
        {
            return null;
        }
    }
    @Override
    public UIElement mouseReleased(MouseEvent paramMouseEvent)
    {
        int x = (int) Math.floor((paramMouseEvent.getX() - UserInterfaceHandler.getScreenLocationX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseEvent.getY() - UserInterfaceHandler.getScreenLocationY()) / tileBounds.getHeight());
        if (x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            if(tileSelected != null )
            {
                tileSelected.mouseReleased(paramMouseEvent);
            }
            tileSelected = tiles[x][y].mouseReleased(paramMouseEvent);

            return tileSelected;
        }
        else
        {
            return null;
        }
    }

    @Override
    public UIElement mouseDragged(MouseEvent paramMouseEvent)
    {
        int x = (int) Math.floor((paramMouseEvent.getX() - UserInterfaceHandler.getScreenLocationX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseEvent.getY() - UserInterfaceHandler.getScreenLocationY()) / tileBounds.getHeight());
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
        int x = (int) Math.floor((paramMouseEvent.getX() - UserInterfaceHandler.getScreenLocationX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseEvent.getY() - UserInterfaceHandler.getScreenLocationY()) / tileBounds.getHeight());
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
        int x = (int) Math.floor((paramMouseWheelEvent.getX() - UserInterfaceHandler.getScreenLocationX()) / tileBounds.getWidth());
        int y = (int) Math.floor((paramMouseWheelEvent.getY() - UserInterfaceHandler.getScreenLocationY()) / tileBounds.getHeight());
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
        private TileState state;
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
            if(dim.contains(paramMouseEvent.getX() - UserInterfaceHandler.getScreenLocationX(), paramMouseEvent.getY() - UserInterfaceHandler.getScreenLocationY()))
            {
                state = TileState.PRESSED;

                return this;
            }
            return null;
        }

        @Override
        public UIElement mouseReleased(MouseEvent paramMouseEvent)
        {
            if(state == TileState.PRESSED)
            {
                if(dim.contains(paramMouseEvent.getX() - UserInterfaceHandler.getScreenLocationX(), paramMouseEvent.getY() - UserInterfaceHandler.getScreenLocationY()))
                {
                    state = TileState.SELECTED;
                    UserInterfaceHandler.setSelectedTile(this);
                    return this;
                }
                else
                {
                    state = TileState.HOVER;
                }
            }
            else if(state == TileState.SELECTED)
            {
                if(!dim.contains(paramMouseEvent.getX() - UserInterfaceHandler.getScreenLocationX(), paramMouseEvent.getY() - UserInterfaceHandler.getScreenLocationY()))
                {
                    state = TileState.DEFAULT;
                }
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
            if(dim.contains(paramMouseEvent.getX() - UserInterfaceHandler.getScreenLocationX(), paramMouseEvent.getY() - UserInterfaceHandler.getScreenLocationY()))
            {
                if(state == TileState.DEFAULT)
                {
                    state = TileState.HOVER;
                }
                return this;
            }
            else if(!dim.contains(paramMouseEvent.getX() - UserInterfaceHandler.getScreenLocationX(), paramMouseEvent.getY() - UserInterfaceHandler.getScreenLocationY()) && state == TileState.HOVER)
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
