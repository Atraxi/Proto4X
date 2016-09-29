package atraxi.core;

import atraxi.core.entities.action.definitions.Action;
import atraxi.core.entities.action.definitions.ActionAttack;
import atraxi.core.entities.action.definitions.ActionBuild;
import atraxi.core.entities.action.definitions.ActionMove;
import atraxi.core.entities.action.definitions.ActionSetState;

import java.util.ArrayList;
import java.util.List;

public class Player extends BaseNetworkObject
{
    public int metal = 0, money = 0;//TODO: replace with whatever actual resources will be used
    private List<ActionSetState> actionSetStatesThisTurn = new ArrayList<>();
    private List<ActionBuild> actionBuildsThisTurn = new ArrayList<>();
    private List<ActionAttack> actionAttacksThisTurn = new ArrayList<>();
    private List<ActionMove> actionMovesThisTurn = new ArrayList<>();
    private String name;
    private boolean isTurnFinished;

    public Player(String name)
    {
        this.name = name;
    }

    public void queueAction(Action action) throws IllegalArgumentException
    {
        if(action.isValid())
        {
            if(action instanceof ActionSetState)
            {
                actionSetStatesThisTurn.add((ActionSetState) action);
            }
            else if(action instanceof ActionBuild)
            {
                actionBuildsThisTurn.add((ActionBuild) action);
            }
            else if(action instanceof ActionAttack)
            {
                actionAttacksThisTurn.add((ActionAttack) action);
            }
            else if(action instanceof ActionMove)
            {
                actionMovesThisTurn.add((ActionMove) action);
            }
            else
            {
                throw new IllegalArgumentException("Unsupported Action type:" + action.getClass() + ". Must extend ActionBuild, ActionAttack, or ActionMove");
            }
        }
        else
        {
            throw new UnsupportedOperationException("Invalid Action");
        }
    }

    public void processSetStatesThisTurn()
    {
        //TODO: modify to keep a change log (or leave it to be done implicitly elsewhere?)
        actionSetStatesThisTurn.forEach(Action::execute);
        actionSetStatesThisTurn.clear();
    }

    public void processBuildsThisTurn()
    {
        actionBuildsThisTurn.forEach(Action::execute);
        actionBuildsThisTurn.clear();
    }

    public void processAttacksThisTurn()
    {
        actionAttacksThisTurn.forEach(Action::execute);
        actionAttacksThisTurn.clear();
    }

    public void processMovesThisTurn()
    {
        actionMovesThisTurn.forEach(Action::execute);
        actionMovesThisTurn.clear();
    }

    public void doWork(long timeDiff, boolean paused){}

    public void setName(String name)
    {
        this.name = name;
    }

    public String getName()
    {
        return name;
    }

    public void endTurn()
    {
        isTurnFinished = true;
    }

    public boolean isTurnFinished()
    {
        return isTurnFinished;
    }
}
