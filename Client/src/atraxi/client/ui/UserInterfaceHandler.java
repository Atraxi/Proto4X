package atraxi.client.ui;

import atraxi.client.Game;
import atraxi.client.Proto;
import atraxi.client.ui.wrappers.WorldUIWrapper;
import atraxi.client.util.RenderUtil;
import atraxi.client.util.ResourceManager;
import atraxi.core.Player;
import atraxi.core.entities.Entity;
import atraxi.core.entities.action.ActionMoveTestImpl;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;
import atraxi.core.world.World;

import java.awt.Graphics2D;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.event.KeyEvent;
import java.awt.event.KeyListener;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.awt.event.MouseMotionListener;
import java.awt.event.MouseWheelEvent;
import java.awt.event.MouseWheelListener;
import java.awt.geom.AffineTransform;
import java.awt.geom.NoninvertibleTransformException;
import java.io.IOException;
import java.math.BigDecimal;
import java.util.Random;

public class UserInterfaceHandler implements KeyListener, MouseListener, MouseWheelListener, MouseMotionListener
{
    private static Point selectedLocation;
    private static Player user;
    private static final Globals.Identifiers[][] mapImages = {
            {Globals.Identifiers.background1A,Globals.Identifiers.background1B,Globals.Identifiers.background1C,Globals.Identifiers.background1D},
            {Globals.Identifiers.background2A,Globals.Identifiers.background2B,Globals.Identifiers.background2C,Globals.Identifiers.background2D},
            {Globals.Identifiers.background3A,Globals.Identifiers.background3B,Globals.Identifiers.background3C,Globals.Identifiers.background3D},
            {Globals.Identifiers.background4A,Globals.Identifiers.background4B,Globals.Identifiers.background4C,Globals.Identifiers.background4D}};
    private static final int edgeScrollArea = 50;
    private static int currentWorldIndex;

    //variables related to edge scrolling
    private static AffineTransform worldTransform = new AffineTransform();
    //initial value is mostly irrelevant, and will be set properly the instant the mouse is moved
    private static Point mouseLocation = new Point(200, 200);

    public final static UIStack uiStack = new UIStack();
    private static boolean isScrollEnabled = false;

    //Used in paintBackground(), avoids recreating an instance ~100 times (or more) a frame
    private static final Random rand = new Random();

    public UserInterfaceHandler(Player user, int defaultWorldIndex)
    {
        UserInterfaceHandler.user = user;

        currentWorldIndex = defaultWorldIndex;
    }

    public static int getScreenLocationX ()
    {
        return (int) worldTransform.getTranslateX();
    }

    public static int getScreenLocationY ()
    {
        return (int) worldTransform.getTranslateY();
    }

    public static int getCurrentWorldIndex()
    {
        return currentWorldIndex;
    }

    public static Point getMouseLocation()
    {
        return mouseLocation;
    }

