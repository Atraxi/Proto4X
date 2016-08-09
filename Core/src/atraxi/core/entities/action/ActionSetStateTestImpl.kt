package atraxi.core.entities.action

import atraxi.core.Player
import atraxi.core.entities.Entity
import atraxi.core.entities.action.definitions.Action
import atraxi.core.entities.action.definitions.ActionSetState
import org.json.JSONObject

/**
 * Created by Atraxi on 17/05/2016.
 */
class ActionSetStateTestImpl(source: Entity, player: Player) : ActionSetState(source, player) {

    override fun execute() {

    }

    override val isValid: Boolean
        get() = false

    override fun fromJSON(jsonObject: JSONObject): Action? {
        return null
    }

    override fun toJSON(): JSONObject? {
        return null
    }
}
