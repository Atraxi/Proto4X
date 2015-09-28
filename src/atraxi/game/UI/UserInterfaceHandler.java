package atraxi.game.UI;

import atraxi.game.Game;
import atraxi.game.Player;
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

public class UserInterfaceHandler implements KeyListener, MouseListener, MouseWheelListener, MouseMotionListener
{
    private Player user;
    public static UIStack uiStack;
    private static double screenLocationX = 0, screenLocationY = 0;

    private Rectangle selectionArea = null;
    private int dragSelectStartX, dragSelectEndX, dragSelectStartY, dragSelectEndY;
    private boolean dragSelect;

    private static int mouseX = 200, mouseY = 200;
    
    public UserInterfaceHandler(Player user)
    {
        this.user = user;
        uiStack = new UIStack();
    }

    public static int getScreenLocationX ()
    {
        return (int) screenLocationX;
    }

    public static int getScreenLocationY ()
    {
        return (int) screenLocationY;
    }

    public void paint(Graphics2D g2d)
    {
        if(selectionArea!=null)
        {
            Color color = g2d.getColor();
            g2d.setColor(Color.WHITE);
            g2d.drawRect(selectionArea.x, selectionArea.y, selectionArea.width, selectionArea.height);
            g2d.setColor(color);
        }
        uiStack.paint(g2d);
    }

    public void doWork (BigDecimal timeAdjustment, boolean paused)
    {
        //TODO: % of screen size, currently assumes 1920*1080
        //TODO: this doesn't feel quite right, experiment with different math. maybe 2 stages of constant speed?
        if(mouseX<300)
        {
            screenLocationX-=timeAdjustment.multiply(new BigDecimal((300 - mouseX)/10)).doubleValue();
        }
        else if(mouseX>1920-300)
        {
            screenLocationX+=timeAdjustment.multiply(new BigDecimal((300-1920+mouseX)/10)).doubleValue();
        }
        if(mouseY<300)
        {
            screenLocationY-=timeAdjustment.multiply(new BigDecimal((300-mouseY)/10)).doubleValue();
        }
        else if(mouseY > 1080-300)
        {
            screenLocationY+=timeAdjustment.multiply(new BigDecimal((300-1080+mouseY)/10)).doubleValue();
        }
    }
    
    public void setSelectionArea(Rectangle selectionArea)
    {
        this.selectionArea = selectionArea;
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
                        Math.max(Math.abs(dragSelectEndX-dragSelectStartX), 1),
                        Math.max(Math.abs(dragSelectEndY-dragSelectStartY), 1));
                user.selectEntity(selectionArea);
                setSelectionArea(selectionArea);
                System.out.println("Drag to x:" +
                                   dragSelectEndX +
                                   " y:" +
                                   dragSelectEndY
                                   +
                                   "\n\tStarted, x:" +
                                   dragSelectStartX +
                                   " y:" +
                                   dragSelectStartY);
                return;
            }
        }
        boolean uiEvent = uiStack.mouseDragged(paramMouseEvent) != null;
    }

    @Override
    public void mouseMoved(MouseEvent paramMouseEvent)
    {
        mouseX = paramMouseEvent.getX();
        mouseY = paramMouseEvent.getY();
        boolean uiEvent = uiStack.mouseMoved(paramMouseEvent) != null;
    }

    @Override
    public void mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent){boolean uiEvent = uiStack.mouseWheelMoved(paramMouseWheelEvent) != null;}

    @Override //Do not use, partially broken implementation. Moving the mouse between press and release will prevent the event firing
    public void mouseClicked(MouseEvent paramMouseEvent){}

    @Override
    public void mouseReleased(MouseEvent paramMouseEvent)
    {
        boolean uiEvent = uiStack.mouseReleased(paramMouseEvent) != null;
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
                        Math.max(Math.abs(dragSelectEndX-dragSelectStartX), 1),
                        Math.max(Math.abs(dragSelectEndY-dragSelectStartY), 1));
                user.selectEntity(selectionArea);
                setSelectionArea(null);
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
    public void mouseEntered(MouseEvent paramMouseEvent){boolean uiEvent = uiStack.mouseEntered(paramMouseEvent) != null;}

    @Override
    public void mouseExited(MouseEvent paramMouseEvent){boolean uiEvent = uiStack.mouseExited(paramMouseEvent) != null;}
}
