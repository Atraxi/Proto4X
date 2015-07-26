package atraxi.game;

import java.awt.Rectangle;
import java.util.ArrayList;

import entities.Entity;
import entities.Ship;

public class World
{
    private static ArrayList<Entity> entities = new ArrayList<Entity>();
    
    public World()
    {
        entities.add(new Ship("baseShipClass", 100, 200));
        entities.add(new Ship("baseShipClass", 100, 220));
        entities.add(new Ship("baseShipClass", 100, 240));
        entities.add(new Ship("baseShipClass", 100, 260));
        entities.add(new Ship("baseShipClass", 100, 280));
        entities.add(new Ship("baseShipClass", 100, 300));
        entities.add(new Ship("baseShipClass", 100, 320));
    }
    
    public static Entity[] getEntityArrayWithin(Rectangle selectionArea)
    {
        ArrayList<Entity> selection = new ArrayList<Entity>();
        for(Entity e : entities)
        {
            if(e.boundsTest(selectionArea))
            {
                selection.add(e);
            }
        }
        return selection.toArray(new Entity[selection.size()]);
    }
    
    public ArrayList<Entity> getEntityList()
    {
        return entities;
    }
}