    /**
     * Paint the background layer, everything else will draw above this
     * @param graphics2D
     */
    public void paintBackground(Graphics2D graphics2D)
    {
        double[] viewArea = {0, 0,
                Proto.getScreenWidth(), 0,
                Proto.getScreenWidth(), Proto.getScreenHeight(),
                0, Proto.getScreenHeight()};
        try
        {
            double[] modifiedViewArea = new double[8];
            worldTransform.inverseTransform(viewArea, 0, modifiedViewArea, 0, 4);

            double xMin = Math.min(modifiedViewArea[0], Math.min(modifiedViewArea[2], Math.min(modifiedViewArea[4], modifiedViewArea[6])));
            double xMax = Math.max(modifiedViewArea[0], Math.max(modifiedViewArea[2], Math.max(modifiedViewArea[4], modifiedViewArea[6])));

            double yMin = Math.min(modifiedViewArea[1], Math.min(modifiedViewArea[3], Math.min(modifiedViewArea[5], modifiedViewArea[7])));
            double yMax = Math.max(modifiedViewArea[1], Math.max(modifiedViewArea[3], Math.max(modifiedViewArea[5], modifiedViewArea[7])));
            for(int i = 1, j = 0; i <= 8; i = i * 2, j++)
            {
                int mapImageWidth = ResourceManager.getImage(mapImages[j][0]).getWidth(null);
                int mapImageHeight = ResourceManager.getImage(mapImages[j][0]).getHeight(null);
                for(int backgroundOffsetX = (int) Math.round((xMin / i % mapImageWidth) - (xMin > 0 ? mapImageWidth : 0));
                    backgroundOffsetX <= xMax;
                    backgroundOffsetX += mapImageWidth)
                {
                    for(int backgroundOffsetY = (int) Math.round((yMin / i % mapImageHeight) - (yMin > 0 ? mapImageHeight : 0));
                        backgroundOffsetY <= yMax;
                        backgroundOffsetY += mapImageHeight)
                    {
                        int indexX = (int) ((backgroundOffsetX - Math.round(xMin / i)) / mapImageWidth);
                        int indexY = (int) ((backgroundOffsetY - Math.round(yMin / i)) / mapImageHeight);
                        rand.setSeed(((long)indexX << 32 + indexY) ^ (Globals.SEED * i));
                        graphics2D.drawImage(ResourceManager.getImage(mapImages[j][rand.nextInt(4)]), backgroundOffsetX, backgroundOffsetY, null);
                        if(Globals.debug.getDetailedInfoLevel() >= 4)
                        {
                            graphics2D.scale(4, 4);
                            graphics2D.drawString("x:" + indexX + " y:" + indexY,
                                                    backgroundOffsetX/4 + 20,
                                                    backgroundOffsetY/4 + 20);
                            graphics2D.scale(0.25, 0.25);
                        }
                    }
                }
            }
        }
        catch(NoninvertibleTransformException e)
        {
            Logger.log(Logger.LogLevel.error, new String[]{"Camera has entered an invalid state, resetting."});
            e.printStackTrace();
            //TODO: save current worldTransform for error reporting
            worldTransform.setToIdentity();
        }
    }

    /**
     * Paint anything that moves relative to objects within the game world (ships, planets, etc)
     * @param renderUtil
     */
    public static void paintWorld(RenderUtil renderUtil)
    {
        WorldUIWrapper worldUIWrapper = Game.getWorld(currentWorldIndex);
        //{0,0} to {screenWidth,screenHeight}, buffered by the width/height of a tile in each direction. The size of the buffer is overkill but not by enough to care about
        // refining it
        double[] viewArea = {-worldUIWrapper.getTileWidth(), -worldUIWrapper.getTileHeight(),
                Proto.getScreenWidth() + worldUIWrapper.getTileWidth(), -worldUIWrapper.getTileHeight(),
                Proto.getScreenWidth() + worldUIWrapper.getTileWidth(), Proto.getScreenHeight() + worldUIWrapper.getTileHeight(),
                -worldUIWrapper.getTileWidth(), Proto.getScreenHeight() + worldUIWrapper.getTileHeight()};
        try
        {
            worldTransform.inverseTransform(viewArea,0,viewArea,0,4);
            Point[] indexes = {
                    World.convertAxialToOffset(worldUIWrapper.getTileAxialIndexFromPixelLocation((int) viewArea[0], (int) viewArea[1])),
                    World.convertAxialToOffset(worldUIWrapper.getTileAxialIndexFromPixelLocation((int) viewArea[2], (int) viewArea[3])),
                    World.convertAxialToOffset(worldUIWrapper.getTileAxialIndexFromPixelLocation((int) viewArea[4], (int) viewArea[5])),
                    World.convertAxialToOffset(worldUIWrapper.getTileAxialIndexFromPixelLocation((int) viewArea[6], (int) viewArea[7]))
            };
            int xMin = Math.min(indexes[0].x, Math.min(indexes[1].x, Math.min(indexes[2].x, indexes[3].x)));
            int xMax = Math.max(indexes[0].x, Math.max(indexes[1].x, Math.max(indexes[2].x, indexes[3].x)));

            int yMin = Math.min(indexes[0].y, Math.min(indexes[1].y, Math.min(indexes[2].y, indexes[3].y)));
            int yMax = Math.max(indexes[0].y, Math.max(indexes[1].y, Math.max(indexes[2].y, indexes[3].y)));

            for(int x = Math.max(0, xMin); x <= Math.min(worldUIWrapper.getWorld().getSizeX(), xMax); x++)
            {
                for(int y = Math.max(0, yMin); y <= Math.min(worldUIWrapper.getWorld().getSizeX(), yMax); y++)
                {
                    worldUIWrapper.paintTile(renderUtil, World.convertOffsetToAxial(new Point(x, y)));
                }
            }
            worldUIWrapper.paintEntities(renderUtil,
                                                   World.convertOffsetToAxial(new Point(Math.max(0, xMin), Math.max(0, yMin))),
                                                   World.convertOffsetToAxial(new Point(Math.min(worldUIWrapper.getWorld().getSizeX(), xMax), Math.min(worldUIWrapper.getWorld().getSizeY(), yMax))));
        }
        catch(NoninvertibleTransformException e)
        {
            Logger.log(Logger.LogLevel.error, new String[]{"Camera has entered an invalid state, resetting.", "Camera state: " + worldTransform.toString()});
            e.printStackTrace();
            //TODO: save current worldTransform for error reporting
            worldTransform.setToIdentity();
        }
    }

