package atraxi.game;

import java.awt.Dimension;
import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.Image;
import java.awt.Toolkit;
import java.util.ArrayList;

import javax.swing.ImageIcon;
import javax.swing.JPanel;

import entities.Entity;
import factions.Human;
import factions.Player;

public class Game extends JPanel implements Runnable
{
    private static final long serialVersionUID = 1L;
    public static final long DELAY = 16;
    private Thread animator;
    private static ArrayList<Player> players;// = new ArrayList<Player>();
    private Image mapImage;
    public static boolean paused;
    private World world;
    public static UserInterfaceHandler uiHandler;
    
    public Game(ArrayList<Player> players)
    {
        Game.players = players;
        uiHandler = new UserInterfaceHandler();
        world = new World();
        mapImage = new ImageIcon("resources/background.jpg").getImage();
        setPreferredSize(new Dimension(Proto.screen_Width, Proto.screen_Width));
        setDoubleBuffered(true);
        paused = false;
        
        addMouseMotionListener((Human)players.get(0));
        addMouseListener((Human)players.get(0));
        addMouseWheelListener((Human)players.get(0));
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
        uiHandler.paint(g);
        Toolkit.getDefaultToolkit().sync();
        g.dispose();
    }
    
    private void gameLoop(long timeDiff)
    {
        for(Entity entity : World.getEntityList())
        {
            entity.doWork(timeDiff, paused);
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
        long beforeTime, timeDiff, sleep;
        
        beforeTime=System.currentTimeMillis();//.currentTimeMillis();
        
        while(true)
        {
            long currentTime = System.currentTimeMillis();//.currentTimeMillis();
            timeDiff = currentTime-beforeTime;
            
            if(timeDiff>DELAY*2)
            {
                System.out.println("Game running behind, skipping "+((int)((timeDiff-DELAY)/DELAY))+" frames");
            }
            while(timeDiff>DELAY)
            {
                timeDiff -= DELAY;
            }
            //TODO: IMPORTANT separate game loop and render into separate threads.
            //requires determining all requirements for render, and locking relevant code. all changes probably need to be done 'simultaneously'
            //perhaps clone relevant data to render thread, and lock entire game loop?
            //it would probably be wise to research possible approaches before seriously attempting this, as it will require significant changes to the code
            //making code thread safe will also likely be useful for threaded AI
            gameLoop(timeDiff);
            repaint();
            
            sleep=DELAY-timeDiff;
            
            try
            {
                Thread.sleep(sleep);
            }
            catch(InterruptedException e)
            {
                System.out.println("Interrupted: "+e.getMessage());
            }
            
            beforeTime = currentTime;
        }
    }
}