package atraxi.game.world;

import java.awt.Rectangle;
import java.util.ArrayList;
import java.util.stream.Collectors;

import atraxi.entities.Entity;
import atraxi.entities.Ship;
import atraxi.entities.Structure;
import atraxi.game.Game;

public class World
{
    private final ArrayList<Entity> entities = new ArrayList<Entity>();
    private int sizeX, sizeY;
    public final long seed;
    
    public World(long seed, int xDim, int yDim)
    {
        this.seed = seed;
        sizeX = xDim;
        sizeY = yDim;
        entities.add(new Ship("entityShipDefault", Game.getPlayerList().get(0), 100, 200, this));
        entities.add(new Ship("entityShipDefault", Game.getPlayerList().get(0), 100, 220, this));
        entities.add(new Ship("entityShipDefault", Game.getPlayerList().get(0), 100, 240, this));
        entities.add(new Ship("entityShipDefault", Game.getPlayerList().get(0), 100, 260, this));
        entities.add(new Ship("entityShipDefault", Game.getPlayerList().get(0), 100, 280, this));
        entities.add(new Ship("entityShipDefault", Game.getPlayerList().get(0), 100, 300, this));
        entities.add(new Ship("entityShipDefault", Game.getPlayerList().get(0), 100, 320, this));
        
        entities.add(new Structure("entityStructureDefault", Game.getPlayerList().get(0), 500, 500, this));
    }
    
    /**
     * Finds all entities within the given area by performing a bounds check on every entity
     * @param selectionArea
     * @return An array of all entities found
     */
    public Entity[] getEntityArrayWithin(Rectangle selectionArea)
    {
        ArrayList<Entity> selection = new ArrayList<Entity>();
        synchronized(entities)
        {
            selection.addAll(entities.stream().filter(e -> e.boundsTest(selectionArea)).collect(Collectors.toList()));
        }
        return selection.toArray(new Entity[selection.size()]);
    }
    
    /**
     * @return A new ArrayList populated from the list of all entities. Being a new copy, this will not reflect any changes made to the main entity list.
     */
    public ArrayList<Entity> getEntityList()
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
    public void addEntity(Entity entity)
    {
        synchronized(entities)
        {
            entities.add(entity);
        }
    }

    public int getSizeY ()
    {
        return sizeY;
    }

    public int getSizeX ()
    {
        return sizeX;
    }
}