    /**
     * Paint anything that is positioned relative to the screen (probably exclusively UI buttons and menus)
     * @param render
     * @param hasTurnEnded
     */
    public static void paintScreen(RenderUtil render, boolean hasTurnEnded)
    {
        uiStack.paint(render, hasTurnEnded);
        if(Globals.debug.getDetailedInfoLevel() > 2)
        {
            render.drawString("mouseX:" + mouseLocation.x, 50, 50, new Rectangle(Proto.getScreenWidth(), Proto.getScreenHeight()));
            render.drawString("mouseY:" + mouseLocation.y, 50, 65, new Rectangle(Proto.getScreenWidth(), Proto.getScreenHeight()));
        }
    }

    public void doWork(BigDecimal timeAdjustment, boolean paused)
    {
        //TODO: this doesn't feel quite right, experiment with different math. maybe 2 stages of constant speed?
        if(isScrollEnabled && !paused)
        {
            int xTranslation = 0, yTranslation = 0;
            if (mouseLocation.x < edgeScrollArea &&
                getScreenLocationX() / worldTransform.getScaleX() <
                Game.getWorld(currentWorldIndex).getTileWidth()/2)
            {
                //Logger.log(LogLevel.info, new String[] {"moving left"});
                xTranslation = timeAdjustment.multiply(new BigDecimal(10)).intValue();
            }
            else if (mouseLocation.x >= Proto.getScreenWidth() - edgeScrollArea &&
                     getScreenLocationX() / worldTransform.getScaleX() >
                     Proto.getScreenWidth() - (Game.getWorld(currentWorldIndex).getTileWidth() * (Game.getWorld(currentWorldIndex).getWorld().getSizeX() + 1)))
            {
                //Logger.log(LogLevel.info, new String[] {"moving right"});
                xTranslation = -timeAdjustment.multiply(new BigDecimal(10)).intValue();
            }
            if (mouseLocation.y < edgeScrollArea &&
                getScreenLocationY() / worldTransform.getScaleY() <
                Game.getWorld(currentWorldIndex).getTileHeight()/2)
            {
                //Logger.log(LogLevel.info, new String[] {"moving up"});
                yTranslation = timeAdjustment.multiply(new BigDecimal(10)).intValue();
            }
            else if (mouseLocation.y >= Proto.getScreenHeight() - edgeScrollArea &&
                     getScreenLocationY() / worldTransform.getScaleY() >
                     Proto.getScreenHeight() - ((3 * Game.getWorld(currentWorldIndex).getTileHeight() / 4) * (Game.getWorld
                    (currentWorldIndex).getWorld().getSizeY() + 1)))
            {
                //Logger.log(LogLevel.info, new String[] {"moving down"});
                yTranslation = -timeAdjustment.multiply(new BigDecimal(10)).intValue();
            }
            //TODO: maybe broken, debug this
//            double rotation = Math.atan2(worldTransform.getShearY(), worldTransform.getScaleY());
//            double cos = Math.cos(rotation);
//            double sin = Math.sin(rotation);
//            worldTransform.translate(xTranslation * cos - yTranslation * sin,
//                                     xTranslation * sin + yTranslation * cos);
            worldTransform.translate(xTranslation, yTranslation);
            mouseMoved(new MouseEvent(Proto.getPROTO(), MouseEvent.MOUSE_MOVED, System.nanoTime(), 0, mouseLocation.x, mouseLocation.y, 0, false));
        }
    }

