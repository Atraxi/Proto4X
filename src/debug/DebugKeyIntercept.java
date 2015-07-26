package debug;

import java.awt.event.KeyEvent;
import java.awt.event.KeyListener;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.awt.event.MouseMotionListener;
import java.awt.event.MouseWheelEvent;
import java.awt.event.MouseWheelListener;

import factions.Player;
import atraxi.game.Game;
import atraxi.game.UserInterfaceHandler;

public class DebugKeyIntercept extends UserInterfaceHandler
{
    public DebugKeyIntercept(Player user)
    {
        super(user);
    }

    @Override
    public void mouseDragged(MouseEvent arg0)
    {
        super.mouseDragged(arg0);
    }

    @Override
    public void mouseMoved(MouseEvent arg0)
    {
        super.mouseMoved(arg0);
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
    public void mousePressed(MouseEvent arg0)
    {
        super.mousePressed(arg0);
    }

    @Override
    public void mouseReleased(MouseEvent arg0)
    {
        super.mouseReleased(arg0);
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
