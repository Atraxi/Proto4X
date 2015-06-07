package entities;

import factions.Player;

public class Structure extends Entity
{
    //private something? rallyPoint;
    private Player owner;
    
    public Structure(String type, Player owner, int x, int y)
    {
        super(type, x, y);
        this.owner = owner;
    }
    
    @Override
    public void rightClickCommand(double x, double y)
    {
        //TODO: rally point. needs to support complex commands, patrol assist etc
    }
    
    public void performAction()
    {
        
    }
    
    public class Action
    {
        
    }
}
