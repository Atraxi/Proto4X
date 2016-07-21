package atraxi.core.world;

import atraxi.core.Player;
import atraxi.core.entities.Entity;
import atraxi.core.util.Globals;
import javafx.geometry.Point3D;
import org.json.JSONArray;
import org.json.JSONObject;

import java.awt.Point;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.SortedMap;
import java.util.TreeMap;

public class World
{
    public final long seed;
    private final TreeMap<Long, Entity> entities;
    private final HashMap<Player, ArrayList<Entity>> playerMappedEntities;
    private int sizeX, sizeY;

    /**
     * Instantiates a new game world. Server side implementation only
     * @param seed
     * @param sizeX
     * @param sizeY
     */
    public World(long seed, int sizeX, int sizeY)
    {
        this.seed = seed;
        this.sizeX = sizeX;
        this.sizeY = sizeY;

        entities = new TreeMap<>();
        playerMappedEntities = new HashMap<>();
    }

    /**
     * @return The number of rows for this world
     */
    public int getSizeY()
    {
        return sizeY;
    }

    /**
     * @return The number of columns wide for this world
     */
    public int getSizeX()
    {
        return sizeX;
    }

    public Entity getEntityAtIndex(int x, int y)
    {
        return entities.get(convertCoordinateToKey(x, y));
    }

    public Entity getEntityAtIndex(Point gridTileIndex)
    {
        return getEntityAtIndex(gridTileIndex.x, gridTileIndex.y);
    }

    private static long convertCoordinateToKey(Point location)
    {
        return convertCoordinateToKey(location.x, location.y);
    }

    private static long convertCoordinateToKey(int x, int y)
    {
        return ((long)x << 32L) + (long)y;
    }

    public static Point convertAxialToOffset(int x, int y)
    {
        return new Point(x + (y - (y&1)) / 2,
                         y);
    }

    /**
     * Modifies the provided point to change between the equivalent points in the Axial and Offset co-ordinate systems
     * @param point
     * @return
     */
    public static Point convertAxialToOffset(Point point)
    {
        point.setLocation(point.x + (point.y - (point.y&1)) / 2,
                          point.y);
        return point;
    }

    /**
     * Modifies the provided point to change between the equivalent points in the Axial and Offset co-ordinate systems
     * @param point
     * @return
     */
    public static Point convertOffsetToAxial(Point point)
    {
        point.setLocation(point.x - (point.y - (point.y&1)) / 2,
                          point.y);
        return point;
    }

    public static Point3D convertAxialToCubic(Point point)
    {
        return new Point3D(point.x,
                           -point.x - point.y,
                           point.y);
    }

    public static Point convertCubicToAxial(int x, int y, int z)
    {
        return new Point(x, z);
    }

    //TODO: add entity should insert into a 'updates' list, to allow changes to be collected and sent to each client, while simultaneously reducing possible race conditions
    //TODO: merge 'updates' list into main world
//    public void addEntity(Entity entity)
//    {
//        entities.put(convertCoordinateToKey(entity.getLocation()), entity);
//
//        ArrayList<Entity> playerEntities = playerMappedEntities.get(entity.getOwner());
//        if(playerEntities == null)
//        {
//            playerEntities = new ArrayList<>();
//        }
//        playerEntities.add(entity);
//        playerMappedEntities.put(entity.getOwner(), playerEntities);
//    }

    public static int distanceBetween(Point a, Point b)
    {
        //Half the cubic distance http://www.redblobgames.com/grids/hexagons/#distances
//        Point3D cubicA = convertAxialToCubic(a);
//        Point3D cubicB = convertAxialToCubic(b);
//        return (int) ((Math.abs(cubicA.getX() - cubicB.getX()) +
//                      Math.abs(cubicA.getY() - cubicB.getY()) +
//                      Math.abs(cubicA.getZ() - cubicB.getZ())) / 2);
        //Inlined version of above, removes pointless allocation and casting to and from double
        return (Math.abs(a.x - b.x) +
                Math.abs((-a.x - a.y) - (-b.x - b.y)) +
                Math.abs(a.y - b.y)) / 2;
    }

