package atraxi.core.world

import atraxi.core.Player
import atraxi.core.entities.Entity
import atraxi.core.util.Globals
import atraxi.core.util.Logger
import javafx.geometry.Point3D
import org.json.JSONArray
import org.json.JSONObject

import java.awt.Point
import java.util.ArrayList
import java.util.HashMap
import java.util.HashSet
import java.util.SortedMap
import java.util.TreeMap

class World
/**
 * Instantiates a new game world. Server side implementation only
 * @param seed
 * *
 * @param sizeX
 * *
 * @param sizeY
 */
(val seed: Long,
 /**
  * @return The number of columns wide for this world
  */
 val sizeX: Int,
 /**
  * @return The number of rows for this world
  */
 val sizeY: Int) {
    private val entities: TreeMap<Long, Entity>
    private val playerMappedEntities: HashMap<Player, ArrayList<Entity>>

    init {

        entities = TreeMap<Long, Entity>()
        playerMappedEntities = HashMap<Player, ArrayList<Entity>>()
    }

    fun getEntityAtIndex(x: Int, y: Int): Entity? {
        return entities[convertCoordinateToKey(x, y)]
    }

    fun getEntityAtIndex(gridTileIndex: Point): Entity? {
        return getEntityAtIndex(gridTileIndex.x, gridTileIndex.y)
    }

    fun getEntitiesInRegion(from: Point, to: Point): SortedMap<Long, Entity> {
        var longFrom = World.convertCoordinateToKey(from.x, from.y)
        var longTo = convertCoordinateToKey(to.x, to.y)
        if (longFrom > longTo) {
            val temp = longFrom
            longFrom = longTo
            longTo = temp
        }
        return entities.subMap(longFrom, longTo)
    }

    fun getEntitiesWithinRangeOfPoint(point: Point3D, range: Int): HashSet<Entity> {//TODO: optimise this? it iterates over all points in range rather than just the entities. Can this be done via entities.subMap()? after making a custom data structure?
        val entitiesInRange = HashSet<Entity>()
        for (x in -range..range)
        //each -N ≤ dx ≤ N:
        {
            for (y in Math.max(-range, -x - range)..Math.max(range, -x + range))
            //each max (-N, -dx - N) ≤dy ≤min(N, -dx + N):
            {
                val z = -x - y
                val point1 = convertCubicToAxial(point.x.toInt() + x, point.y.toInt() + y, point.z.toInt() + z)
                val entity = getEntityAtIndex(point1)
                if (entity != null) {
                    entitiesInRange.add(entity)
                }
            }
        }
        return entitiesInRange
    }

    fun serializeForPlayer(player: Player): JSONObject {
        val playerVisibleEntitiesJSON = JSONArray()
        val playerEntities = playerMappedEntities[player]
        playerEntities?.forEach { sourceEntity ->
            playerVisibleEntitiesJSON.put(sourceEntity.serializeForPlayer(player))

            getEntitiesWithinRangeOfPoint(convertAxialToCubic(sourceEntity.location), sourceEntity.visionRange).forEach { visibleEntity -> playerVisibleEntitiesJSON.put(visibleEntity.serializeForPlayer(player)) }
        }
        val jsonObject = JSONObject()
        jsonObject.put(Globals.JSON_KEY_World_SizeX, sizeX).put(Globals.JSON_KEY_World_SizeY, sizeY).put(Globals.JSON_KEY_World_Entities, playerVisibleEntitiesJSON)
        return jsonObject
    }

    companion object {

        private fun convertCoordinateToKey(location: Point): Long {
            return convertCoordinateToKey(location.x, location.y)
        }

        private fun convertCoordinateToKey(x: Int, y: Int): Long {
            return (x.toLong() shl 32) + y.toLong()
        }

        fun convertAxialToOffset(x: Int, y: Int): Point {
            return Point(x + (y - (y and 1)) / 2,
                    y)
        }

        /**
         * Modifies the provided point to change between the equivalent points in the Axial and Offset co-ordinate systems
         * @param point
         * *
         * @return
         */
        fun convertAxialToOffset(point: Point): Point {
            point.setLocation(point.x + (point.y - (point.y and 1)) / 2,
                    point.y)
            return point
        }

        /**
         * Modifies the provided point to change between the equivalent points in the Axial and Offset co-ordinate systems
         * @param point
         * *
         * @return
         */
        fun convertOffsetToAxial(point: Point): Point {
            point.setLocation(point.x - (point.y - (point.y and 1)) / 2,
                    point.y)
            return point
        }

        fun convertAxialToCubic(point: Point): Point3D {
            return Point3D(point.x.toDouble(),
                    (-point.x - point.y).toDouble(),
                    point.y.toDouble())
        }

        fun convertCubicToAxial(x: Int, y: Int, z: Int): Point {
            return Point(x, z)
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

        fun distanceBetween(a: Point, b: Point): Int {
            //Half the cubic distance http://www.redblobgames.com/grids/hexagons/#distances
            //        Point3D cubicA = convertAxialToCubic(a);
            //        Point3D cubicB = convertAxialToCubic(b);
            //        return (int) ((Math.abs(cubicA.getX() - cubicB.getX()) +
            //                      Math.abs(cubicA.getY() - cubicB.getY()) +
            //                      Math.abs(cubicA.getZ() - cubicB.getZ())) / 2);
            //Inlined version of above, removes pointless allocation and casting to and from double
            return (Math.abs(a.x - b.x) +
                    Math.abs(-a.x - a.y - (-b.x - b.y)) +
                    Math.abs(a.y - b.y)) / 2
        }

        @Throws(ClassNotFoundException::class, InstantiationException::class)
        fun deserialize(jsonObject: JSONObject): World {
            val world = World(0, jsonObject.getInt(Globals.JSON_KEY_World_SizeX), jsonObject.getInt(Globals.JSON_KEY_World_SizeY))

            val entitiesJSON = jsonObject.getJSONArray(Globals.JSON_KEY_World_Entities)
            for (i in 0..entitiesJSON.length() - 1) {
                val entityJSON = entitiesJSON.getJSONObject(i)
                val entityType = entityJSON.getString(Globals.JSON_KEY_Entity_Type)
                try {
                    val entity = (Class.forName(entityType).newInstance() as Entity).deserialize(entityJSON)

                    //TODO:insert into 'updates' list, merge in updates? allows re-use of any sanity checks and minimises code repetition
                    world.entities.put(convertCoordinateToKey(entity.location), entity)
                    var playerEntities: ArrayList<Entity>? = world.playerMappedEntities[entity.owner]
                    if (playerEntities == null) {
                        playerEntities = ArrayList<Entity>()
                    }
                    playerEntities.add(entity)
                    world.playerMappedEntities.put(entity.owner, playerEntities)

                } catch (e: IllegalAccessException) {
                    Logger.log(Logger.LogLevel.error, arrayOf("Game is running under an unknown security manager, unable to continue."))
                    e.printStackTrace()
                    //TODO: cleanup before exiting - reroute to standardized exit method?
                    System.exit(0)
                }
            }

            return world
        }
    }
}