    @Override
    public void keyReleased (KeyEvent paramKeyEvent){}
    
    @Override
    public void keyPressed(KeyEvent paramKeyEvent)
    {
        switch (paramKeyEvent.getKeyCode())
        {
            case KeyEvent.VK_B:
                if(selectedLocation != null)
                {
                    //selectedLocation.queueAction(new ActionBuild(null));
                    Logger.log(Logger.LogLevel.debug, new String[]{"build temporarily disabled"});
                }
                break;
            case KeyEvent.VK_PAUSE:
                Game.paused = !Game.paused;
                if(Game.paused)
                {
                    Logger.log(Logger.LogLevel.debug, new String[]{"Game Paused."});
                }
                else
                {
                    Logger.log(Logger.LogLevel.debug, new String[]{"Game Resumed."});
                }
                break;
            case KeyEvent.VK_ESCAPE:
                Logger.log(Logger.LogLevel.debug, new String[]{"escape"});
                //TODO: a proper 'quit' method to save, prompt user etc
                System.exit(0);
                break;
            case KeyEvent.VK_A:
                Logger.log(Logger.LogLevel.debug, new String[]{"Creating test menu"});
                uiStack.push(UIStack.getNewTestMenu());
                break;
            case KeyEvent.VK_SPACE:
                isScrollEnabled = !isScrollEnabled;
                if(isScrollEnabled)
                {
                    Logger.log(Logger.LogLevel.debug, new String[]{"Camera pan enabled"});
                }
                else
                {
                    Logger.log(Logger.LogLevel.debug, new String[]{"Camera pan disabled"});
                }
                break;
            case KeyEvent.VK_R:
                Logger.log(Logger.LogLevel.info, new String[]{"Reloading all resource files"});
                ResourceManager.resetLoadedImages();
                break;
            case KeyEvent.VK_ENTER:
                Game.endTurn();
                break;
            case KeyEvent.VK_P:
                worldTransform.translate(Proto.getScreenWidth() / 2, Proto.getScreenHeight() / 2);
                worldTransform.rotate(Math.PI/16);
                worldTransform.translate(-Proto.getScreenWidth() / 2, -Proto.getScreenHeight() / 2);
                break;
            case KeyEvent.VK_UP:
                worldTransform.translate(0, -1);
                break;
            case KeyEvent.VK_DOWN:
                worldTransform.translate(0, 1);
                break;
            case KeyEvent.VK_LEFT:
                worldTransform.translate(-1, 0);
                break;
            case KeyEvent.VK_RIGHT:
                worldTransform.translate(1, 0);
                break;
            case KeyEvent.VK_SLASH:
                Logger.log(Logger.LogLevel.debug, new String[]{"x:" + worldTransform.getTranslateX(),"y:" + worldTransform.getTranslateY()});
                break;
        }
    }

    @Override
    public void keyTyped(KeyEvent paramKeyEvent){}
    
    @Override
    public void mousePressed(MouseEvent paramMouseEvent)
    {
        boolean isUIEventHandled = uiStack.mousePressed(paramMouseEvent) != null;

        //if this event was intercepted by overlaid UI elements
        if(!isUIEventHandled)
        {
            //Convert the mouse coordinates from screen to world coordinates
            transformMouseEvent(paramMouseEvent, worldTransform);

            Game.getWorld(currentWorldIndex).mousePressed(paramMouseEvent);
        }
    }
    
    @Override
    public void mouseDragged(MouseEvent paramMouseEvent)
    {
        //See doWork() for usage, store coords for camera pan
        mouseLocation = paramMouseEvent.getPoint();

        //if this event was intercepted by overlaid UI elements
        boolean isUIEventHandled = uiStack.mouseDragged(paramMouseEvent) != null;

        if(!isUIEventHandled)
        {
            //Convert the mouse coordinates from screen to world coordinates
            transformMouseEvent(paramMouseEvent, worldTransform);

            Game.getWorld(currentWorldIndex).mouseDragged(paramMouseEvent);
        }
    }

