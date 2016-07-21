package atraxi.client;

import atraxi.client.networking.ClientUtil;
import atraxi.client.ui.UserInterfaceHandler;
import atraxi.client.ui.wrappers.WorldUIWrapper;
import atraxi.client.util.RenderUtil;
import atraxi.core.Player;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;
import org.json.JSONObject;

import javax.swing.JPanel;
import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.RenderingHints;
import java.awt.Toolkit;
import java.awt.geom.AffineTransform;
import java.io.IOException;
import java.math.BigDecimal;
import java.util.ArrayList;

public class Game extends JPanel implements Runnable
{
    private static final long serialVersionUID = 1L;
    /**
     * The desired time per frame (in nanoseconds) from which to adjust the speed of all events. 60fps or 1,000,000,000ns/60
     */
    public static final long OPTIMALFRAMETIME = 16666666;
    private static final long MINIMUMFRAMETIME = 8333333;
    private static ArrayList<Player> players;
    private static ArrayList<WorldUIWrapper> worlds;
    public static boolean paused;
    private static UserInterfaceHandler uiHandler;
    private static RenderUtil renderUtil;
    private static ClientUtil clientUtil;
    private static boolean hasTurnEnded;

    public Game(UserInterfaceHandler uiHandler, ClientUtil clientUtil, ArrayList<WorldUIWrapper> worlds, ArrayList<Player> players)
    {
        Game.uiHandler = uiHandler;
        Game.clientUtil = clientUtil;
        Game.worlds = worlds;
        Game.players = players;
        setDoubleBuffered(true);
        paused = true;
        renderUtil = new RenderUtil();
    }
    
    public static ArrayList<Player> getPlayerList()
    {
        return players;
    }

    public static WorldUIWrapper getWorld(int index)
    {
        return worlds.get(index);
    }

    public static int getWorldCount()
    {
        return worlds.size();
    }

    public static ClientUtil getClientUtil()
    {
        return clientUtil;
    }

    @Override
    public void paint(Graphics g)
    {
        super.paint(g);
        Graphics2D g2d = (Graphics2D) g;

        g2d.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
        g2d.setRenderingHint(RenderingHints.KEY_RENDERING, RenderingHints.VALUE_RENDER_QUALITY);

        renderUtil.setG2d(g2d);

        if(Globals.debug.isExpandedZoomEnabled())
        {//zoom out, show a box where the edge of the screen would normally be
            g2d.scale(0.75, 0.75);
            g2d.translate(250, 100);
            g2d.drawRect(0, 0, getWidth(), getHeight());
        }

        //The background doesn't move at the same speed as the rest of the game objects, due to the desired parallax illusion,
        // so the camera offset is managed inside it's paint method
//        uiHandler.paintBackground(g2d);
//        uiHandler.paintBackgroundAtScrollSpeed(1, g2d, renderUtil);

        //Transform camera position/scale/rotation to draw any world objects
        AffineTransform g2dTransformBackup = g2d.getTransform();
        g2d.transform(UserInterfaceHandler.getWorldTransform());

        UserInterfaceHandler.paintWorld(renderUtil);

        //Remove/reset camera transform to draw UI
        g2d.setTransform(g2dTransformBackup);

        UserInterfaceHandler.paintScreen(renderUtil, hasTurnEnded);

//        new InfoPanel(new Rectangle(40, 40, ImageID.infoPanelDefault.getImage().getWidth(null)+200, ImageID.infoPanelDefault.getImage().getHeight(null)+200),
//                      ImageID.infoPanelDefault,0,0,0).paint(renderUtil);

        Toolkit.getDefaultToolkit().sync();
        g2d.dispose();
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
        UserInterfaceHandler.doWork(timeAdjustment, paused);
    }
    
    @Override
    public void addNotify()
    {
        super.addNotify();

        Thread animator = new Thread(this);
        animator.start();
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
                repaint();

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
                repaint();

                beforeTime = currentTime;
            }
        }
    }

    public static void endTurn()
    {
        hasTurnEnded = true;

        JSONObject jsonObject = new JSONObject();
        jsonObject.put(Globals.JSON_KEY_MessageType, Globals.JSON_VALUE_MessageType_EndTurn);
        try
        {
            clientUtil.sendToServer(jsonObject);
        }
        catch(IOException e)
        {
            e.printStackTrace();
        }
    }
}