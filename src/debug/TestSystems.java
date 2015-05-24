package debug;

import java.util.ArrayList;

import atraxi.game.Human;
import atraxi.game.Player;

/**
 * @author Atraxi
 * 
 * debug mode; no part of the core game should be modified for, or hold any reference in any form to, this class
 * any non-essential logging should be done here, tracking detailed game state etc.
 */
public class TestSystems
{
    private ArrayList<Player> players;
    private DebugKeyIntercept debugKey;
    
    public TestSystems(ArrayList<Player> players)
    {
        this.players = players;
        for(Player player : players)
        {
            if(player instanceof Human)
            {
                debugKey = new DebugKeyIntercept((Human)player);
            }
        }
    }
    
    public DebugKeyIntercept getDebugKeyIntercept()
    {
        return debugKey;
    }
}
