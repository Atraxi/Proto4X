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

    @Override
    public UIElement mousePressed(MouseEvent paramMouseEvent)
    {
        return tiles[(int) ((paramMouseEvent.getX() + UserInterfaceHandler.getScreenLocationX()) / tileBounds.getWidth())]
                [(int) ((paramMouseEvent.getY() + UserInterfaceHandler.getScreenLocationY()) / tileBounds.getHeight())]
                .mousePressed(paramMouseEvent);
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
    public UIElement mouseReleased(MouseEvent paramMouseEvent)
    {
        return tiles[(int) ((paramMouseEvent.getX() + UserInterfaceHandler.getScreenLocationX()) / tileBounds.getWidth())]
                [(int) ((paramMouseEvent.getY() + UserInterfaceHandler.getScreenLocationY()) / tileBounds.getHeight())]
                .mousePressed(paramMouseEvent);
    }

    private enum TileState
    {
        DEFAULT, HOVER, PRESSED, SELECTED
    }

    public class GridTile implements UIElement
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
            this.dim = dim;
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

        @Override
        public UIElement mousePressed(MouseEvent paramMouseEvent)
        {
            return null;
        }

        public void replaceQueue(Action action)
        {
            for (Entity entity : entities)
            {
                entity.queueAction(action);
            }
        }

        @Override
        public UIElement mouseReleased(MouseEvent paramMouseEvent)
        {
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
            if(dim.contains(paramMouseEvent.getPoint()) && (state == TileState.DEFAULT))
            {
                state = TileState.HOVER;
                return this;
            }
            else if(!dim.contains(paramMouseEvent.getPoint()) && state == TileState.HOVER)
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


    @Override
    public UIElement mouseDragged(MouseEvent paramMouseEvent)
    {
        return tiles[(int) ((paramMouseEvent.getX() + UserInterfaceHandler.getScreenLocationX()) / tileBounds.getWidth())]
                [(int) ((paramMouseEvent.getY() + UserInterfaceHandler.getScreenLocationY()) / tileBounds.getHeight())]
                .mousePressed(paramMouseEvent);
    }

    @Override
    public UIElement mouseMoved(MouseEvent paramMouseEvent)
    {
        int x = (int) ((paramMouseEvent.getX() + UserInterfaceHandler.getScreenLocationX()) / tileBounds.getWidth());
        int y = (int) ((paramMouseEvent.getY() + UserInterfaceHandler.getScreenLocationY()) / tileBounds.getHeight());
        if (x >= 0 && y >= 0 && x < tiles.length && y < tiles[0].length)
        {
            return tiles[(int) ((paramMouseEvent.getX() + UserInterfaceHandler.getScreenLocationX()) / tileBounds.getWidth())]
                    [(int) ((paramMouseEvent.getY() + UserInterfaceHandler.getScreenLocationY()) / tileBounds.getHeight())]
                    .mousePressed(paramMouseEvent);
        }
        else
        {
            return null;
        }
    }

    @Override
    public UIElement mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent)
    {
        return tiles[(int) ((paramMouseWheelEvent.getX() + UserInterfaceHandler.getScreenLocationX()) /
                            tileBounds.getWidth())]
                [(int) ((paramMouseWheelEvent.getY() + UserInterfaceHandler.getScreenLocationY()) / tileBounds.getHeight())]
                .mousePressed(paramMouseWheelEvent);
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
}
