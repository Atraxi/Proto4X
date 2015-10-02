package atraxi.game;

import java.awt.Dimension;
import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.Image;
import java.awt.Toolkit;
import java.math.BigDecimal;
import java.util.ArrayList;

import javax.swing.ImageIcon;
import javax.swing.JPanel;

import atraxi.game.UI.UserInterfaceHandler;
import entities.Entity;

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
    private World world;
    public static boolean paused;
    private static UserInterfaceHandler uiHandler;
    
    public Game(ArrayList<Player> players, UserInterfaceHandler uiHandler)
    {
        Game.players = players;
        Game.uiHandler = uiHandler;
        world = new World();
        setPreferredSize(new Dimension(Proto.screen_Width, Proto.screen_Width));
        setDoubleBuffered(true);
        paused = false;
    }
    
    public static ArrayList<Player> getPlayerList()
    {
        return players;
    }
    
    @Override
    public void paint(Graphics g)
    {
        super.paint(g);
        Graphics2D g2d = (Graphics2D) g;
        uiHandler.paintBackground(g2d);
        g2d.translate(UserInterfaceHandler.getScreenLocationX(), UserInterfaceHandler.getScreenLocationY());
        for( Entity entity : World.getEntityList())
        {
            g2d.drawImage(entity.getImage(), entity.getTransform(), null);
        }
        uiHandler.paintWorld(g2d);
        g2d.translate(-UserInterfaceHandler.getScreenLocationX(), -UserInterfaceHandler.getScreenLocationY());
        uiHandler.paintScreen(g2d);
        Toolkit.getDefaultToolkit().sync();
        g2d.dispose();
    }
    
    private void gameLoop(BigDecimal timeAdjustment)
    {
        for(Entity entity : World.getEntityList())
        {
            entity.doWork(timeAdjustment, paused);
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
                gameLoop(BigDecimal.valueOf(actualFrameTime)
                                   .divide(BigDecimal.valueOf(OPTIMALFRAMETIME), 8, BigDecimal.ROUND_DOWN));
                repaint();

                beforeTime = currentTime;
            }

        }
    }
}