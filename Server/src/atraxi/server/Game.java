package atraxi.server;

import atraxi.core.Player;
import atraxi.core.util.Logger;
import atraxi.core.world.World;
import atraxi.server.networking.ServerUtil;

import java.math.BigDecimal;
import java.util.ArrayList;

public class Game implements Runnable
{
    /**
     * The desired time per frame (in nanoseconds) from which to adjust the speed of all events. 60fps or 1,000,000,000ns/60
     */
    public static final long OPTIMALFRAMETIME = 16666666;
    private static final long MINIMUMFRAMETIME = 8333333;
    private static ArrayList<Player> players;
    private static ArrayList<World> worlds;
    private static ServerUtil serverUtil;
    
    public Game(ArrayList<Player> players, ArrayList<World> worlds)
    {
        Game.players = players;
        Game.worlds = worlds;
        serverUtil = new ServerUtil(players.size());
        new Thread(serverUtil).start();
    }
    
    public static ArrayList<Player> getPlayerList()
    {
        return players;
    }

    public static World getWorld(int index)
    {
        return worlds.get(index);
    }
    
    private void gameLoop(BigDecimal timeAdjustment)
    {
//        for(World world : worlds)
//        {
//            for(Entity entity : world.getEntityList())
//            {
//                entity.doWork(timeAdjustment, paused);
//            }
//        }
    }
    
    @Override
    public void run()
    {
        long beforeTime, actualFrameTime;
        
        beforeTime = System.nanoTime();

        while(true)
        {
            long currentTime = System.nanoTime();
            
            actualFrameTime = currentTime-beforeTime;
            
            if(actualFrameTime > OPTIMALFRAMETIME)
            {
                Logger.log(Logger.LogLevel.warning, new String[] {"Game running behind, skipping " + ((int)((actualFrameTime) / OPTIMALFRAMETIME)) + " frames"});
            }
            if(actualFrameTime < MINIMUMFRAMETIME)
            {
                gameLoop(BigDecimal.valueOf(MINIMUMFRAMETIME).divide(BigDecimal.valueOf(OPTIMALFRAMETIME),8,BigDecimal.ROUND_DOWN));

                beforeTime = currentTime;

                try
                {
                    //Limit framerate to 120fps
                    Thread.sleep((MINIMUMFRAMETIME-actualFrameTime)/1000000);
                }
                catch (InterruptedException e)
                {//TODO: handle this
                    e.printStackTrace();
                }
            }
            else
            {
                gameLoop(BigDecimal.valueOf(actualFrameTime).divide(BigDecimal.valueOf(OPTIMALFRAMETIME),
                                                                    8,
                                                                    BigDecimal.ROUND_DOWN));

                beforeTime = currentTime;
            }
        }
    }

    public static void processTurn()
    {
        players.parallelStream().forEach(Player::processSetStatesThisTurn);
        players.parallelStream().forEach(Player::processBuildsThisTurn);
        players.parallelStream().forEach(Player::processAttacksThisTurn);
        players.parallelStream().forEach(Player::processMovesThisTurn);
    }
}