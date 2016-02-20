package atraxi.entities.actionQueue;

public class ActionQueue
{
    private Action head;
    private Action tail;
    
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
    
    public Action popAction()
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
    
    public boolean isEmpty()
    {
        if(head==null)
        {
            return true;
        }
        else
        {
            return false;
        }
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
