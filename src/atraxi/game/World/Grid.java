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
public class Grid extends UIStack implements UIStackNode
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

    public class GridTile implements UIElement, UIStackNode
    {
        private UIStackNode nextNode = null, previousNode = null;

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
        public boolean mousePressed (MouseEvent paramMouseEvent)
        {
            return false;
        }

        @Override
        public boolean mouseReleased (MouseEvent paramMouseEvent)
        {
            return false;
        }

        @Override
        public boolean mouseEntered (MouseEvent paramMouseEvent)
        {
            return false;
        }

        @Override
        public boolean mouseExited (MouseEvent paramMouseEvent)
        {
            return false;
        }

        @Override
        public boolean mouseDragged (MouseEvent paramMouseEvent)
        {
            return false;
        }

        @Override
        public boolean mouseMoved (MouseEvent paramMouseEvent)
        {
            return false;
        }

        @Override
        public boolean mouseWheelMoved (MouseWheelEvent paramMouseWheelEvent)
        {
            return false;
        }

        @Override
        public void paint (Graphics2D graphics)
        {

        }
    }
}
