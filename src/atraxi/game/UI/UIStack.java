package atraxi.game.UI;

import atraxi.game.Proto;
import atraxi.game.ResourceManager;
import atraxi.game.ResourceManager.ImageID;

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
    public UIElement mousePressed (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                UIElement nodePressed = currentNode.mousePressed(paramMouseEvent);
                if(nodePressed != null)
                {
                    return nodePressed;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return null;
    }

    @Override
    public UIElement mouseReleased (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                UIElement nodeReleased = currentNode.mouseReleased(paramMouseEvent);
                if(nodeReleased != null)
                {
                    return nodeReleased;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return null;
    }

    @Override
    public UIElement mouseEntered (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                UIElement nodeEntered = currentNode.mouseEntered(paramMouseEvent);
                if(nodeEntered != null)
                {
                    return nodeEntered;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return null;
    }

    @Override
    public UIElement mouseExited (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                UIElement nodeExited = currentNode.mouseExited(paramMouseEvent);
                if(nodeExited != null)
                {
                    return nodeExited;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return null;
    }

    @Override
    public UIElement mouseDragged (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                UIElement nodeDragged = currentNode.mouseDragged(paramMouseEvent);
                if(nodeDragged != null)
                {
                    return nodeDragged;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return null;
    }

    @Override
    public UIElement mouseMoved (MouseEvent paramMouseEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                UIElement nodeMouseMoved = currentNode.mouseMoved(paramMouseEvent);
                if(nodeMouseMoved != null)
                {
                    return nodeMouseMoved;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return null;
    }

    @Override
    public UIElement mouseWheelMoved (MouseWheelEvent paramMouseWheelEvent)
    {
        if(tail!=null)
        {
            UIStackNode currentNode = tail;
            do
            {
                UIElement nodeScrolled = currentNode.mouseWheelMoved(paramMouseWheelEvent);
                if(nodeScrolled != null)
                {
                    return nodeScrolled;
                }
                currentNode = currentNode.getPreviousNode();
            }
            while(currentNode != null);
        }
        return null;
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

    public void remove(UIStackNode node)
    {
        if (node.getNextNode()!=null)
        {
            node.getNextNode().setPreviousNode(node.getPreviousNode());
        }
        else
        {
            tail = node.getPreviousNode();
        }
        if(node.getPreviousNode()!=null)
        {
            node.getPreviousNode().setNextNode(node.getNextNode());
        }
        else
        {
            head = node.getNextNode();
        }
    }

    public static UIStackNode getNewTestMenu()
    {
        return new Menu(ResourceManager.getImage(ImageID.menuBackground),
                        (Proto.screen_Width / 2) - 30,
                        (Proto.screen_Height / 2) - 30,
                        new Button[]{
                                new Button(
                                        ResourceManager.getImage(ImageID.buttonDefault),
                                        ResourceManager.getImage(ImageID.buttonHover),
                                        ResourceManager.getImage(ImageID.buttonClick),
                                        60,
                                        80,
                                        (Menu menu) -> {
                                            System.out.println("button 1 clicked");
                                            return null;
                                        }),
                                new Button(
                                        ResourceManager.getImage(ImageID.buttonDefault),
                                        ResourceManager.getImage(ImageID.buttonHover),
                                        ResourceManager.getImage(ImageID.buttonClick),
                                        60,
                                        150,
                                        menu -> {
                                            System.out.println("button 2 clicked");
                                            Proto.PROTO.setDimensions(500, 500);
                                            return null;
                                        }),
                                new Button(
                                        ResourceManager.getImage(ImageID.buttonDefault),
                                        ResourceManager.getImage(ImageID.buttonHover),
                                        ResourceManager.getImage(ImageID.buttonClick),
                                        60,
                                        220,
                                        menu -> {
                                            System.out.println("button 3 clicked\n\tmenu closed");
                                            UserInterfaceHandler.uiStack.remove(menu);
                                            return null;
                                        }),
                                new Button(
                                        ResourceManager.getImage(ImageID.buttonDefault),
                                        ResourceManager.getImage(ImageID.buttonHover),
                                        ResourceManager.getImage(ImageID.buttonClick),
                                        60,
                                        290,
                                        menu -> {
                                            System.out.println("quit game");
                                            System.exit(0);
                                            return null;
                                        })});
    }
}
