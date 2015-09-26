package atraxi.game.World;

import atraxi.game.UI.UIElement;
import atraxi.game.UI.UIStack;
import atraxi.game.UI.UIStackNode;

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

    public Grid(Rectangle bounds)
    {
        tiles = new GridTile[World.WORLDWIDTH/bounds.width][World.WORLDHEIGHT/bounds.height];
        for(int x=0;x<tiles.length;x++)
        {
            for(int y=0;y<tiles[x].length;y++)
            {
                tiles[x][y] = new GridTile();
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
    public UIElement mouseEntered (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseExited (MouseEvent paramMouseEvent)
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
    public void paint (Graphics2D graphics)
    {

    }

    public class GridTile implements UIElement
    {


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
        public UIElement mouseEntered (MouseEvent paramMouseEvent)
        {
            return null;
        }

        @Override
        public UIElement mouseExited (MouseEvent paramMouseEvent)
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
        public void paint (Graphics2D graphics)
        {

        }
    }
}
