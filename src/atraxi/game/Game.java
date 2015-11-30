package atraxi.game;

import java.awt.Dimension;
import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.Rectangle;
import java.awt.Toolkit;
import java.math.BigDecimal;
import java.util.ArrayList;

import javax.swing.JPanel;

import atraxi.ui.InfoPanel;
import atraxi.ui.UserInterfaceHandler;
import atraxi.entities.Entity;
import atraxi.util.ResourceManager;
import atraxi.util.ResourceManager.ImageID;

public class Game extends JPanel implements Runnable
{
    private static final long serialVersionUID = 1L;
    /**
     * The desired time per frame (in nanoseconds) from which to adjust the speed of all events. 60fps or 1,000,000,000ns/60
     */
    public static final long OPTIMALFRAMETIME = 16666666;
    private static final long MINIMUMFRAMETIME = 8333333;
    private Thread animator;
    private static ArrayList<Player> players;
    private static ArrayList<World> worlds;
    public static boolean paused;
    private static UserInterfaceHandler uiHandler;
    public static final long SEED = System.nanoTime();
    
    public Game(ArrayList<Player> players, UserInterfaceHandler uiHandler)
    {
        Game.players = players;
        Game.uiHandler = uiHandler;
        worlds = new ArrayList<World>();
        worlds.add(new World(SEED, 10000, 10000));
        setPreferredSize(new Dimension(Proto.screen_Width, Proto.screen_Height));
        setDoubleBuffered(true);
        paused = false;
    }
    
    public static ArrayList<Player> getPlayerList()
    {
        return players;
    }

    public static World getWorld(int index)
    {
        return worlds.get(index);
    }
    
    @Override
    public void paint(Graphics g)
    {
        super.paint(g);
        Graphics2D g2d = (Graphics2D) g;
        if(Proto.debug)
        {//zoom out, show a box where the edge of the screen would normally be
            g2d.scale(0.75, 0.75);
            g2d.translate(250, 100);
            g2d.drawRect(0, 0, Proto.screen_Width, Proto.screen_Height);
        }
        //The background doesn't move at the same speed as the rest of the game objects, due to the desired parallax illusion,
        // so the camera offset is managed inside it's paint method
        uiHandler.paintBackground(g2d);
        //Offset to camera position to draw any world objects
        g2d.translate(UserInterfaceHandler.getScreenLocationX(), UserInterfaceHandler.getScreenLocationY());
        for(World world : worlds)
        {
            for(Entity entity : world.getEntityList())
            {
                entity.paint(g2d);
            }
        }
        uiHandler.paintWorld(g2d);
        //Remove camera offset to draw UI
        g2d.translate(-UserInterfaceHandler.getScreenLocationX(), -UserInterfaceHandler.getScreenLocationY());
        uiHandler.paintScreen(g2d);

        new InfoPanel(new Rectangle(20, 20, ResourceManager.getImage(ImageID.infoPanelDefault).getWidth(null)+100, ResourceManager.getImage(ImageID.infoPanelDefault).getHeight(null)+100),
                      ImageID.infoPanelDefault,0,0,0).paint(g2d);

        Toolkit.getDefaultToolkit().sync();
        g2d.dispose();
    }
    
    private void gameLoop(BigDecimal timeAdjustment)
    {
        for(World world : worlds)
        {
            for(Entity entity : world.getEntityList())
            {
                entity.doWork(timeAdjustment, paused);
            }
        }
        uiHandler.doWork(timeAdjustment, paused);
    }
    
    @Override
    public void addNotify()
    {
        super.addNotify();
        
        animator = new Thread(this);
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
                System.out.println("Game running behind, skipping "+((int)((actualFrameTime)/OPTIMALFRAMETIME))+" frames");
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
}