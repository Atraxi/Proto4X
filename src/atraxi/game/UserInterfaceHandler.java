package atraxi.game;

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

import factions.Player;

public class UserInterfaceHandler implements KeyListener, MouseListener, MouseWheelListener, MouseMotionListener
{
    Player user;
    private Rectangle selectionArea = null;
    
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
    
    private int dragSelectStartX, dragSelectEndX, dragSelectStartY, dragSelectEndY;
    private boolean dragSelect;
    
    @Override
    public void keyReleased(KeyEvent paramKeyEvent){}
    
    @Override
    public void keyPressed(KeyEvent paramKeyEvent)
    {
        if(paramKeyEvent.getKeyCode()==KeyEvent.VK_ESCAPE)
        {
            System.exit(0);
        }
        else if(paramKeyEvent.getKeyCode()==KeyEvent.VK_PAUSE)
        {
            Game.paused=!Game.paused;
        }
    }

    @Override
    public void keyTyped(KeyEvent paramKeyEvent){}
    
    @Override
    public void mousePressed(MouseEvent paramMouseEvent)
    {
        if(paramMouseEvent.getButton()==MouseEvent.BUTTON1)
        {
            dragSelectStartX = paramMouseEvent.getX();
            dragSelectStartY = paramMouseEvent.getY();
            dragSelect=true;
            System.out.println("DragSelectStarted, x:"+dragSelectStartX+" y:"+dragSelectStartY);
        }
    }
    
    @Override
    public void mouseDragged(MouseEvent paramMouseEvent)
    {
        if(paramMouseEvent.getModifiers()==MouseEvent.BUTTON1_MASK)
        {
            if(dragSelect)
            {
                dragSelectEndX=paramMouseEvent.getX();
                dragSelectEndY=paramMouseEvent.getY();
                System.out.println("Drag to x:"+dragSelectEndX+" y:"+dragSelectEndY+"\n\tStarted, x:"+dragSelectStartX+" y:"+dragSelectStartY);
                /* Rectangle must processed as if drawn from top left corner with positive width&height.
                 * Not sure what step actually breaks (bounding box invalid or intersecting area calculation failing most likely)
                 */
                Rectangle selectionArea = new Rectangle(dragSelectStartX<dragSelectEndX?dragSelectStartX:dragSelectEndX,
                        dragSelectStartY<dragSelectEndY?dragSelectStartY:dragSelectEndY,
                        Math.max(Math.abs(dragSelectEndX-dragSelectStartX), 1),
                        Math.max(Math.abs(dragSelectEndY-dragSelectStartY), 1));
                user.selectEntity(selectionArea);
                setSelectionArea(selectionArea);
            }
        }
    }

    @Override
    public void mouseMoved(MouseEvent paramMouseEvent){}

    @Override
    public void mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent){}

    @Override //Do not use, partially broken implementation. Moving the mouse between press and release will prevent the event firing
    public void mouseClicked(MouseEvent paramMouseEvent){}

    @Override
    public void mouseReleased(MouseEvent paramMouseEvent)
    {
        if(paramMouseEvent.getButton()==MouseEvent.BUTTON1)
        {
            if(dragSelect)
            {
                dragSelectEndX=paramMouseEvent.getX();
                dragSelectEndY=paramMouseEvent.getY();
                dragSelect=false;
                System.out.println("Drag Ended, x:"+dragSelectEndX+" y:"+dragSelectEndY+"\n\tStarted, x:"+dragSelectStartX+" y:"+dragSelectStartY);
                /* Rectangle must be drawn from top left corner with positive width&height.
                 * Not sure what step actually breaks (bounding box invalid or intersecting area calculation failing most likely)
                 */
                Rectangle selectionArea = new Rectangle(dragSelectStartX<dragSelectEndX?dragSelectStartX:dragSelectEndX,
                        dragSelectStartY<dragSelectEndY?dragSelectStartY:dragSelectEndY,
                        Math.max(Math.abs(dragSelectEndX-dragSelectStartX), 1),
                        Math.max(Math.abs(dragSelectEndY-dragSelectStartY), 1));
                user.selectEntity(selectionArea);
                setSelectionArea(null);
            }
        }
        else if(paramMouseEvent.getButton()==MouseEvent.BUTTON3)
        {
            //TODO: refactor to allow drag for target orientation
            user.issueMoveToSelected(paramMouseEvent.getX(), paramMouseEvent.getY());
        }
    }

    @Override
    public void mouseEntered(MouseEvent paramMouseEvent){}

    @Override
    public void mouseExited(MouseEvent paramMouseEvent){}
}
