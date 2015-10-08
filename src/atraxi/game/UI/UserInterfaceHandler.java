package atraxi.game.UI;

import atraxi.game.Game;
import atraxi.game.Player;
import atraxi.game.Proto;
import atraxi.game.ResourceManager;
import atraxi.game.ResourceManager.ImageID;
import entities.actionQueue.Action;

import java.awt.Color;
import java.awt.Graphics2D;
import java.awt.Rectangle;
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
    private Player user;
    private static final ResourceManager.ImageID[] mapImages = {ImageID.background1A,ImageID.background1B,ImageID.background1C,ImageID.background1D,
                                                                ImageID.background2A,ImageID.background2B,ImageID.background2C,ImageID.background2D,
                                                                ImageID.background3A,ImageID.background3B,ImageID.background3C,ImageID.background3D,
                                                                ImageID.background4A,ImageID.background4B,ImageID.background4C,ImageID.background4D};
    private int currentWorld = 0;

    //variables related to selecting a group of units
    private Rectangle selectionArea = null;
    private int dragSelectStartX, dragSelectEndX, dragSelectStartY, dragSelectEndY;
    private boolean dragSelect;

    //variables related to edge scrolling
    private static double screenLocationX = 0, screenLocationY = 0;
    private static int mouseX = 200, mouseY = 200;

    public static UIStack uiStack;
    
    public UserInterfaceHandler(Player user)
    {
        this.user = user;
        uiStack = new UIStack();
    }

    public static double getScreenLocationX ()
    {
        return screenLocationX;
    }

    public static double getScreenLocationY ()
    {
        return screenLocationY;
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
                Random rand = new Random((1234*indexX) ^ (5678*indexY) ^ Game.SEED);
                g2d.drawImage(ResourceManager.getImage(mapImages[rand.nextInt(4)]), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
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
                Random rand = new Random((1234*indexX) ^ (5678*indexY) ^ Game.SEED*2);
                g2d.drawImage(ResourceManager.getImage(mapImages[rand.nextInt(4)+4]), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
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
                Random rand = new Random((1234*indexX) ^ (5678*indexY) ^ Game.SEED*3);
                g2d.drawImage(ResourceManager.getImage(mapImages[rand.nextInt(4)+8]), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
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
                Random rand = new Random((1234*indexX) ^ (5678*indexY) ^ Game.SEED*5);
                g2d.drawImage(ResourceManager.getImage(mapImages[rand.nextInt(4)+12]), (int) backgroundOffsetX, (int) backgroundOffsetY, null);
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
     * @param g2d
     */
    public void paintWorld(Graphics2D g2d)
    {
        if(selectionArea!=null)
        {
            Color color = g2d.getColor();
            g2d.setColor(Color.WHITE);
            g2d.drawRect(selectionArea.x, selectionArea.y, selectionArea.width, selectionArea.height);
            g2d.setColor(color);
        }
    }

    /**
     * Paint anything that is positioned relative to the screen (probably exclusively UI buttons and menus)
     * @param g2d
     */
    public void paintScreen(Graphics2D g2d)
    {
        uiStack.paint(g2d);
    }

    public void doWork (BigDecimal timeAdjustment, boolean paused)
    {
        //TODO: this doesn't feel quite right, experiment with different math. maybe 2 stages of constant speed?
        if(mouseX < 100 && screenLocationX<Game.getWorld(currentWorld).getSizeX()/2)
        {
            screenLocationX += timeAdjustment.multiply(new BigDecimal((100 - mouseX) / 10)).doubleValue();
        }
        else if(mouseX > Proto.screen_Width - 100 && screenLocationX>-Game.getWorld(currentWorld).getSizeX()/2)
        {
            screenLocationX -= timeAdjustment.multiply(new BigDecimal((100 - Proto.screen_Width + mouseX) / 10)).doubleValue();
        }
        if(mouseY < 100 && screenLocationY<Game.getWorld(currentWorld).getSizeY()/2)
        {
            screenLocationY += timeAdjustment.multiply(new BigDecimal((100 - mouseY) / 10)).doubleValue();
        }
        else if(mouseY > Proto.screen_Height - 100 && screenLocationY>-Game.getWorld(currentWorld).getSizeY()/2)
        {
            screenLocationY -= timeAdjustment.multiply(new BigDecimal((100 - Proto.screen_Height + mouseY) / 10)).doubleValue();
        }
    }

    @Override
    public void keyReleased(KeyEvent paramKeyEvent){}
    
    @Override
    public void keyPressed(KeyEvent paramKeyEvent)
    {
        switch (paramKeyEvent.getKeyCode())
        {
            case KeyEvent.VK_B:
                user.queueAction(new Action(Action.ActionType.BUILD, null));
                break;
            case KeyEvent.VK_PAUSE:
                Game.paused=!Game.paused;
                if(Game.paused)
                {
                    System.out.println("Game Paused.");
                }
                else
                {
                    System.out.println("Game Resumed.");
                }
                break;
            case KeyEvent.VK_ESCAPE:
                System.out.println("escape");
                System.exit(0);
                break;
            case KeyEvent.VK_A:
                System.out.println("Creating test menu");
                uiStack.push(UIStack.getNewTestMenu());
        }
    }

    @Override
    public void keyTyped(KeyEvent paramKeyEvent){}
    
    @Override
    public void mousePressed(MouseEvent paramMouseEvent)
    {
        boolean uiEvent = uiStack.mousePressed(paramMouseEvent) != null;
        paramMouseEvent.translatePoint(-(int) screenLocationX, -(int) screenLocationY);
        if(!uiEvent)
        {
            if(paramMouseEvent.getButton() == MouseEvent.BUTTON1)
            {
                dragSelectStartX = paramMouseEvent.getX();
                dragSelectStartY = paramMouseEvent.getY();
                dragSelect = true;
                System.out.println("DragSelectStarted, x:" + dragSelectStartX + " y:" + dragSelectStartY);
            }
        }
    }
    
    @Override
    public void mouseDragged(MouseEvent paramMouseEvent)
    {
        mouseX = paramMouseEvent.getX();
        mouseY = paramMouseEvent.getY();
        boolean uiEvent = uiStack.mouseDragged(paramMouseEvent) != null;
        paramMouseEvent.translatePoint(-(int) screenLocationX, -(int) screenLocationY);

        if(paramMouseEvent.getModifiers()==MouseEvent.BUTTON1_MASK)
        {
            if(dragSelect)
            {
                dragSelectEndX=paramMouseEvent.getX();
                dragSelectEndY=paramMouseEvent.getY();
                /* Rectangle must be created as if drawn from top left corner with positive width&height.
                 * Not sure what step actually breaks otherwise (bounding box invalid or intersecting area calculation failing most likely)
                 */
                Rectangle selectionArea = new Rectangle(dragSelectStartX<dragSelectEndX?dragSelectStartX:dragSelectEndX,
                        dragSelectStartY<dragSelectEndY?dragSelectStartY:dragSelectEndY,
                        Math.max(Math.abs(dragSelectEndX-dragSelectStartX), 1),//at least 1, if the mouse isn't moved this guarantees at least a 1x1 area is selected
                        Math.max(Math.abs(dragSelectEndY-dragSelectStartY), 1));
                user.selectEntity(selectionArea,currentWorld);
                this.selectionArea = selectionArea;
                System.out.println("Drag to x:" +
                                   dragSelectEndX +
                                   " y:" +
                                   dragSelectEndY
                                   +
                                   "\n\tStarted, x:" +
                                   dragSelectStartX +
                                   " y:" +
                                   dragSelectStartY);
            }
        }
    }

    @Override
    public void mouseMoved(MouseEvent paramMouseEvent)
    {
        mouseX = paramMouseEvent.getX();
        mouseY = paramMouseEvent.getY();
        boolean uiEvent = uiStack.mouseMoved(paramMouseEvent) != null;
        //paramMouseEvent.translatePoint(-(int)screenLocationX,-(int)screenLocationY);
    }

    @Override
    public void mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent)
    {
        boolean uiEvent = uiStack.mouseWheelMoved(paramMouseWheelEvent) != null;
        //paramMouseEvent.translatePoint(-(int)screenLocationX,-(int)screenLocationY);
    }

    @Override //Do not use, partially broken implementation. Moving the mouse between press and release will prevent the event firing
    public void mouseClicked(MouseEvent paramMouseEvent){}

    @Override
    public void mouseReleased(MouseEvent paramMouseEvent)
    {
        boolean uiEvent = uiStack.mouseReleased(paramMouseEvent) != null;
        paramMouseEvent.translatePoint(-(int) screenLocationX, -(int) screenLocationY);

        if(paramMouseEvent.getButton()==MouseEvent.BUTTON1)
        {
            if(dragSelect)
            {
                dragSelectEndX=paramMouseEvent.getX();
                dragSelectEndY=paramMouseEvent.getY();
                dragSelect=false;
                /* Rectangle must be created as if drawn from top left corner with positive width&height.
                 * Not sure what step actually breaks otherwise (bounding box invalid or intersecting area calculation failing most likely)
                 */
                Rectangle selectionArea = new Rectangle(dragSelectStartX<dragSelectEndX?dragSelectStartX:dragSelectEndX,
                        dragSelectStartY<dragSelectEndY?dragSelectStartY:dragSelectEndY,
                        Math.max(Math.abs(dragSelectEndX-dragSelectStartX), 1),//at least 1, if the mouse isn't moved this guarantees at least a 1x1 area is selected
                        Math.max(Math.abs(dragSelectEndY-dragSelectStartY), 1));
                user.selectEntity(selectionArea,currentWorld);
                this.selectionArea = null;
                System.out.println("Drag Ended, x:" +
                                   dragSelectEndX +
                                   " y:" +
                                   dragSelectEndY
                                   +
                                   "\n\tStarted, x:" +
                                   dragSelectStartX +
                                   " y:" +
                                   dragSelectStartY);
            }
        }
        else if(paramMouseEvent.getButton()==MouseEvent.BUTTON3)
        {
            if(!uiEvent)
            {
                //TODO: refactor? to allow drag for target orientation
                if(paramMouseEvent.isShiftDown())
                {
                    user.queueAction(new Action(Action.ActionType.MOVE, new Object[]{(double)paramMouseEvent.getX(), (double)paramMouseEvent.getY()}));
                }
                else
                {
                    user.replaceQueue(new Action(Action.ActionType.MOVE, new Object[]{(double)paramMouseEvent.getX(), (double)paramMouseEvent.getY()}));
                }
            }
        }
    }

    @Override
    public void mouseEntered (MouseEvent paramMouseEvent)
    {
        boolean uiEvent = uiStack.mouseEntered(paramMouseEvent) != null;
        //paramMouseEvent.translatePoint(-(int)screenLocationX,-(int)screenLocationY);
    }

    @Override
    public void mouseExited(MouseEvent paramMouseEvent)
    {
        boolean uiEvent = uiStack.mouseExited(paramMouseEvent) != null;
        //paramMouseEvent.translatePoint(-(int)screenLocationX,-(int)screenLocationY);
    }
}
