package atraxi.client.ui;

import atraxi.client.Proto;
import atraxi.client.util.RenderUtil;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;

import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

public class UIStack implements UIElement
{
    private UIStackNode head = null, tail = null;

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
    public void paint(RenderUtil render)
    {
        if(head!=null)
        {
            UIStackNode currentNode = head;
            do
            {
                currentNode.paint(render);
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
        return new Menu(Globals.Identifiers.menuBackground,
                        (Proto.getScreenWidth() / 2) - 30,
                        (Proto.getScreenHeight() / 2) - 30,
                        new Button[]{
                                new Button(
                                        Globals.Identifiers.buttonDefault,
                                        Globals.Identifiers.buttonHover,
                                        Globals.Identifiers.buttonClick,
                                        60,
                                        80,
                                        "empty",
                                        menu -> {
                                            Logger.log(Logger.LogLevel.debug, new String[]{"button 1 clicked"});
                                            return null;
                                        }),
                                new Button(
                                        Globals.Identifiers.buttonDefault,
                                        Globals.Identifiers.buttonHover,
                                        Globals.Identifiers.buttonClick,
                                        60,
                                        150,
                                        "empty",
                                        menu -> {
                                            Logger.log(Logger.LogLevel.debug, new String[]{"button 2 clicked"});
                                            return null;
                                        }),
                                new Button(
                                        Globals.Identifiers.buttonDefault,
                                        Globals.Identifiers.buttonHover,
                                        Globals.Identifiers.buttonClick,
                                        60,
                                        220,
                                        "Return to game",
                                        menu -> {
                                            Logger.log(Logger.LogLevel.debug, new String[]{"button 3 clicked", "\tmenu closed"});
                                            UserInterfaceHandler.uiStack.remove(menu);
                                            return null;
                                        }),
                                new Button(
                                        Globals.Identifiers.buttonDefault,
                                        Globals.Identifiers.buttonHover,
                                        Globals.Identifiers.buttonClick,
                                        60,
                                        290,
                                        "Quit game",
                                        menu -> {
                                            Logger.log(Logger.LogLevel.debug, new String[]{"quit game"});
                                            System.exit(0);
                                            return null;
                                        })});
    }
}
