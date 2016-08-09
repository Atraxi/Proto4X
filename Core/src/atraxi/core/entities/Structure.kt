package atraxi.core.entities

import atraxi.core.Player
import atraxi.core.entities.action.definitions.Action
import atraxi.core.util.Globals
import org.json.JSONObject

import java.awt.Point
import java.math.BigDecimal

class Structure(type: Globals.Identifiers, owner: Player, location: Point, moveSpeed: Int, visionRange: Int) : Entity(type, owner, location, moveSpeed, visionRange) {

    override fun canAcceptAction(action: Action): Boolean {
        //        switch (action)
        //        {
        //            case BUILD:
        //            case SUICIDE:
        //                return true;
        //            default:
        //                return false;
        //        }
        return true
    }

    override fun doWork(timeDiff: BigDecimal, paused: Boolean) {
        super.doWork(timeDiff, paused)
    }

    override fun serializeForPlayer(player: Player): JSONObject {
        val jsonObject = JSONObject()
        jsonObject.put(Globals.JSON_KEY_Entity_Player, player.name).put(Globals.JSON_KEY_Entity_PositionX, getLocation().x).put(Globals.JSON_KEY_Entity_PositionY, getLocation().y)
        return jsonObject
    }

    override fun deserialize(entityJSON: JSONObject): Entity? {
        return null
    }

    override fun startActionFromQueue(action: Action) {

    }
}
