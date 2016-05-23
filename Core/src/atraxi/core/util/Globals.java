package atraxi.core.util;

import java.util.Random;

public class Globals
{
    public static DebugState debug;
    public static final long SEED = System.nanoTime();
    public static Random random;

    //Networking layer JSON tag names
    public static final String JSONPlayerIndex = "playerIndex";
    public static final String JSONPlayerName = "playerName";

    public static final String JSONActionClassName = "actionType";

    public static final String JSONMessageTypeKey = "messageType";

    public static final String JSONMessageTypeValue_Action = "action";
    public static final String JSONMessageTypeValue_EndTurn = "endTurn";
    public static final String JSONMessageTypeValue_Error = "error";

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
