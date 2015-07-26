package debug;

import java.util.ArrayList;

import atraxi.game.UserInterfaceHandler;
import factions.Player;

/**
 * @author Atraxi
 * 
 * debug mode; no part of the core game should be modified for, or hold any direct reference to, this class
 * any non-essential logging should be done here, tracking detailed game state etc.
 */
public class TestSystems
{
    private ArrayList<Player> players;
    private DebugKeyIntercept debugKey;
    
    public TestSystems(UserInterfaceHandler ui, ArrayList<Player> players)
    {
        this.players = players;
    }
}
