package atraxi.core.entities

import atraxi.core.Player
import atraxi.core.entities.action.definitions.Action
import atraxi.core.util.Globals
import org.json.JSONObject

import java.awt.Point
import java.math.BigDecimal

class Ship(type: Globals.Identifiers, owner: Player, location: Point, moveSpeed: Int, viewRange: Int) : Entity(type, owner, location, moveSpeed, viewRange) {

    override fun doWork(timeAdjustment: BigDecimal, paused: Boolean) {
        super.doWork(timeAdjustment, paused)
        //        if ( !paused)
        //        {
        //            synchronized(actionInProgressLock)
        //            {
        //                if(actionInProgress!=null && actionInProgress.isExecuting())
        //                {
        //                    boolean finishedRotation, finishedMoving;
        //
        //                    finishedRotation = updateOrientation(timeAdjustment);
        //                    finishedMoving = updatePosition(timeAdjustment);
        //
        //                    if(finishedRotation && finishedMoving)
        //                    {
        //                        // TODO: allow drag for target orientation
        //                        actionInProgress = null;
        //                    }
        //                }
        //            }
        //        }
    }

    override fun serializeForPlayer(player: Player): JSONObject {
        val jsonObject = JSONObject()
        jsonObject.put(Globals.JSON_KEY_Entity_Player, this.owner.name).put(Globals.JSON_KEY_Entity_PositionX, getLocation().x).put(Globals.JSON_KEY_Entity_PositionY, getLocation().y).put(Globals.JSON_KEY_Entity_ID, id)
        return jsonObject
    }

    override fun deserialize(entityJSON: JSONObject): Entity {
        //TODO: implement deserialization
        owner = Globals.playersByName[entityJSON.getString(Globals.JSON_KEY_Entity_Player)]
        location.setLocation(entityJSON.getInt(Globals.JSON_KEY_Entity_PositionX),
                entityJSON.getInt(Globals.JSON_KEY_Entity_PositionY))
        id = entityJSON.getLong(Globals.JSON_KEY_Entity_ID)
        return this
    }

    override fun canAcceptAction(action: Action): Boolean {
        return true
    }

    override fun startActionFromQueue(nextActionFromQueue: Action) {

    }
}
