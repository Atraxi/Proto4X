package atraxi.game;

import atraxi.game.world.World;
import atraxi.ui.UserInterfaceHandler;
import atraxi.util.Logger;

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

import static atraxi.util.ResourceManager.ImageID;

public class Proto extends JFrame
{
    private static final long serialVersionUID = 1L;
    public static DebugState debug;
    public static Proto PROTO;
    private static Dimension physicalScreenSize;

    public static final long SEED = System.nanoTime();
    public static Random random;

    public Proto()
    {
        super();
        PROTO = this;
        physicalScreenSize = Toolkit.getDefaultToolkit().getScreenSize();

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
                             100,
                             100,
                             ImageID.hexagonDefault, ImageID.hexagonHover, ImageID.hexagonClick, ImageID.hexagonSelected));

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
        //TODO: a debug object/enum to toggle various debug features
        if (args.length > 0)
        {
            for(int i = 0; i < args.length; i++)
            {
                switch(args[i])
                {
                    case "-debug":
                        boolean expandedZoom = false;
                        int infoLevel = 0;
                        while(!args[i+1].startsWith("-"))
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
                            }
                        }
                        debug = new DebugState(expandedZoom, infoLevel);
                        Logger.log(Logger.LogLevel.debug, new String[]{"Debug enabled, " + debug});
                        break;
                }
            }
        }
        else
        {
            debug = new DebugState(false, 0);
        }

        EventQueue.invokeLater(() -> {
            Proto frame = new Proto();
            frame.setIconImage(ImageID.entityShipDefault.getImage());
            frame.setLocationByPlatform(true);
            frame.setVisible(true);
            Insets insets = frame.getInsets();
            frame.getContentPane().setSize((int) (physicalScreenSize.width * 0.75), (int) (physicalScreenSize.height * 0.75));
            frame.setSize((int) ((physicalScreenSize.width * 0.75) + insets.left + insets.right), (int) ((physicalScreenSize.height * 0.75) + insets.top + insets.bottom));
            Logger.log(Logger.LogLevel.info, new String[]{"screen_Width:" + getScreenWidth(),"screen_Height:" + getScreenHeight()});

            try
            {
                Robot robot = new Robot();
                robot.mouseMove(getScreenWidth()/2 + frame.getContentPane().getLocationOnScreen().x, getScreenHeight()/2 + frame.getContentPane().getLocationOnScreen().y);
            }
            catch(AWTException e)
            {
                //TODO: environment either doesn't support, or doesn't allow, controlling mouse input. Log this, and disable features or quit if needed
                e.printStackTrace();
            }
        });
    }

    public static class DebugState
    {
        private boolean expandedZoom;
        private int infoLevel;

        private DebugState(boolean expandedZoom, int infoLevel)
        {
            this.expandedZoom = expandedZoom;
            this.infoLevel = infoLevel;
        }

        public boolean isExpandedZoomEnabled()
        {
            return expandedZoom;
        }

        /**
         * @return An indication of how verbose various info displays should be, where 0 is the absolute minimum
         */
        public int getDetailedInfoLevel()
        {
            return infoLevel;
        }

        @Override
        public String toString()
        {
            return "DebugState{" +
                   "expandedZoom=" + expandedZoom +
                   ", detailedInfoLevel=" + infoLevel +
                   '}';
        }
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
