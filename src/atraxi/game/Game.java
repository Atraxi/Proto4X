package atraxi.game;

import java.awt.Dimension;
import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.Image;
import java.awt.Rectangle;
import java.awt.Toolkit;
import java.math.BigDecimal;
import java.util.ArrayList;

import javax.swing.ImageIcon;
import javax.swing.JPanel;

import atraxi.game.UI.UserInterfaceHandler;
import atraxi.game.World.World;
import entities.Entity;

public class Game extends JPanel implements Runnable
{
    private static final long serialVersionUID = 1L;
    /**
     * The desired time per frame (in nanoseconds) from which to adjust the speed of all events. 60fps or 1,000,000,000ns/60
     */
    public static final long OPTIMALFRAMETIME = 16666666;
    private Thread animator;
    private static ArrayList<Player> players;
    private World world;
    private Image mapImage;
    public static boolean paused;
    private static UserInterfaceHandler uiHandler;
    private Rectangle viewArea;
    
    public Game(ArrayList<Player> players, UserInterfaceHandler uiHandler)
    {
        Game.players = players;
        Game.uiHandler = uiHandler;
        world = new World();
        mapImage = new ImageIcon("resources/background.jpg").getImage();
        setPreferredSize(new Dimension(Proto.screen_Width, Proto.screen_Width));
        setDoubleBuffered(true);
        paused = false;
        viewArea = new Rectangle(0,0,Proto.screen_Width, Proto.screen_Width);
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
        g2d.drawImage(mapImage, 0, 0, null);
        for( Entity entity : World.getEntityList())
        {
            g2d.drawImage(entity.getImage(), entity.getTransform(), null);
        }
        uiHandler.paint(g2d);
        Toolkit.getDefaultToolkit().sync();
        g2d.dispose();
    }
    
    private void gameLoop(BigDecimal timeAdjustment)
    {
        for(Entity entity : World.getEntityList())
        {
            entity.doWork(timeAdjustment, paused);
        }
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
            
            if(actualFrameTime>OPTIMALFRAMETIME)
            {
                System.out.println("Game running behind, skipping "+((int)((actualFrameTime)/OPTIMALFRAMETIME))+" frames");
            }
            gameLoop(BigDecimal.valueOf(actualFrameTime).divide(BigDecimal.valueOf(OPTIMALFRAMETIME),8,BigDecimal.ROUND_DOWN));
            repaint();
            
            beforeTime = currentTime;
            
//            try
//            {
//                //Simulate a slow computer (this is actually really helpful)
//                Thread.sleep(5);
//            }
//            catch (InterruptedException e)
//            {
//                e.printStackTrace();
//            }
        }
    }
}