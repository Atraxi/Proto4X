package debug;

import java.awt.event.KeyEvent;
import java.awt.event.KeyListener;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.awt.event.MouseMotionListener;
import java.awt.event.MouseWheelEvent;
import java.awt.event.MouseWheelListener;

import factions.Human;
import factions.Player;
import atraxi.game.Game;

public class DebugKeyIntercept implements KeyListener, MouseListener, MouseWheelListener, MouseMotionListener
{
    private Human human;
    public DebugKeyIntercept(Human human)
    {
        this.human = human;
    }

    @Override
    public void mouseDragged(MouseEvent arg0)
    {
        human.mouseDragged(arg0);
    }

    @Override
    public void mouseMoved(MouseEvent arg0)
    {
        human.mouseMoved(arg0);
    }

    @Override
    public void mouseWheelMoved(MouseWheelEvent arg0)
    {
        human.mouseWheelMoved(arg0);
    }

    @Override
    public void mouseClicked(MouseEvent arg0)
    {
        human.mouseClicked(arg0);
    }

    @Override
    public void mouseEntered(MouseEvent arg0)
    {
        human.mouseEntered(arg0);
    }

    @Override
    public void mouseExited(MouseEvent arg0)
    {
        human.mouseExited(arg0);
    }

    @Override
    public void mousePressed(MouseEvent arg0)
    {
        human.mousePressed(arg0);
    }

    @Override
    public void mouseReleased(MouseEvent arg0)
    {
        human.mouseReleased(arg0);
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
        human.keyPressed(paramKeyEvent);
    }

    @Override
    public void keyReleased(KeyEvent arg0)
    {
        human.keyReleased(arg0);
    }

    @Override
    public void keyTyped(KeyEvent arg0)
    {
        human.keyTyped(arg0);
    }
}
