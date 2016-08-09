package atraxi.core.entities.action

import atraxi.core.Player
import atraxi.core.entities.Entity
import atraxi.core.entities.action.definitions.Action
import atraxi.core.entities.action.definitions.ActionMove
import org.json.JSONObject

import java.awt.Point

/**
 * Created by Atraxi on 17/05/2016.
 */
class ActionMoveTestImpl(source: Entity, player: Player, target: Point) : ActionMove(source, player, target) {

    override fun execute() {

    }

    override val isValid: Boolean
        get() = super.isValid && source!!.turnCountToReachLocation(target) >= 0

    override fun fromJSON(jsonObject: JSONObject): Action? {
        return null
    }

    override fun toJSON(): JSONObject? {
        return null
    }
}
