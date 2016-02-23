package atraxi.game;

import atraxi.game.world.World;
import atraxi.ui.UserInterfaceHandler;
import atraxi.util.Logger;
import atraxi.util.ResourceManager.ImageID;

import javax.swing.JFrame;
import javax.swing.WindowConstants;
import java.awt.AWTException;
import java.awt.Dimension;
import java.awt.EventQueue;
import java.awt.Rectangle;
import java.awt.Robot;
import java.awt.Toolkit;
import java.util.ArrayList;
import java.util.Random;

public class Proto extends JFrame
{
    private static final long serialVersionUID = 1L;
    public static int screen_Width;
    public static int screen_Height;
    public static boolean debug;
    public static Proto PROTO;

    public static final long SEED = System.nanoTime();
    public static Random random;

    public Proto()
    {
        super();
        PROTO = this;
        Dimension dim = Toolkit.getDefaultToolkit().getScreenSize();
        //TEST: This is terrible, but will probably be replaced when proper resolution options are added
        screen_Width = (int) (dim.width*0.75);
        screen_Height = (int) (dim.height*0.75);

        random = new Random(SEED);

        Player user = new Player();
        ArrayList<Player> players = new ArrayList<Player>();
        players.add(user);

        ArrayList<World> worlds = new ArrayList<World>();
        worlds.add(new World(random.nextInt(), new Rectangle(ImageID.gridDefault.getImage().getWidth(), ImageID.gridDefault.getImage().getHeight()), 100, 100,
                             ImageID.gridDefault, ImageID.gridHover, ImageID.gridClick, ImageID.gridSelected));

        UserInterfaceHandler ui = new UserInterfaceHandler(user, 0);
        try
        {
            Robot robot = new Robot();
            robot.mouseMove((int) (dim.width*0.5),(int)(dim.height * 0.5));
        }
        catch(AWTException e)
        {
            //TODO: environment either doesn't support, or doesn't allow, controlling mouse input. Log this, and disable features or quit if needed
            e.printStackTrace();
        }

        Game game = new Game(players, ui, worlds);
        addKeyListener(ui);
        //bind mouse to JPanel not JFrame to account for taskbar in mouse coords
        game.addMouseMotionListener(ui);
        game.addMouseListener(ui);
        game.addMouseWheelListener(ui);
        
        //add a JPanel to this JFrame
        add(game);
        
        setFocusable(true);
        //TODO: toggle windowed mode, add resizing and resolution options. This can probably wait for the main menu
        //setUndecorated(true);
        setDefaultCloseOperation(WindowConstants.EXIT_ON_CLOSE);
        setSize(screen_Width, screen_Height);
        setLocationRelativeTo(null);
        setTitle("4X_proto");
        setResizable(false);
    }

    public void setDimensions (int width, int height)
    {
        screen_Width=width;
        screen_Height=height;
        setSize(screen_Width,screen_Height);
    }
    
    public static void main(String[] args)
    {
        if(args.length>0 && args[0].equals("-debug"))
        {
            Logger.log(Logger.LogLevel.debug, new String[] {"Debug enabled"});
            debug = true;
        }
        else
        {
            debug = false;
        }

        EventQueue.invokeLater(() -> {
            Proto frame = new Proto();
            frame.setVisible(true);
        });
    }
}
