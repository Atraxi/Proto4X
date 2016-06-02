package atraxi.core.util;

import java.util.Random;

public class Globals
{
    public static DebugState debug;
    public static final long SEED = System.nanoTime();
    public static Random random;

    //Networking layer JSON tag names
    public static final String JSON_KEY_PlayerIndex = "playerIndex";
    public static final String JSON_KEY_PlayerName = "playerName";

    public static final String JSON_KEY_ActionClassName = "actionType";
    public static final String JSON_KEY_ActionData = "actionData";

    public static final String JSON_KEY_MessageType = "messageType";

    public static final String JSON_VALUE_MessageType_Action = "action";
    public static final String JSON_VALUE_MessageType_EndTurn = "endTurn";
    public static final String JSON_VALUE_MessageType_Error = "error";

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
