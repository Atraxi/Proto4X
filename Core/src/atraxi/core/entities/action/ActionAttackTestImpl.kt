package atraxi.core.entities.action

import atraxi.core.Player
import atraxi.core.entities.Entity
import atraxi.core.entities.action.definitions.Action
import atraxi.core.entities.action.definitions.ActionAttack
import org.json.JSONObject

/**
 * Created by Atraxi on 17/05/2016.
 */
class ActionAttackTestImpl(source: Entity, player: Player, target: Entity) : ActionAttack(source, player, target) {

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
