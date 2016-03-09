package atraxi.ui;

import atraxi.entities.actionQueue.Action;
import atraxi.game.Game;
import atraxi.game.Player;
import atraxi.game.Proto;
import atraxi.game.world.World;
import atraxi.util.CheckedRender;
import atraxi.util.Logger;
import atraxi.util.Logger.LogLevel;
import atraxi.util.ResourceManager;
import atraxi.util.ResourceManager.ImageID;

import java.awt.Graphics2D;
import java.awt.event.KeyEvent;
import java.awt.event.KeyListener;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.awt.event.MouseMotionListener;
import java.awt.event.MouseWheelEvent;
import java.awt.event.MouseWheelListener;
import java.math.BigDecimal;
import java.util.Random;

public class UserInterfaceHandler implements KeyListener, MouseListener, MouseWheelListener, MouseMotionListener
{
    private static World.GridTile selectedTile;
    private static Player user;
    private static final ResourceManager.ImageID[] mapImages = {ImageID.background1A,ImageID.background1B,ImageID.background1C,ImageID.background1D,
                                                                ImageID.background2A,ImageID.background2B,ImageID.background2C,ImageID.background2D,
                                                                ImageID.background3A,ImageID.background3B,ImageID.background3C,ImageID.background3D,
                                                                ImageID.background4A,ImageID.background4B,ImageID.background4C,ImageID.background4D};
    private static int currentWorldIndex;

    //variables related to edge scrolling
    private static int screenLocationX = 0, screenLocationY = 0;
    //initial value is mostly irrelevant, and will be set properly the instant the mouse is moved
    private static int mouseX = 200, mouseY = 200;

    public static UIStack uiStack;
    
    public UserInterfaceHandler(Player user, int defaultWorldIndex)
    {
        UserInterfaceHandler.user = user;
        uiStack = new UIStack();
        currentWorldIndex = defaultWorldIndex;
    }

    public static int getScreenLocationX ()
    {
        return screenLocationX;
    }

    public static int getScreenLocationY ()
    {
        return screenLocationY;
    }

    public static int getCurrentWorldIndex()
    {
        return currentWorldIndex;
    }

