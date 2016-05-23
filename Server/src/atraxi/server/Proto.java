package atraxi.server;

import atraxi.core.Player;
import atraxi.core.util.DebugState;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;
import atraxi.core.world.World;

import java.util.ArrayList;
import java.util.Random;

public class Proto
{
    public static void main(String[] args)
    {
        Globals.debug = new DebugState(false, 0);
        for(int i = 0; i < args.length; i++)
        {
            switch(args[i])
            {
                case "-debug":
                    boolean expandedZoom = false;
                    int infoLevel = 0;
                    while(i < args.length-1 && !args[i+1].startsWith("-"))
                    {
                        i++;
                        String[] splitArgs = args[i].split("=");
                        switch(splitArgs[0])
                        {
                            case "expandedZoom":
                                expandedZoom = Boolean.valueOf(splitArgs[1]);
                                break;
                            case "infoLevel":
                                infoLevel = Integer.valueOf(splitArgs[1]);
                                break;
                            default:
                                Logger.log(Logger.LogLevel.warning, new String[]{"Unknown debug flag \"" + args[i] + "\""});
                                break;
                        }
                    }
                    Globals.debug = new DebugState(expandedZoom, infoLevel);
                    Logger.log(Logger.LogLevel.debug, new String[]{"Debug enabled, " + Globals.debug});
                    break;
                default:
                    Logger.log(Logger.LogLevel.debug, new String[]{"Unknown argument \"" + args[i] + "\""});
                    break;
            }
        }

        Globals.random = new Random(Globals.SEED);

        Player user = new Player();
        ArrayList<Player> players = new ArrayList<Player>();
        players.add(user);

        ArrayList<World> worlds = new ArrayList<World>();

        //Hexagonal - assumes regular hexagon with points at top/bottom with all points tightly bound to image dimensions, traverse points clockwise from top center
        worlds.add(new World(Globals.random.nextInt(), 100, 100));

        Game game = new Game(players, worlds);
    }
}
