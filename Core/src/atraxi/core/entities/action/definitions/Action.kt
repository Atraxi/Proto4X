package atraxi.core.entities.action.definitions

import atraxi.core.Player
import atraxi.core.entities.Entity
import org.json.JSONObject

/**
 * Created by Atraxi on 2/05/2016.
 */
abstract class Action protected constructor(protected var source: Entity?, protected var player: Player?) {
    var nextAction: Action? = null

    abstract fun execute()

    /**
     * Verifies [Action.execute] can run without issue, however without modifying any game state
     * @return true if this action is fully valid for the specific data it contains
     */
    val isValid: Boolean
        get() = source != null &&
                player != null &&
                source!!.owner === player

    abstract fun fromJSON(jsonObject: JSONObject): Action

    abstract fun toJSON(): JSONObject
}
