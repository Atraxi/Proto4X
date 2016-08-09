package atraxi.core.entities.action;

import atraxi.core.entities.action.definitions.Action;

/**
 * A FIFO linked list for queued actions
 */
public class ActionQueue
{
    private Action head;
    private Action tail;

    /**
     * Add a new Action to the end of the list
     * @param action
     */
    public void queueAction(Action action)
    {
        if(isEmpty())
        {
            head = action;
            tail = action;
        }
        else
        {
            tail.nextAction = action;
            tail = action;
        }
    }

    /**
     * Remove the Action at the head of the list, and return it
     * @return
     */
    public Action pullAction()
    {
        if(isEmpty())
        {
            return null;
        }
        else
        {
            Action temp = head;
            head = head.nextAction;
            return temp;
        }
    }

    /**
     * Return the Action at the head of the list, without removing it from the list
     * @return
     */
    public Action getHead()
    {
        return head;
    }

    public boolean isEmpty()
    {
        return head == null;
    }

    /**
     * Replace the current queue with a new queue initialized to one node;
     * @param action
     */
    public void replaceQueue(Action action)
    {
        head = action;
        tail = action;
    }
}