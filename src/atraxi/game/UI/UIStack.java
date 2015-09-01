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
        if(head!=null)
        {
            UIStackNode currentNode = head;
            do
            {
                if(currentNode.mousePressed(paramMouseEvent))
                {
                    return true;
                }
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseReleased (MouseEvent paramMouseEvent)
    {
        if(head!=null)
        {
            UIStackNode currentNode = head;
            do
            {
                if(currentNode.mouseReleased(paramMouseEvent))
                {
                    return true;
                }
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseEntered (MouseEvent paramMouseEvent)
    {
        if(head!=null)
        {
            UIStackNode currentNode = head;
            do
            {
                if(currentNode.mouseEntered(paramMouseEvent))
                {
                    return true;
                }
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseExited (MouseEvent paramMouseEvent)
    {
        if(head!=null)
        {
            UIStackNode currentNode = head;
            do
            {
                if(currentNode.mouseExited(paramMouseEvent))
                {
                    return true;
                }
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseDragged (MouseEvent paramMouseEvent)
    {
        if(head!=null)
        {
            UIStackNode currentNode = head;
            do
            {
                if(currentNode.mouseDragged(paramMouseEvent))
                {
                    return true;
                }
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseMoved (MouseEvent paramMouseEvent)
    {
        if(head!=null)
        {
            UIStackNode currentNode = head;
            do
            {
                if(currentNode.mouseMoved(paramMouseEvent))
                {
                    return true;
                }
            }
            while(currentNode != null);
        }
        return false;
    }

    @Override
    public boolean mouseWheelMoved (MouseWheelEvent paramMouseWheelEvent)
    {
        if(head!=null)
        {
            UIStackNode currentNode = head;
            do
            {
                if(currentNode.mouseWheelMoved(paramMouseWheelEvent))
                {
                    return true;
                }
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
            tail = newNode;
        }
    }

    public static UIStackNode getNewTestMenu()
    {
        return new Menu(new ImageIcon("resources/testButton.png").getImage(),Proto.screen_Width/2,Proto.screen_Height/2, new Button[]{new Button(
                    new ImageIcon("resources/baseBuildingClass.png").getImage(),
                    0,
                    0,
                    () -> {
                        System.out.println("button clicked");
                        return null;
                    })});
    }
}
