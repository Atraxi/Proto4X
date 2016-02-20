package atraxi.game.world;

import atraxi.ui.UIElement;
import atraxi.ui.UIStack;
import atraxi.ui.UIStackNode;
import atraxi.util.*;
import atraxi.util.ResourceManager.*;

import java.awt.Graphics2D;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

/**
 * Created by Atraxi on 13/09/2015.
 */
public class Grid implements UIElement, UIStackNode
{
    private UIStackNode nextNode = null, previousNode = null;
    private GridTile[][] tiles;

    public Grid(Rectangle bounds, World world, ImageID tileDefault, ImageID tileHover, ImageID tileClick)
    {
        tiles = new GridTile[world.getSizeX()/bounds.width][world.getSizeY()/bounds.height];
        for(int x=0;x<tiles.length;x++)
        {
            for(int y=0;y<tiles[x].length;y++)
            {
                tiles[x][y] = new GridTile(tileDefault, tileHover, tileClick);
            }
        }
    }

    @Override
    public UIStackNode getNextNode ()
    {
        return nextNode;
    }

    @Override
    public UIStackNode getPreviousNode ()
    {
        return previousNode;
    }

    @Override
    public void setNextNode (UIStackNode node)
    {
        nextNode = node;
    }

    @Override
    public void setPreviousNode (UIStackNode node)
    {
        previousNode = node;
    }

    @Override
    public UIElement mousePressed (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseReleased (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseDragged (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseMoved (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseWheelMoved (MouseWheelEvent paramMouseWheelEvent)
    {
        return null;
    }

    @Override
    public void paint (CheckedRender render)
    {
        for(GridTile[] tileRow : tiles)
        {
            for (GridTile tile : tileRow)
            {
                tile.paint(render);
            }
        }
    }

    public class GridTile implements UIElement
    {
        ImageID imageDefault, imageHover, imageClick;

        public GridTile(ImageID imageDefault, ImageID imageHover, ImageID imageClick)
        {
            this.imageDefault = imageDefault;
            this.imageHover = imageHover;
            this.imageClick = imageClick;
        }

        @Override
        public UIElement mousePressed (MouseEvent paramMouseEvent)
        {
            return null;
        }

        @Override
        public UIElement mouseReleased (MouseEvent paramMouseEvent)
        {
            return null;
        }

        @Override
        public UIElement mouseDragged (MouseEvent paramMouseEvent)
        {
            return null;
        }

        @Override
        public UIElement mouseMoved (MouseEvent paramMouseEvent)
        {
            return null;
        }

        @Override
        public UIElement mouseWheelMoved (MouseWheelEvent paramMouseWheelEvent)
        {
            return null;
        }

        @Override
        public void paint (CheckedRender render)
        {

        }
    }
}
