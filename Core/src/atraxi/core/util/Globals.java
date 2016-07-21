package atraxi.core.util;

import java.util.Random;

public class Globals
{
    public static final int MAX_MOVEMENT_DISTANCE = 200;
    public static DebugState debug;
    public static final long SEED = System.nanoTime();
    public static Random random;

    //Networking layer JSON tag names
    public static final String JSON_KEY_INITIALIZATION_PlayerIndex = "playerIndex";
    public static final String JSON_KEY_INITIALIZATION_PlayerName = "playerName";

    public static final String JSON_KEY_MessageType = "messageType";

    public static final String JSON_VALUE_MessageType_Action = "action";
    public static final String JSON_VALUE_MessageType_EndTurn = "endTurn";
    public static final String JSON_VALUE_MessageType_Error = "error";
    public static final String JSON_VALUE_MessageType_GameData = "gameData";

    public static final String JSON_KEY_MessagePayload_Action_ClassName = "actionType";
    public static final String JSON_KEY_MessagePayload_ActionData = "actionData";
    public static final String JSON_KEY_MessagePayload_WorldData = "worldData";
    public static final String JSON_KEY_MessagePayload_PlayerData = "playerData";

    public static final String JSON_KEY_World_SizeX = "sizeX";
    public static final String JSON_KEY_World_SizeY = "sizeY";
    public static final String JSON_KEY_World_Entities = "entities";

    public static final String JSON_KEY_Entity_Player = "player";
    public static final String JSON_KEY_Entity_PositionX = "positionX";
    public static final String JSON_KEY_Entity_PositionY = "positionY";
    public static final String JSON_KEY_Entity_Type = "type";

    //TODO: split by category
    public enum Identifiers
    {
        background1A,background1B,background1C,background1D,
        background2A,background2B,background2C,background2D,
        background3A,background3B,background3C,background3D,
        background4A,background4B,background4C,background4D,
        buttonDefault,buttonHover,buttonClick,
        menuBackground,
        entityShipDefault,
        infoPanelDefault, entityStructureDefault,
        gridClick, gridHover, gridDefault, gridSelected,
        hexagonClick, hexagonHover, hexagonDefault, hexagonSelected;
    }
}
