package atraxi.client.ui;

import atraxi.client.Game;
import atraxi.client.Proto;
import atraxi.client.ui.wrappers.WorldUIWrapper;
import atraxi.client.util.RenderUtil;
import atraxi.client.util.ResourceManager;
import atraxi.core.Player;
import atraxi.core.entities.action.ActionMoveTestImpl;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;

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
    private static WorldUIWrapper.GridTileUIWrapper selectedTile;
    private static Player user;
    private static final Globals.Identifiers[] mapImages = {
            Globals.Identifiers.background1A,Globals.Identifiers.background1B,Globals.Identifiers.background1C,Globals.Identifiers.background1D,
            Globals.Identifiers.background2A,Globals.Identifiers.background2B,Globals.Identifiers.background2C,Globals.Identifiers.background2D,
            Globals.Identifiers.background3A,Globals.Identifiers.background3B,Globals.Identifiers.background3C,Globals.Identifiers.background3D,
            Globals.Identifiers.background4A,Globals.Identifiers.background4B,Globals.Identifiers.background4C,Globals.Identifiers.background4D};
    private static final int edgeScrollArea = 50;
    private static int currentWorldIndex;

    //variables related to edge scrolling
    private static AffineTransform worldTransform = new AffineTransform();
    //initial value is mostly irrelevant, and will be set properly the instant the mouse is moved
    private static Point mouseLocation = new Point(200, 200);

    public static UIStack uiStack = new UIStack();
    private static boolean isScrollEnabled = false;

    //Used in paintBackground(), avoids recreating an instance ~100 times a frame
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
     * @param g2d
     */
    public void paintBackground(Graphics2D g2d)
    {
        //Good luck anyone reading this. It creates the illusion of parallax for the scrolling backdrop
        //I (mostly) understood it while writing, but I have no idea how to explain the logic behind this math
        //At it's core it determines the top/left most position for a background tile where it will still be visible (most complex part, loop variable initialization)
        //then iterates across the screen until reaching the point where the image will no longer be visible

        //Also picks a random background image to draw from a selection, seeded buy the x,y index of the image in the background
        //the image index must be calculated as needed, due to the images not being in an indexed list that corresponds to the location they are drawn

        int mapImageWidth = ResourceManager.getImage(mapImages[0]).getWidth(null);
        int mapImageHeight = ResourceManager.getImage(mapImages[0]).getHeight(null);
        int indexX;
        int indexY;
        for(double backgroundOffsetX = (getScreenLocationX() % mapImageWidth) - (getScreenLocationX()>0?mapImageWidth:0);
            backgroundOffsetX <= Proto.getScreenWidth();
            backgroundOffsetX += mapImageWidth)
        {
            for(double backgroundOffsetY = (getScreenLocationY() % mapImageHeight) - (getScreenLocationY()>0?mapImageHeight:0);
                backgroundOffsetY <= Proto.getScreenHeight();
                backgroundOffsetY += mapImageHeight)
            {
                indexX = (int)((backgroundOffsetX-getScreenLocationX()+10)/mapImageWidth);
                indexY = (int)((backgroundOffsetY-getScreenLocationY()+10)/mapImageHeight);
                rand.setSeed((1234*indexX) ^ (5678*indexY) ^ Globals.SEED);
                g2d.drawImage(ResourceManager.getImage(mapImages[rand.nextInt(4)]), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
                if(Globals.debug.getDetailedInfoLevel() >= 4)
                {
                    g2d.scale(4, 4);
                    g2d.drawString("x:" + indexX + " y:" + indexY,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 20);
                    /*g2d.drawString("B:" + (int)backgroundOffsetX + " s:" + (int)getScreenLocationX(),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 40);
                    g2d.drawString("dif:" + (int)(backgroundOffsetX-getScreenLocationX()),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 60);*/
                    g2d.scale(0.25, 0.25);
                }
            }
        }
        for(double backgroundOffsetX = (getScreenLocationX()/2 % mapImageWidth) - (getScreenLocationX()>0?mapImageWidth:0);
            backgroundOffsetX <= Proto.getScreenWidth();
            backgroundOffsetX += mapImageWidth)
        {
            for(double backgroundOffsetY = (getScreenLocationY()/2 % mapImageHeight) - (getScreenLocationY()>0?mapImageHeight:0);
                backgroundOffsetY <= Proto.getScreenHeight();
                backgroundOffsetY += mapImageHeight)
            {
                indexX = (int)((backgroundOffsetX-(getScreenLocationX()/2)+10)/mapImageWidth);
                indexY = (int)((backgroundOffsetY-(getScreenLocationY()/2)+10)/mapImageHeight);
                rand.setSeed((1234*indexX) ^ (5678*indexY) ^ Globals.SEED*2);
                g2d.drawImage(ResourceManager.getImage(mapImages[rand.nextInt(4)+4]), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
                if(Globals.debug.getDetailedInfoLevel() >= 4)
                {
                    g2d.scale(4, 4);
                    g2d.drawString("x:" + indexX + " y:" + indexY,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 20);
                    /*g2d.drawString("B:" + (int)backgroundOffsetX + " s:" + (int)getScreenLocationX(),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 40);
                    g2d.drawString("dif:" + (int)(backgroundOffsetX-getScreenLocationX()),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 60);*/
                    g2d.scale(0.25, 0.25);
                }
            }
        }

        for(double backgroundOffsetX = (getScreenLocationX()/3 % mapImageWidth) - (getScreenLocationX()>0?mapImageWidth:0);
            backgroundOffsetX <= Proto.getScreenWidth();
            backgroundOffsetX += mapImageWidth)
        {
            for(double backgroundOffsetY = (getScreenLocationY()/3 % mapImageHeight) - (getScreenLocationY()>0?mapImageHeight:0);
                backgroundOffsetY <= Proto.getScreenHeight();
                backgroundOffsetY += mapImageHeight)
            {
                indexX = (int)((backgroundOffsetX-(getScreenLocationX()/3)+10)/mapImageWidth);
                indexY = (int)((backgroundOffsetY-(getScreenLocationY()/3)+10)/mapImageHeight);
                rand.setSeed((1234*indexX) ^ (5678*indexY) ^ Globals.SEED*3);
                g2d.drawImage(ResourceManager.getImage(mapImages[rand.nextInt(4)+8]), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
                if(Globals.debug.getDetailedInfoLevel() >= 4)
                {
                    g2d.scale(4, 4);
                    g2d.drawString("x:" + indexX + " y:" + indexY,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 20);
                    /*g2d.drawString("B:" + (int)backgroundOffsetX + " s:" + (int)getScreenLocationX(),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 40);
                    g2d.drawString("dif:" + (int)(backgroundOffsetX-getScreenLocationX()),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 60);*/
                    g2d.scale(0.25, 0.25);
                }
            }
        }

        for(double backgroundOffsetX = (getScreenLocationX()/5 % mapImageWidth) - (getScreenLocationX()>0?mapImageWidth:0);
            backgroundOffsetX <= Proto.getScreenWidth();
            backgroundOffsetX += mapImageWidth)
        {
            for(double backgroundOffsetY = (getScreenLocationY()/5 % mapImageHeight) - (getScreenLocationY()>0?mapImageHeight:0);
                backgroundOffsetY <= Proto.getScreenHeight();
                backgroundOffsetY += mapImageHeight)
            {
                indexX = (int)((backgroundOffsetX-(getScreenLocationX()/5)+10)/mapImageWidth);
                indexY = (int)((backgroundOffsetY-(getScreenLocationY()/5)+10)/mapImageHeight);
                rand.setSeed((1234*indexX) ^ (5678*indexY) ^ Globals.SEED*5);
                g2d.drawImage(ResourceManager.getImage(mapImages[rand.nextInt(4)+12]), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
                if(Globals.debug.getDetailedInfoLevel() >= 4)
                {
                    g2d.scale(4, 4);
                    g2d.drawString("x:" + indexX + " y:" + indexY,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 20);
                    /*g2d.drawString("B:" + (int)backgroundOffsetX + " s:" + (int)getScreenLocationX(),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 40);
                    g2d.drawString("dif:" + (int)(backgroundOffsetX-getScreenLocationX()),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 60);*/
                    g2d.scale(0.25, 0.25);
                }
            }
        }
    }

    /**
     * Paint anything that moves relative to objects within the game world (ships, planets, etc)
     * @param renderUtil
     */
public static void paintWorld(RenderUtil renderUtil, boolean hasTurnEnded)
    {
        Game.getWorld(currentWorldIndex).paint(renderUtil, hasTurnEnded);
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
            render.drawString("mouseY:" + mouseLocation.y, 50, 60, new Rectangle(Proto.getScreenWidth(), Proto.getScreenHeight()));
        }
    }

    public static void doWork(BigDecimal timeAdjustment, boolean paused)
    {
        //TODO: this doesn't feel quite right, experiment with different math. maybe 2 stages of constant speed?
        //TODO: scroll down is slower than up, why? fix it
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
                     Proto.getScreenWidth() - (Game.getWorld(currentWorldIndex).getTileWidth() * (Game.getWorld(currentWorldIndex).getSizeX() + 1)))
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
                    (currentWorldIndex).getSizeY() + 1)))
            {
                //Logger.log(LogLevel.info, new String[] {"moving down"});
                yTranslation = -timeAdjustment.multiply(new BigDecimal(10)).intValue();
            }
            worldTransform.translate(xTranslation, yTranslation);
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
                if(selectedTile != null)
                {
                    //selectedTile.queueAction(new ActionBuild(null));
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
//            case KeyEvent.VK_0:
//                Logger.log(LogLevel.debug, new String[]{"Starting multiplayer atraxi.server thread"});
//                serverUtil = new ServerUtil(2);
//                Thread serverThread = new Thread(serverUtil);
//                serverThread.setName("Server");
//                serverThread.start();
//                break;
//            case KeyEvent.VK_9:
//                Logger.log(LogLevel.debug, new String[]{"Starting multiplayer atraxi.client thread"});
//                try
//                {
//                    clientUtil = new ClientUtil(user);
//                    Thread clientThread = new Thread(clientUtil);
//                    clientThread.setName("Client");
//                    clientThread.start();
//                }
//                catch(ConnectException e)
//                {
//                    Logger.log(LogLevel.debug, new String[]{"Failed to connect to the atraxi.server"});
//                    e.printStackTrace();
//                }
//                catch(IOException e)
//                {
//                    Logger.log(LogLevel.debug, new String[]{"error"});
//                    e.printStackTrace();
//                }
//                break;
//            case KeyEvent.VK_8:
//                Logger.log(LogLevel.debug, new String[]{"Attempt sending atraxi.client->atraxi.server"});
//                try
//                {
//                    JSONObject jsonObject = new JSONObject();
//                    jsonObject.append("message", "atraxi.client->atraxi.server test");
//                    clientUtil.sendToServer(jsonObject);
//                }
//                catch(IOException e)
//                {
//                    e.printStackTrace();
//                }
//                break;
//            case KeyEvent.VK_7:
//                Logger.log(LogLevel.debug, new String[]{"Attempt sending atraxi.server->atraxi.client"});
//                JSONObject jsonObject = new JSONObject();
//                jsonObject.append("message", "atraxi.server->atraxi.client test");
//                serverUtil.sendToPlayer(user, jsonObject);
//                break;
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
        boolean isUIEventHandled = uiStack.mouseWheelMoved(paramMouseWheelEvent) != null;
        //if this event was intercepted by overlaid UI elements
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
        boolean isUIEventHandled = uiStack.mouseReleased(paramMouseEvent) != null;

        //if this event was intercepted by overlaid UI elements
        if(!isUIEventHandled)
        {
            //Convert the mouse coordinates from screen to world coordinates
            transformMouseEvent(paramMouseEvent, worldTransform);

            WorldUIWrapper.GridTileUIWrapper releasedTile = (WorldUIWrapper.GridTileUIWrapper) Game.getWorld(currentWorldIndex).mouseReleased(paramMouseEvent);

            //if left click
            if (paramMouseEvent.getButton() == MouseEvent.BUTTON1)
            {
                selectedTile = releasedTile;
            }
            //else if right click
            else if (paramMouseEvent.getButton() == MouseEvent.BUTTON3)
            {
                //TODO: refactor? to allow drag for target orientation
                Action action = new ActionMoveTestImpl(selectedTile.getEntity(), releasedTile);//TODO: change to (selectedTile.getEntity(), ***TARGET***Tile)
                if(action.isValid())
                {
                    user.queueAction(action);
                    try
                    {
                        Game.getClientUtil().sendToServer(action.toJSON());
                    }
                    catch(IOException e)
                    {
                        e.printStackTrace();
                    }
                }
                else { Logger.log(Logger.LogLevel.debug, new String[]{"Invalid action"}); }
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
