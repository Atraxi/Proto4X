package debug;

import java.awt.event.KeyEvent;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;
import atraxi.game.Player;
import atraxi.game.Game;
import atraxi.game.UI.UserInterfaceHandler;

public class DebugKeyIntercept extends UserInterfaceHandler
{
    public DebugKeyIntercept(Player user)
    {
        super(user);
    }

    @Override
    public void mouseDragged(MouseEvent paramMouseEvent)
    {
        if(paramMouseEvent.getModifiers()==MouseEvent.BUTTON1_MASK)
        {
            if(DebugUtil.accessSuperclassBoolean(this, "dragSelect"))
            {
                System.out.println("Drag to x:"+DebugUtil.accessSuperclassInt(this, "dragSelectEndX")+" y:"+DebugUtil.accessSuperclassInt(this, "dragSelectEndY")
                        +"\n\tStarted, x:"+DebugUtil.accessSuperclassInt(this, "dragSelectStartX")+" y:"+DebugUtil.accessSuperclassInt(this, "dragSelectStartY"));
            }
        }
        super.mouseDragged(paramMouseEvent);
    }

    @Override
    public void mouseMoved(MouseEvent paramMouseEvent)
    {
        super.mouseMoved(paramMouseEvent);
    }

    @Override
    public void mouseWheelMoved(MouseWheelEvent arg0)
    {
        super.mouseWheelMoved(arg0);
    }

    @Override
    public void mouseClicked(MouseEvent arg0)
    {
        super.mouseClicked(arg0);
    }

    @Override
    public void mouseEntered(MouseEvent arg0)
    {
        super.mouseEntered(arg0);
    }

    @Override
    public void mouseExited(MouseEvent arg0)
    {
        super.mouseExited(arg0);
    }

    @Override
    public void mousePressed(MouseEvent paramMouseEvent)
    {
        super.mousePressed(paramMouseEvent);
        if(paramMouseEvent.getButton()==MouseEvent.BUTTON1)
        {
            if(playableAreaContains(paramMouseEvent.getLocationOnScreen()))
            {
                System.out.println("DragSelectStarted, x:"+DebugUtil.accessSuperclassInt(this, "dragSelectStartX")+" y:"+DebugUtil.accessSuperclassInt(this, "dragSelectStartY"));
            }
        }
    }

    @Override
    public void mouseReleased(MouseEvent paramMouseEvent)
    {
        if(paramMouseEvent.getButton()==MouseEvent.BUTTON1)
        {
            if(DebugUtil.accessSuperclassBoolean(this, "dragSelect"))
            {
                System.out.println("Drag Ended, x:"+DebugUtil.accessSuperclassInt(this, "dragSelectEndX")+" y:"+DebugUtil.accessSuperclassInt(this, "dragSelectEndY")
                        +"\n\tStarted, x:"+DebugUtil.accessSuperclassInt(this, "dragSelectStartX")+" y:"+DebugUtil.accessSuperclassInt(this, "dragSelectStartY"));
            }
        }
        super.mouseReleased(paramMouseEvent);
    }

    @Override
    public void keyPressed(KeyEvent paramKeyEvent)
    {
        if(paramKeyEvent.getKeyCode()==KeyEvent.VK_ESCAPE)
        {
            System.out.println("escape");
        }
        else if(paramKeyEvent.getKeyCode()==KeyEvent.VK_PAUSE)
        {
            if(!Game.paused)
            {
                System.out.println("Game Paused.");
            }
            else
            {
                System.out.println("Game Resumed.");
            }
        }
        super.keyPressed(paramKeyEvent);
    }

    @Override
    public void keyReleased(KeyEvent arg0)
    {
        super.keyReleased(arg0);
    }

    @Override
    public void keyTyped(KeyEvent arg0)
    {
        super.keyTyped(arg0);
    }
}
