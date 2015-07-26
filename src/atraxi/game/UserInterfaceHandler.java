package atraxi.game;

import java.awt.Color;
import java.awt.Graphics;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.event.KeyEvent;
import java.awt.event.KeyListener;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.awt.event.MouseMotionListener;
import java.awt.event.MouseWheelEvent;
import java.awt.event.MouseWheelListener;
import java.awt.geom.Area;

import factions.Player;

public class UserInterfaceHandler implements KeyListener, MouseListener, MouseWheelListener, MouseMotionListener
{
    private Player user;
    private Rectangle selectionArea = null;
    private static Area playableArea;
    
    private int dragSelectStartX, dragSelectEndX, dragSelectStartY, dragSelectEndY;
    private boolean dragSelect;
    
    public UserInterfaceHandler(Player user)
    {
        this.user = user;

        playableArea = new Area(new Rectangle(0,0,Proto.screen_Width,Proto.screen_Height));
        playableArea.subtract(new Area(new Rectangle(0,0,Proto.screen_Width,(int)(Proto.screen_Height*0.1))));
        playableArea.subtract(new Area(new Rectangle(0,(int)(Proto.screen_Height*0.9),Proto.screen_Width,(int)(Proto.screen_Height*0.1))));
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
    
    protected static boolean playableAreaContains(Point point)
    {
        return playableArea.contains(point);
    }

    @Override
    public void keyReleased(KeyEvent paramKeyEvent){}
    
    @Override
    public void keyPressed(KeyEvent paramKeyEvent)
    {
        switch (paramKeyEvent.getKeyCode())
        {
            case KeyEvent.VK_B:
                //TODO!!!half done, tell selected building(s) to build something
                break;
            case KeyEvent.VK_PAUSE:
                Game.paused=!Game.paused;
                break;
            case KeyEvent.VK_ESCAPE:
                System.exit(0);
                break;
        }
    }

    @Override
    public void keyTyped(KeyEvent paramKeyEvent){}
    
    @Override
    public void mousePressed(MouseEvent paramMouseEvent)
    {
        if(paramMouseEvent.getButton()==MouseEvent.BUTTON1)
        {
            if(playableAreaContains(paramMouseEvent.getLocationOnScreen()))
            {
                dragSelectStartX = paramMouseEvent.getX();
                dragSelectStartY = paramMouseEvent.getY();
                dragSelect=true;
            }
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
            if(playableAreaContains(paramMouseEvent.getLocationOnScreen()))
            {
                //TODO: refactor to allow drag for target orientation
                user.issueMoveToSelected(paramMouseEvent.getX(), paramMouseEvent.getY());
            }
        }
    }

    @Override
    public void mouseEntered(MouseEvent paramMouseEvent){}

    @Override
    public void mouseExited(MouseEvent paramMouseEvent){}

}
