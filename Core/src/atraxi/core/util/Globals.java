package atraxi.core.util;

import atraxi.core.BaseNetworkObject;
import atraxi.core.Player;
import atraxi.core.world.World;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;
import java.util.Random;
import java.util.concurrent.ConcurrentHashMap;

public class Globals
{
    public static final int MAX_MOVEMENT_DISTANCE = 200;
    public static DebugState debug;
    public static final long SEED = System.nanoTime();
    public static Random random;

    public static final ArrayList<Player> players = new ArrayList<>();
    public static final Map<String, Player> playersByName = new HashMap<>();
    public static final ArrayList<World> worlds = new ArrayList<>();
    public static final Map<Long, Wrapper<BaseNetworkObject>> idMappedObjects = new ConcurrentHashMap<>();

    private static int runtimeID;
    private static long previousID = 0;

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
    public static final String JSON_KEY_MessagePayload_Error = "error";

    public static final String JSON_VALUE_Error_UnknownType = "unknownType";
    public static final String JSON_VALUE_Error_NonInstantiatableType = "nonInstantiatableType";

    public static final String JSON_KEY_World_SizeX = "sizeX";
    public static final String JSON_KEY_World_SizeY = "sizeY";
    public static final String JSON_KEY_World_Entities = "entities";

    public static final String JSON_KEY_BaseNetworkObject_ID = "id";

    public static final String JSON_KEY_Entity_Player = "player";
    public static final String JSON_KEY_Entity_PositionX = "positionX";
    public static final String JSON_KEY_Entity_PositionY = "positionY";
    public static final String JSON_KEY_Entity_ClassType = "classType";
    public static final String JSON_KEY_Entity_IdentifierType = "idType";

    public static final String JSON_KEY_Action_Source = "source";
    public static final String JSON_KEY_Action_Player = "player";

    public static final String JSON_KEY_ActionMove_TargetX = "targetX";
    public static final String JSON_KEY_ActionMove_TargetY = "targetY";

    public synchronized static long getNewID()
    {
        //TODO: salt the ids? Knowing the age of an enemy ship could potentially impact strategy, there could be other minor exploits too. Would need to be guaranteed unique
        // still and have minimal additional performance impact. Debatable if this is actually worth looking into except for a bit of fun
        long newID = System.nanoTime();
        newID = newID & 0x0000_0000_FFFF_FFFFL | ((long)runtimeID << 32);
        //in case more than one id is requested in a single nanosecond (or milli, depending on system time resolution)
        //increment manually when under load, catch up later
        if(newID <= previousID)
        {
            previousID++;
            newID = previousID;
        }
        else
        {
            previousID = newID;
        }
        return newID;
    }

    public static World getWorld(int index)
    {
        return worlds.get(index);
    }

    public static int getWorldCount()
    {
        return worlds.size();
    }

    public static void setRuntimeID(int runtimeID)
    {
        Globals.runtimeID = runtimeID;
    }

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

    private class Wrapper<T extends BaseNetworkObject>
    {
        private T value;

        public Wrapper(T value)
        {
            this.value = value;
        }

        @Override
        public void finalize() throws Throwable
        {
            idMappedObjects.remove(value.getID());
            super.finalize();
        }
    }
}
