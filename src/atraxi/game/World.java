package atraxi.game;

import java.awt.Rectangle;
import java.util.ArrayList;
import java.util.stream.Collectors;

import entities.Entity;
import entities.Ship;
import entities.Structure;

public class World
{
    private final static ArrayList<Entity> entities = new ArrayList<Entity>();
    public static final int WORLDWIDTH = 1920;
    public static final int WORLDHIEGHT = 1080;
    
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
}