    public SortedMap<Long, Entity> getEntitiesInRegion(Point from, Point to)
    {
        long longFrom = World.convertCoordinateToKey(from.x, from.y);
        long longTo = convertCoordinateToKey(to.x, to.y);
        if(longFrom > longTo)
        {
            long temp = longFrom;
            longFrom = longTo;
            longTo = temp;
        }
        return entities.subMap(longFrom, longTo);
    }

    public HashSet<Entity> getEntitiesWithinRangeOfPoint(Point3D point, int range)
    {//TODO: optimise this? it iterates over all points in range rather than just the entities. Can this be done via entities.subMap()? after making a custom data structure?
        HashSet<Entity> entitiesInRange = new HashSet<>();
        for(int x = -range; x <= range; x++) //each -N ≤ dx ≤ N:
        {
            for(int y = Math.max(-range, -x - range); y <= Math.max(range, -x + range); y++) //each max (-N, -dx - N) ≤dy ≤min(N, -dx + N):
            {
                int z = -x - y;
                Point point1 = convertCubicToAxial((int)point.getX() + x, (int)point.getY() + y, (int)point.getZ() + z);
                Entity entity = getEntityAtIndex(point1);
                if(entity != null)
                {
                    entitiesInRange.add(entity);
                }
            }
        }
        return entitiesInRange;
    }

    public JSONObject serializeForPlayer(Player player)
    {
        final JSONArray playerVisibleEntitiesJSON = new JSONArray();
        ArrayList<Entity> playerEntities = playerMappedEntities.get(player);
        if(playerEntities != null)//
        {
            playerEntities.forEach(sourceEntity ->
                                   {
                                       playerVisibleEntitiesJSON.put(sourceEntity.serializeForPlayer(player));

                                       getEntitiesWithinRangeOfPoint(convertAxialToCubic(sourceEntity.getLocation()), sourceEntity.getVisionRange()).forEach(
                                               visibleEntity -> playerVisibleEntitiesJSON.put(visibleEntity.serializeForPlayer(player)));
                                   });
        }
        JSONObject jsonObject = new JSONObject();
        jsonObject.put(Globals.JSON_KEY_World_SizeX, sizeX)
                  .put(Globals.JSON_KEY_World_SizeY, sizeY)
                  .put(Globals.JSON_KEY_World_Entities, playerVisibleEntitiesJSON);
        return jsonObject;
    }

    public static World deserialize(JSONObject jsonObject) throws ClassNotFoundException, IllegalAccessException, InstantiationException
    {
        World world = new World(0, jsonObject.getInt(Globals.JSON_KEY_World_SizeX), jsonObject.getInt(Globals.JSON_KEY_World_SizeY));

        JSONArray entitiesJSON = jsonObject.getJSONArray(Globals.JSON_KEY_World_Entities);
        for(int i = 0; i < entitiesJSON.length(); i++)
        {
            JSONObject entityJSON = entitiesJSON.getJSONObject(i);
            String entityType = entityJSON.getString(Globals.JSON_KEY_Entity_Type);
            Entity entity = ((Entity)Class.forName(entityType).newInstance()).deserialize(entityJSON);

            //TODO:insert into 'updates' list, merge in updates? allows re-use of any sanity checks and minimises code repetition
            world.entities.put(convertCoordinateToKey(entity.getLocation()), entity);
            ArrayList<Entity> playerEntities = world.playerMappedEntities.get(entity.getOwner());
            if(playerEntities == null)
            {
                playerEntities = new ArrayList<>();
            }
            playerEntities.add(entity);
            world.playerMappedEntities.put(entity.getOwner(), playerEntities);
        }

        return world;
    }
}