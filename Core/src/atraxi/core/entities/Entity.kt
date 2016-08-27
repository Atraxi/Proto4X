package atraxi.core.entities

import atraxi.core.Player
import atraxi.core.entities.action.ActionQueue
import atraxi.core.entities.action.definitions.Action
import atraxi.core.util.Globals
import atraxi.core.world.World
import org.json.JSONObject

import java.awt.Point
import java.math.BigDecimal

abstract class Entity(val type: Globals.Identifiers, owner: Player, protected var location: Point, private val moveSpeed: Int, visionRange: Int) {
    private val orientation: Int = 0
    var owner: Player
        internal set
    protected val actionQueue: ActionQueue
    val visionRange: Int = 0
    protected var id: Long = 0

    init {
        this.owner = owner
        this.actionQueue = ActionQueue()
        this.id = Globals.newID
    }

    abstract fun canAcceptAction(action: Action): Boolean
    protected abstract fun startActionFromQueue(action: Action)

    /**
     * Calculate how long it will take to reach a given location
     * @param destination The location to path towards
     * *
     * @return The distance to the target location, or -1 if the target cannot be reached
     */
    fun turnCountToReachLocation(destination: Point): Double {//TODO: extend this to also try to pathfind through teleporters/ftl

        val distance = World.distanceBetween(this.location, destination)
        if (distance <= Globals.MAX_MOVEMENT_DISTANCE) {
            return (distance / moveSpeed).toDouble()
        }
        return -1.0
    }

    open fun doWork(timeAdjustment: BigDecimal, paused: Boolean) {
        if (!actionQueue.isEmpty) {
            val action = actionQueue.pullAction()
            if (action != null) {
                startActionFromQueue(action)
            }
        }
    }

    override fun toString(): String {
        return "[" + type + " Pos:" + location.x + "," + location.y + " Orientation:" + orientation + "]"
    }

    fun queueAction(action: Action) {
        actionQueue.queueAction(action)
    }

    fun replaceQueue(action: Action) {
        actionQueue.replaceQueue(action)
    }

    val orientationInRadians: Double
        get() = orientation * Math.toRadians(60.0)

    /**
     * Serialize this entity to be sent to the specified player. For cheat prevention this should only include information that the player is currently supposed to be able to view.

     * It is safe to assume this entity is already visible by the given player by some means, therefore visibility checks are not required.
     * @param player
     * *
     * @return
     */
    abstract fun serializeForPlayer(player: Player): JSONObject

    abstract fun deserialize(entityJSON: JSONObject): Entity
}
