package atraxi.core.util

import atraxi.core.Player
import atraxi.core.world.World

import java.util.ArrayList
import java.util.HashMap
import java.util.Random

object Globals {
    val MAX_MOVEMENT_DISTANCE = 200
    var debug: DebugState? = null
    val SEED = System.nanoTime()
    var random: Random? = null

    val players = ArrayList<Player>()
    val playersByName: Map<String, Player> = HashMap()
    val worlds = ArrayList<World>()

    private var previousID: Long = 0

    //Networking layer JSON tag names
    val JSON_KEY_INITIALIZATION_PlayerIndex = "playerIndex"
    val JSON_KEY_INITIALIZATION_PlayerName = "playerName"

    val JSON_KEY_MessageType = "messageType"

    val JSON_VALUE_MessageType_Action = "action"
    val JSON_VALUE_MessageType_EndTurn = "endTurn"
    val JSON_VALUE_MessageType_Error = "error"
    val JSON_VALUE_MessageType_GameData = "gameData"

    val JSON_KEY_MessagePayload_Action_ClassName = "actionType"
    val JSON_KEY_MessagePayload_ActionData = "actionData"
    val JSON_KEY_MessagePayload_WorldData = "worldData"
    val JSON_KEY_MessagePayload_PlayerData = "playerData"
    val JSON_KEY_MessagePayload_ErrorMessage = "error"

    val JSON_VALUE_Error_UnknownType = "unknownType"
    val JSON_VALUE_Error_NonInstantiatableType = "nonInstantiatableType"

    val JSON_KEY_World_SizeX = "sizeX"
    val JSON_KEY_World_SizeY = "sizeY"
    val JSON_KEY_World_Entities = "entities"

    val JSON_KEY_Entity_Player = "player"
    val JSON_KEY_Entity_PositionX = "positionX"
    val JSON_KEY_Entity_PositionY = "positionY"
    val JSON_KEY_Entity_Type = "type"
    val JSON_KEY_Entity_ID = "id"

    //in case more than one id is requested in a single nanosecond (or milli, depending on system time resolution)
    //increment manually when under load, catch up later
    val newID: Long
        @Synchronized get() {
            val newID = System.nanoTime()
            if (newID <= previousID) {
                previousID++
            } else {
                previousID = newID
            }
            return previousID
        }

    //TODO: split by category
    enum class Identifiers {
        background1A, background1B, background1C, background1D,
        background2A, background2B, background2C, background2D,
        background3A, background3B, background3C, background3D,
        background4A, background4B, background4C, background4D,
        buttonDefault, buttonHover, buttonClick,
        menuBackground,
        entityShipDefault,
        infoPanelDefault, entityStructureDefault,
        gridClick, gridHover, gridDefault, gridSelected,
        hexagonClick, hexagonHover, hexagonDefault, hexagonSelected
    }
}
