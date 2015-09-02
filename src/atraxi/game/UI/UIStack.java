package atraxi.game.UI;

import atraxi.game.Proto;

import javax.swing.ImageIcon;
import java.awt.Graphics2D;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

public class UIStack implements UIElement
{
    private UIStackNode head = null, tail = null;

    public UIStack()
    {

    }

    @Override
    public boolean mousePressed (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                if(currentNode.mousePressed(paramMouseEvent))
                {
                    return true;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseReleased (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                if(currentNode.mouseReleased(paramMouseEvent))
                {
                    return true;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseEntered (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                if(currentNode.mouseEntered(paramMouseEvent))
                {
                    return true;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseExited (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                if(currentNode.mouseExited(paramMouseEvent))
                {
                    return true;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseDragged (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                if(currentNode.mouseDragged(paramMouseEvent))
                {
                    return true;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseMoved (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                if(currentNode.mouseMoved(paramMouseEvent))
                {
                    return true;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseWheelMoved (MouseWheelEvent paramMouseWheelEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                if(currentNode.mouseWheelMoved(paramMouseWheelEvent))
                {
                    return true;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public void paint(Graphics2D g2d)
    {
        if(head!=null)
        {
            UIStackNode currentNode = head;
            do
            {
                currentNode.paint(g2d);
                currentNode = currentNode.getNextNode();
            }
            while(currentNode != null);
        }
    }

    public void push(UIStackNode newNode)
    {
        if(head==null)
        {
            head = newNode;
            tail = newNode;
        }
        else
        {
            tail.setNextNode(newNode);
            newNode.setPreviousNode(tail);
            tail = newNode;
        }
    }

    public static UIStackNode getNewTestMenu()
    {
        return new Menu(new ImageIcon("resources/baseMenuClass.png").getImage(),
                        (Proto.screen_Width / 2) - 30,
                        (Proto.screen_Height / 2) - 30,
                        new Button[]{
                                new Button(
                                        new ImageIcon("resources/baseButtonClass.png").getImage(),
                                        60,
                                        80,
                                        () -> {
                                            System.out.println("button 1 clicked");
                                            return null;
                                        }),
                                new Button(
                                        new ImageIcon("resources/baseButtonClass.png").getImage(),
                                        60,
                                        150,
                                        () -> {
                                            System.out.println("button 2 clicked");
                                            return null;
                                        }),
                                new Button(
                                        new ImageIcon("resources/baseButtonClass.png").getImage(),
                                        60,
                                        220,
                                        () -> {
                                            System.out.println("button 3 clicked");
                                            return null;
                                        }),
                                new Button(
                                        new ImageIcon("resources/baseButtonClass.png").getImage(),
                                        60,
                                        290,
                                        () -> {
                                            System.out.println("quit game");
                                            System.exit(0);
                                            return null;
                                        })});
    }
}
