package atraxi.game;

import atraxi.game.world.World;
import atraxi.ui.UserInterfaceHandler;
import atraxi.util.Logger;

import javax.swing.JFrame;
import javax.swing.WindowConstants;
import java.awt.AWTException;
import java.awt.Dimension;
import java.awt.EventQueue;
import java.awt.Polygon;
import java.awt.Robot;
import java.awt.Toolkit;
import java.util.ArrayList;
import java.util.Random;

import static atraxi.util.ResourceManager.ImageID;

public class Proto extends JFrame
{
    private static final long serialVersionUID = 1L;
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
        screen_Width = (int) (dim.width * 0.75);
        screen_Height = (int) (dim.height * 0.75);

        random = new Random(SEED);

        Player user = new Player();
        ArrayList<Player> players = new ArrayList<Player>();
        players.add(user);

        ArrayList<World> worlds = new ArrayList<World>();
        
        //Hexagonal - assumes regular hexagon with points at top/bottom with all points tightly bound to image dimensions, traverse points clockwise from top center
        worlds.add(new World(random.nextInt(),
                             new Polygon(new int[]
                                                 {
                                                         ImageID.hexagonDefault.getImage().getWidth() / 2,
                                                         ImageID.hexagonDefault.getImage().getWidth(),
                                                         ImageID.hexagonDefault.getImage().getWidth(),
                                                         ImageID.hexagonDefault.getImage().getWidth() / 2,
                                                         0,
                                                         0
                                                 },
                                         new int[]
                                                 {
                                                         0,
                                                         ImageID.hexagonDefault.getImage().getHeight() / 4,
                                                         3 * ImageID.hexagonDefault.getImage().getHeight() / 4,
                                                         ImageID.hexagonDefault.getImage().getHeight(),
                                                         3 * ImageID.hexagonDefault.getImage().getHeight() / 4,
                                                         ImageID.hexagonDefault.getImage().getHeight() / 4
                                                 },
                                         6),
                             10,
                             10,
                             ImageID.hexagonDefault, ImageID.hexagonHover, ImageID.hexagonClick, ImageID.hexagonSelected));

        UserInterfaceHandler ui = new UserInterfaceHandler(user, 0);
        try
        {
            Robot robot = new Robot();
            robot.mouseMove((int) (dim.width * 0.5), (int) (dim.height * 0.5));
        }
        catch (AWTException e)
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

    public void setDimensions(int width, int height)
    {
        screen_Width = width;
        screen_Height = height;
        setSize(screen_Width, screen_Height);
    }

    public static void main(String[] args)
    {
        //TODO: a debug object/enum to toggle various debug features
        if (args.length > 0 && args[0].equals("-debug"))
        {
            Logger.log(Logger.LogLevel.debug, new String[]{"Debug enabled"});
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
    /**
     * Get the width of the active game area (i.e. without the title bar etc)
     */
    public static int getScreenWidth()
    {
        return PROTO.getContentPane().getWidth();
    }

    /**
     * Get the height of the active game area (i.e. without the title bar etc)
     */
    public static int getScreenHeight()
    {
        return PROTO.getContentPane().getHeight();
    }
}
