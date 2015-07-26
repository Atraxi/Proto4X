package entities;

import atraxi.game.World;
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
    
    public void performAction(Actions action)
    {
        switch (action)
        {
            case BUILDSHIP1:
                World.getEntityList().add(new Ship("basicShipClass",x,y));
                break;
            case SUICIDE:
                World.getEntityList().remove(this);//TEST:This will probably cause problems, need a removal state
                break;
        }
    }
    
    public enum Actions
    {
        BUILDSHIP1, SUICIDE
    }
}