    public static void setSelectedTile(World.GridTile selectedTile)
    {
        UserInterfaceHandler.selectedTile = selectedTile;
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

        int mapImageWidth = mapImages[0].getImage().getWidth(null);
        int mapImageHeight = mapImages[0].getImage().getHeight(null);
        int indexX;
        int indexY;
        for(double backgroundOffsetX = (screenLocationX % mapImageWidth) - (screenLocationX>0?mapImageWidth:0);
            backgroundOffsetX <= Proto.screen_Width;
            backgroundOffsetX += mapImageWidth)
        {
            for(double backgroundOffsetY = (screenLocationY % mapImageHeight) - (screenLocationY>0?mapImageHeight:0);
                backgroundOffsetY <= Proto.screen_Height;
                backgroundOffsetY += mapImageHeight)
            {
                indexX = (int)((backgroundOffsetX-screenLocationX+10)/mapImageWidth);
                indexY = (int)((backgroundOffsetY-screenLocationY+10)/mapImageHeight);
                Random rand = new Random((1234*indexX) ^ (5678*indexY) ^ Proto.SEED);
                g2d.drawImage(mapImages[rand.nextInt(4)].getImage(), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
                if(Proto.debug)
                {
                    g2d.scale(4, 4);
                    g2d.drawString("x:" + indexX + " y:" + indexY,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 20);
                    /*g2d.drawString("B:" + (int)backgroundOffsetX + " s:" + (int)screenLocationX,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 40);
                    g2d.drawString("dif:" + (int)(backgroundOffsetX-screenLocationX),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 60);*/
                    g2d.scale(0.25, 0.25);
                }
            }
        }
        for(double backgroundOffsetX = (screenLocationX/2 % mapImageWidth) - (screenLocationX>0?mapImageWidth:0);
            backgroundOffsetX <= Proto.screen_Width;
            backgroundOffsetX += mapImageWidth)
        {
            for(double backgroundOffsetY = (screenLocationY/2 % mapImageHeight) - (screenLocationY>0?mapImageHeight:0);
                backgroundOffsetY <= Proto.screen_Height;
                backgroundOffsetY += mapImageHeight)
            {
                indexX = (int)((backgroundOffsetX-(screenLocationX/2)+10)/mapImageWidth);
                indexY = (int)((backgroundOffsetY-(screenLocationY/2)+10)/mapImageHeight);
                Random rand = new Random((1234*indexX) ^ (5678*indexY) ^ Proto.SEED*2);
                g2d.drawImage(mapImages[rand.nextInt(4)+4].getImage(), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
                if(Proto.debug)
                {
                    g2d.scale(4, 4);
                    g2d.drawString("x:" + indexX + " y:" + indexY,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 20);
                    /*g2d.drawString("B:" + (int)backgroundOffsetX + " s:" + (int)screenLocationX,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 40);
                    g2d.drawString("dif:" + (int)(backgroundOffsetX-screenLocationX),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 60);*/
                    g2d.scale(0.25, 0.25);
                }
            }
        }

        for(double backgroundOffsetX = (screenLocationX/3 % mapImageWidth) - (screenLocationX>0?mapImageWidth:0);
            backgroundOffsetX <= Proto.screen_Width;
            backgroundOffsetX += mapImageWidth)
        {
            for(double backgroundOffsetY = (screenLocationY/3 % mapImageHeight) - (screenLocationY>0?mapImageHeight:0);
                backgroundOffsetY <= Proto.screen_Height;
                backgroundOffsetY += mapImageHeight)
            {
                indexX = (int)((backgroundOffsetX-(screenLocationX/3)+10)/mapImageWidth);
                indexY = (int)((backgroundOffsetY-(screenLocationY/3)+10)/mapImageHeight);
                Random rand = new Random((1234*indexX) ^ (5678*indexY) ^ Proto.SEED*3);
                g2d.drawImage(mapImages[rand.nextInt(4)+8].getImage(), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
                if(Proto.debug)
                {
                    g2d.scale(4, 4);
                    g2d.drawString("x:" + indexX + " y:" + indexY,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 20);
                    /*g2d.drawString("B:" + (int)backgroundOffsetX + " s:" + (int)screenLocationX,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 40);
                    g2d.drawString("dif:" + (int)(backgroundOffsetX-screenLocationX),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 60);*/
                    g2d.scale(0.25, 0.25);
                }
            }
        }

        for(double backgroundOffsetX = (screenLocationX/5 % mapImageWidth) - (screenLocationX>0?mapImageWidth:0);
            backgroundOffsetX <= Proto.screen_Width;
            backgroundOffsetX += mapImageWidth)
        {
            for(double backgroundOffsetY = (screenLocationY/5 % mapImageHeight) - (screenLocationY>0?mapImageHeight:0);
                backgroundOffsetY <= Proto.screen_Height;
                backgroundOffsetY += mapImageHeight)
            {
                indexX = (int)((backgroundOffsetX-(screenLocationX/5)+10)/mapImageWidth);
                indexY = (int)((backgroundOffsetY-(screenLocationY/5)+10)/mapImageHeight);
                Random rand = new Random((1234*indexX) ^ (5678*indexY) ^ Proto.SEED*5);
                g2d.drawImage(mapImages[rand.nextInt(4)+12].getImage(), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
                if(Proto.debug)
                {
                    g2d.scale(4, 4);
                    g2d.drawString("x:" + indexX + " y:" + indexY,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 20);
                    /*g2d.drawString("B:" + (int)backgroundOffsetX + " s:" + (int)screenLocationX,
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 40);
                    g2d.drawString("dif:" + (int)(backgroundOffsetX-screenLocationX),
                                   (int) backgroundOffsetX/4 + 20,
                                   (int) backgroundOffsetY/4 + 60);*/
                    g2d.scale(0.25, 0.25);
                }
            }
        }
    }

    /**
     * Paint anything that moves relative to objects within the game world (ships, planets, etc)
     * @param checkedRender
     */
    public void paintWorld(CheckedRender checkedRender)
    {
        Game.getWorld(currentWorldIndex).paint(checkedRender);
    }

    /**
     * Paint anything that is positioned relative to the screen (probably exclusively UI buttons and menus)
     * @param render
     */
    public void paintScreen(CheckedRender render)
    {
        uiStack.paint(render);
    }

    public void doWork (BigDecimal timeAdjustment, boolean paused)
    {
        //TODO: this doesn't feel quite right, experiment with different math. maybe 2 stages of constant speed?
        if(mouseX < 100 && screenLocationX< Game.getWorld(currentWorldIndex).getSizeX() * Game.getWorld(currentWorldIndex).getGridSize() / 2)
        {
            screenLocationX += timeAdjustment.multiply(new BigDecimal((100 - mouseX) / 10)).doubleValue();
        }
        else if(mouseX >= Proto.screen_Width - 100 && screenLocationX > -Game.getWorld(currentWorldIndex).getSizeX() * Game.getWorld(currentWorldIndex).getGridSize() / 2)
        {
            screenLocationX -= timeAdjustment.multiply(new BigDecimal((100 - Proto.screen_Width + mouseX) / 10)).doubleValue();
        }
        if(mouseY < 100 && screenLocationY< Game.getWorld(currentWorldIndex).getSizeY() * Game.getWorld(currentWorldIndex).getGridSize() / 2)
        {
            screenLocationY += timeAdjustment.multiply(new BigDecimal((100 - mouseY) / 10)).doubleValue();
        }
        else if(mouseY >= Proto.screen_Height - 100 && screenLocationY > -Game.getWorld(currentWorldIndex).getSizeY() * Game.getWorld(currentWorldIndex).getGridSize() / 2)
        {
            screenLocationY -= timeAdjustment.multiply(new BigDecimal((100 - Proto.screen_Height + mouseY) / 10)).doubleValue();
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
                selectedTile.queueAction(new Action(Action.ActionType.BUILD, null));
                break;
            case KeyEvent.VK_PAUSE:
                Game.paused=!Game.paused;
                if(Game.paused)
                {
                    Logger.log(LogLevel.debug, new String[] {"Game Paused."});
                }
                else
                {
                    Logger.log(LogLevel.debug, new String[] {"Game Resumed."});
                }
                break;
            case KeyEvent.VK_ESCAPE:
                Logger.log(LogLevel.debug, new String[] {"escape"});
                System.exit(0);
                break;
            case KeyEvent.VK_A:
                Logger.log(LogLevel.debug, new String[] {"Creating test menu"});
                uiStack.push(UIStack.getNewTestMenu());
        }
    }

    @Override
    public void keyTyped(KeyEvent paramKeyEvent){}
    
    @Override
    public void mousePressed(MouseEvent paramMouseEvent)
    {
        boolean isUIEventHandled;
        {
            UIElement element = uiStack.mousePressed(paramMouseEvent);
            //GridTile is the default unless the mouse event is over a non-selectable part of the world
            isUIEventHandled = !(element instanceof World.GridTile) && element != null;
        }

        if(!isUIEventHandled)
        {
            Game.getWorld(currentWorldIndex).mousePressed(paramMouseEvent);
        }
    }
    
    @Override
    public void mouseDragged(MouseEvent paramMouseEvent)
    {
        //See doWork() for usage, store coords for camera pan
        mouseX = paramMouseEvent.getX();
        mouseY = paramMouseEvent.getY();

        boolean isUIEventHandled;
        {
            UIElement element = uiStack.mouseDragged(paramMouseEvent);
            //GridTile is the default unless the mouse event is over a non-selectable part of the world
            isUIEventHandled = !(element instanceof World.GridTile) && element != null;
        }

        if(!isUIEventHandled)
        {
            Game.getWorld(currentWorldIndex).mouseDragged(paramMouseEvent);
        }
    }

    @Override
    public void mouseMoved(MouseEvent paramMouseEvent)
    {
        //See doWork() for usage, store coords for camera pan
        mouseX = paramMouseEvent.getX();
        mouseY = paramMouseEvent.getY();

        boolean isUIEventHandled;
        {
            UIElement element = uiStack.mouseMoved(paramMouseEvent);
            //GridTile is the default unless the mouse event is over a non-selectable part of the world
            isUIEventHandled = !(element instanceof World.GridTile) && element != null;
        }

        if(!isUIEventHandled)
        {
            Game.getWorld(currentWorldIndex).mouseMoved(paramMouseEvent);
        }
    }

    @Override
    public void mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent)
    {
        boolean isUIEventHandled;
        {
            UIElement element = uiStack.mouseWheelMoved(paramMouseWheelEvent);
            //GridTile is the default unless the mouse event is over a non-selectable part of the world
            isUIEventHandled = !(element instanceof World.GridTile) && element != null;
        }

        if(!isUIEventHandled)
        {
            Game.getWorld(currentWorldIndex).mouseWheelMoved(paramMouseWheelEvent);
        }
    }

    @Override //Do not use, partially broken implementation. Moving the mouse between press and release will prevent the event firing
    public void mouseClicked(MouseEvent paramMouseEvent){}

    @Override
    public void mouseReleased(MouseEvent paramMouseEvent)
    {
        boolean isUIEventHandled;
        {
            UIElement element = uiStack.mouseReleased(paramMouseEvent);
            //GridTile is the default unless the mouse event is over a non-selectable part of the world
            isUIEventHandled = !(element instanceof World.GridTile) && element != null;
        }
        //if this event wasn't intercepted by overlaid UI elements
        if(!isUIEventHandled)
        {
            //Convert the mouse coordinates from screen to world coordinates
            paramMouseEvent.translatePoint(-(int) screenLocationX, -(int) screenLocationY);

            //if left click
            if (paramMouseEvent.getButton() == MouseEvent.BUTTON1)
            {
                selectedTile = (World.GridTile) Game.getWorld(currentWorldIndex).mouseReleased(paramMouseEvent);
            }
            //else if right click
            else if (paramMouseEvent.getButton() == MouseEvent.BUTTON3)
            {
                //TODO: refactor? to allow drag for target orientation
                if (paramMouseEvent.isShiftDown())
                {
                    selectedTile.queueAction(new Action(Action.ActionType.MOVE, new Object[]{(double) paramMouseEvent.getX(), (double) paramMouseEvent.getY()}));
                }
                else
                {
                    selectedTile.replaceQueue(new Action(Action.ActionType.MOVE, new Object[]{(double) paramMouseEvent.getX(), (double) paramMouseEvent.getY()}));
                }
            }
        }
    }

    @Override //Not relevant with a custom UI stack
    public void mouseEntered (MouseEvent paramMouseEvent) {}

    @Override//Not relevant with a custom UI stack
    public void mouseExited(MouseEvent paramMouseEvent) {}
}
