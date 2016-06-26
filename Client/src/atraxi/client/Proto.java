package atraxi.client;

import atraxi.client.ui.UserInterfaceHandler;
import atraxi.client.ui.wrappers.WorldUIWrapper;
import atraxi.client.util.ResourceManager;
import atraxi.core.Player;
import atraxi.core.util.DebugState;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;
import atraxi.core.world.World;

import javax.swing.JFrame;
import javax.swing.WindowConstants;
import java.awt.AWTException;
import java.awt.Dimension;
import java.awt.EventQueue;
import java.awt.Insets;
import java.awt.Polygon;
import java.awt.Robot;
import java.awt.Toolkit;
import java.util.ArrayList;
import java.util.Random;

public class Proto extends JFrame
{
    private static final long serialVersionUID = 1L;
    private static Dimension physicalScreenSize;
    private static Proto PROTO;

    private Proto()
    {
        super();
        physicalScreenSize = Toolkit.getDefaultToolkit().getScreenSize();

        Globals.random = new Random(Globals.SEED);

        Player user = new Player("testPlayer");
        ArrayList<Player> players = new ArrayList<Player>();
        players.add(user);

        ArrayList<WorldUIWrapper> worlds = new ArrayList<WorldUIWrapper>();

        //Hexagonal - assumes regular hexagon with points at top/bottom with all points tightly bound to image dimensions, traverse points clockwise from top center
        worlds.add(new WorldUIWrapper(new World(Globals.random.nextInt(), 100, 100),
                                      new Polygon(new int[]
                                                 {
                                                         ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getWidth() / 2,
                                                         ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getWidth(),
                                                         ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getWidth(),
                                                         ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getWidth() / 2,
                                                         0,
                                                         0
                                                 },
                                         new int[]
                                                 {
                                                         0,
                                                         ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getHeight() / 4,
                                                         3 * ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getHeight() / 4,
                                                         ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getHeight(),
                                                         3 * ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getHeight() / 4,
                                                         ResourceManager.getImage(Globals.Identifiers.hexagonDefault).getHeight() / 4
                                                 },
                                         6),
                                      Globals.Identifiers.hexagonDefault, Globals.Identifiers.hexagonHover, Globals.Identifiers.hexagonClick, Globals.Identifiers.hexagonSelected));

        UserInterfaceHandler ui = new UserInterfaceHandler(user, 0);

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

        setTitle("4X_proto");
        setResizable(false);
    }

    public void setDimensions(int width, int height)
    {
        Insets insets = getInsets();
        getContentPane().setSize(width, height);
        setSize(width + insets.left + insets.right, height + insets.top + insets.bottom);
    }

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
            }
        }

        EventQueue.invokeLater(() -> {
            Proto frame = new Proto();
            frame.setIconImage(ResourceManager.getImage(Globals.Identifiers.entityShipDefault));
            frame.setLocationByPlatform(true);
            frame.setVisible(true);
            Insets insets = frame.getInsets();
            frame.getContentPane().setSize((int) (physicalScreenSize.width * 0.75), (int) (physicalScreenSize.height * 0.75));
            frame.setSize((int) ((physicalScreenSize.width * 0.75) + insets.left + insets.right), (int) ((physicalScreenSize.height * 0.75) + insets.top + insets.bottom));
            Logger.log(Logger.LogLevel.info, new String[]{"screen_Width:" + frame.getContentPane().getWidth(), "screen_Height:" + frame.getContentPane().getHeight()});

            try
            {
                Robot robot = new Robot();
                robot.mouseMove(frame.getContentPane().getWidth()/2 + frame.getContentPane().getLocationOnScreen().x,
                                frame.getContentPane().getHeight()/2 + frame.getContentPane().getLocationOnScreen().y);
            }
            catch(AWTException e)
            {
                //TODO: environment either doesn't support, or doesn't allow, controlling mouse input. Log this, and disable features or quit if needed
                e.printStackTrace();
            }
            PROTO = frame;
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