    @Override
    public void mouseMoved(MouseEvent paramMouseEvent)
    {
        //See doWork() for usage, store coords for camera pan
        mouseLocation = paramMouseEvent.getPoint();

        //if this event was intercepted by overlaid UI elements
        boolean isUIEventHandled = uiStack.mouseMoved(paramMouseEvent) != null;

        if(!isUIEventHandled)
        {
            //Convert the mouse coordinates from screen to world coordinates
            transformMouseEvent(paramMouseEvent, worldTransform);

            Game.getWorld(currentWorldIndex).mouseMoved(paramMouseEvent);
        }
    }

    @Override
    public void mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent)
    {
        //if this event was intercepted by overlaid UI elements
        boolean isUIEventHandled = uiStack.mouseWheelMoved(paramMouseWheelEvent) != null;
        if(!isUIEventHandled)
        {
            double scaleFactor = paramMouseWheelEvent.getWheelRotation() > 0.0 ? 0.9 * paramMouseWheelEvent.getWheelRotation() : (10.0 / 9.0) * -paramMouseWheelEvent
                .getPreciseWheelRotation();

            if(((worldTransform.getScaleX() < 2) && (worldTransform.getScaleY() < 2) && (scaleFactor > 1)) ||
               ((worldTransform.getScaleX() > 0.5) && (worldTransform.getScaleY() > 0.5) && (scaleFactor < 1)))
            {
                worldTransform.translate(paramMouseWheelEvent.getX(), paramMouseWheelEvent.getY());
                worldTransform.scale(scaleFactor, scaleFactor);
                worldTransform.translate(-paramMouseWheelEvent.getX(), -paramMouseWheelEvent.getY());
            }
            //Convert the mouse coordinates from screen to world coordinates
            transformMouseEvent(paramMouseWheelEvent, worldTransform);
            Game.getWorld(currentWorldIndex).mouseWheelMoved(paramMouseWheelEvent);
        }
    }

    @Override //Do not use, partially broken implementation. Moving the mouse between press and release will prevent the event firing
    public void mouseClicked(MouseEvent paramMouseEvent){}

    @Override
    public void mouseReleased(MouseEvent paramMouseEvent)
    {
        //if this event was intercepted by overlaid UI elements
        boolean isUIEventHandled = uiStack.mouseReleased(paramMouseEvent) != null;

        if(!isUIEventHandled)
        {
            //Convert the mouse coordinates from screen to world coordinates
            transformMouseEvent(paramMouseEvent, worldTransform);

            Point releasedLocation = Game.getWorld(currentWorldIndex).mouseReleased(paramMouseEvent);

            //if left click
            if (paramMouseEvent.getButton() == MouseEvent.BUTTON1)
            {
                selectedLocation = releasedLocation;
            }
            //else if right click
            else if (paramMouseEvent.getButton() == MouseEvent.BUTTON3)
            {
                //TODO: refactor? to allow drag for target orientation
                Entity selectedEntity = Game.getWorld(currentWorldIndex).getWorld().getEntityAtIndex(selectedLocation);
                Action action = new ActionMoveTestImpl(selectedEntity, user, releasedLocation);
                if(action.isValid())
                {
                    user.queueAction(action);
                    try
                    {
                        Game.getClientUtil().sendToServer(action.serializeForPlayer(user));
                    }
                    catch(IOException e)
                    {
                        e.printStackTrace();
                    }
                }
                else
                {
                    Logger.log(Logger.LogLevel.debug, new String[]{"Invalid action:", action.serializeForPlayer(user).toString()});
                }
            }
        }
    }

    @Override //Not relevant with a custom UI stack
    public void mouseEntered (MouseEvent paramMouseEvent) {}

    @Override//Not relevant with a custom UI stack
    public void mouseExited(MouseEvent paramMouseEvent) {}

    private static void transformMouseEvent(MouseEvent event, AffineTransform transform)
    {
        Point point = new Point(event.getX(), event.getY());
        try
        {
            transform.createInverse().transform(point, point);
        }
        catch(NoninvertibleTransformException e)
        {
            e.printStackTrace();
        }
        event.translatePoint((int) (point.getX() - event.getX()), (int) point.getY() - event.getY());
    }

    public static AffineTransform getWorldTransform()
    {
        return worldTransform;
    }
}
