package atraxi.game.UI;

import atraxi.game.Game;
import atraxi.game.Player;
import entities.actionQueue.Action;

import java.awt.Color;
import java.awt.Graphics;
import java.awt.Rectangle;
import java.awt.event.KeyEvent;
import java.awt.event.KeyListener;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.awt.event.MouseMotionListener;
import java.awt.event.MouseWheelEvent;
import java.awt.event.MouseWheelListener;

public class UserInterfaceHandler implements KeyListener, MouseListener, MouseWheelListener, MouseMotionListener
{
    private Player user;
    private Rectangle selectionArea = null;
    
    private int dragSelectStartX, dragSelectEndX, dragSelectStartY, dragSelectEndY;
    private boolean dragSelect;
    
    public UserInterfaceHandler(Player user)
    {
        this.user = user;
    }

    public void paint(Graphics g)
    {
        //Graphics2D g2d = (Graphics2D) g;
        if(selectionArea!=null)
        {
            g.setColor(Color.WHITE);
            g.drawRect(selectionArea.x, selectionArea.y, selectionArea.width, selectionArea.height);
        }
    }
    
    public void setSelectionArea(Rectangle selectionArea)
    {
        this.selectionArea = selectionArea;
    }

    /**
     * Passes the event through the UI stack
     * @param paramMouseEvent
     * @return true if this event occurred over a UI element that blocks other UI interaction below it
     */
    protected static boolean uiOverlay(MouseEvent paramMouseEvent)
    {
        return false;
    }

    /**
     *
     * @param paramMouseWheelEvent
     * @return true if this event occurred over a UI element that blocks other UI interaction below it
     */
    protected static boolean uiOverlay(MouseWheelEvent paramMouseWheelEvent)
    {
        return false;
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
        }
    }

    @Override
    public void keyTyped(KeyEvent paramKeyEvent){}
    
    @Override
    public void mousePressed(MouseEvent paramMouseEvent)
    {
        boolean uiEvent = uiOverlay(paramMouseEvent);
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
        boolean uiEvent = uiOverlay(paramMouseEvent);
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
            }
        }
    }

    @Override
    public void mouseMoved(MouseEvent paramMouseEvent){boolean uiEvent = uiOverlay(paramMouseEvent);}

    @Override
    public void mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent){boolean uiEvent = uiOverlay(paramMouseWheelEvent);}

    @Override //Do not use, partially broken implementation. Moving the mouse between press and release will prevent the event firing
    public void mouseClicked(MouseEvent paramMouseEvent){}

    @Override
    public void mouseReleased(MouseEvent paramMouseEvent)
    {
        boolean uiEvent = uiOverlay(paramMouseEvent);
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
    public void mouseEntered(MouseEvent paramMouseEvent){boolean uiEvent = uiOverlay(paramMouseEvent);}

    @Override
    public void mouseExited(MouseEvent paramMouseEvent){boolean uiEvent = uiOverlay(paramMouseEvent);}
}
