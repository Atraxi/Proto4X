package atraxi.game;

import java.awt.*;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;
import java.util.ArrayList;
import java.util.stream.Collectors;

import atraxi.game.UI.UIElement;
import atraxi.game.UI.UIStack;
import atraxi.game.UI.UIStackNode;
import atraxi.game.UI.UserInterfaceHandler;
import entities.Entity;
import entities.Ship;
import entities.Structure;

public class World
{
    private final static ArrayList<Entity> entities = new ArrayList<Entity>();
    public static final int WORLDWIDTH = 1920;
    public static final int WORLDHEIGHT = 1080;
    private static final int GRIDWIDTH = 20;
    private static final int GRIDHEIGHT = 20;
    
    public World()
    {
        entities.add(new Ship("baseShipClass", Game.getPlayerList().get(0), 100, 200));
        entities.add(new Ship("baseShipClass", Game.getPlayerList().get(0), 100, 220));
        entities.add(new Ship("baseShipClass", Game.getPlayerList().get(0), 100, 240));
        entities.add(new Ship("baseShipClass", Game.getPlayerList().get(0), 100, 260));
        entities.add(new Ship("baseShipClass", Game.getPlayerList().get(0), 100, 280));
        entities.add(new Ship("baseShipClass", Game.getPlayerList().get(0), 100, 300));
        entities.add(new Ship("baseShipClass", Game.getPlayerList().get(0), 100, 320));
        
        entities.add(new Structure("baseBuildingClass", Game.getPlayerList().get(0), 500, 500));

        UserInterfaceHandler.uiStack.push(new Grid(new Rectangle(0,0,GRIDWIDTH,GRIDHEIGHT)));
    }
    
    /**
     * Finds all entities within the given area by performing a bounds check on every entity
     * @param selectionArea
     * @return An array of all entities found
     */
    public static Entity[] getEntityArrayWithin(Rectangle selectionArea)
    {
        ArrayList<Entity> selection = new ArrayList<Entity>();
        synchronized(entities)
        {
            selection.addAll(entities.stream().filter(e -> e.boundsTest(selectionArea)).collect(Collectors.toList()));
        }
        return selection.toArray(new Entity[selection.size()]);
    }
    
    /**
     * @return A new ArrayList populated from the list of all entities between the given x-coordinate range. Being a new copy, this will not reflect any changes made to the main entity list.
     */
    public static ArrayList<Entity> getEntityList()
    {
        synchronized(entities)
        {
            //Protection against concurrent modification, all modifications must be performed via synchronised methods within this class
            return new ArrayList<Entity>(entities);
        }
    }
    
    /**
     * A thread safe means to add a new entity
     * @param entity The entity to be added
     */
    public static void addEntity(Entity entity)
    {
        synchronized(entities)
        {
            entities.add(entity);
        }
    }

    public class Grid extends UIStack implements UIStackNode
    {
        private UIStackNode nextNode = null, previousNode = null;
        private GridTile[][] tiles;

        public Grid(Rectangle bounds)
        {
            tiles = new GridTile[WORLDWIDTH/bounds.width][WORLDHEIGHT/bounds.height];
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

        private class GridTile implements UIElement, UIStackNode
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
}
