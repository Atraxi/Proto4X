package entities.actionQueue;

import java.util.Arrays;

public class Action
{
    protected Action nextAction;
    public ActionType type;
    private Object[] data;
    private boolean executing;
    
    
    public Action(ActionType type, Object[] data, boolean executing)
    {//TODO: safeguards on data types, probably tied to ActionType enumeration
        this.type = type;
        this.data = data;
        this.executing = executing;
    }
    
    public Action(ActionType type, Object[] data)
    {
        this(type, data, false);
    }
    
    public static enum ActionType
    {
        MOVE, STOP, BUILD, SUICIDE, ATTACK, PATROL, GUARD
    }
    
    public Object[] getData()
    {
        return Arrays.copyOf(data, data.length);
    }

    public boolean isExecuting()
    {
        return executing;
    }
}
