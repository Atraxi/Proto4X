package atraxi.game;

import java.awt.Rectangle;
import java.util.ArrayList;

import atraxi.game.World.World;
import entities.Entity;
import entities.actionQueue.Action;

public class Player
{
    private Selection selection = new Selection();
    public int metal = 0, money = 0;
    
    /**
     * Attempt to select an entity within the given area (i.e. left click for human)
     */
    public void selectEntity(Rectangle selectionArea)
    {
        selection = new Selection();
        selection.add(World.getEntityArrayWithin(selectionArea));
        System.out.println("Selected:\n"+selection.toString());
    }
    
    public void replaceQueue(Action action)
    {
        selection.replaceQueue(action);
    }
    public void queueAction(Action action)
    {
        selection.queueAction(action);
    }
    
    private class Selection
    {
        private ArrayList<Entity> selected;
        
        public Selection()
        {
            selected = new ArrayList<Entity>();
        }
        
        public void replaceQueue(Action action)
        {
            if(action.type == Action.ActionType.MOVE)
            {
                if(selected.size()==1)
                {
                    if(selected.get(0).canAcceptAction(action.type))
                    {
                        selected.get(0).replaceQueue(action);
                    }
                }
                else
                {
                    //TODO: proper formations, determine destination from formation
                    double xAvg = 0, yAvg = 0;
                    for(Entity selectedEntity : selected)
                    {
                        xAvg+=selectedEntity.getX();
                        yAvg+=selectedEntity.getY();
                    }
                    xAvg=xAvg/selected.size();
                    yAvg=yAvg/selected.size();
                        
                    double x = (double)action.getData()[0];
                    double y = (double)action.getData()[1];
                        
                    for(Entity e : selected)
                    {
                        if(e.canAcceptAction(action.type))
                        {
                            e.replaceQueue(new Action(action.type, new Object[]{x+e.getX()-xAvg,y+e.getY()-yAvg}));
                        }
                    }
                }
            }
            else
            {
                for(Entity e : selected)
                {
                    if(e.canAcceptAction(action.type))
                    {
                        e.replaceQueue(action);
                    }
                }
            }
        }

        public void queueAction(Action action)
        {
            if(action.type==Action.ActionType.MOVE)
            {
                if(selected.size()==1)
                {
                    if(selected.get(0).canAcceptAction(action.type))
                    {
                        selected.get(0).queueAction(action);
                    }
                }
                else
                {
                    //TODO: proper formations, determine destination from formation
                    double xAvg = 0, yAvg = 0;
                    for(Entity selectedEntity : selected)
                    {
                        xAvg+=selectedEntity.getX();
                        yAvg+=selectedEntity.getY();
                    }
                    xAvg=xAvg/selected.size();
                    yAvg=yAvg/selected.size();
                        
                    double x = (double)action.getData()[0];
                    double y = (double)action.getData()[1];
                        
                    for(Entity e : selected)
                    {
                        if(e.canAcceptAction(action.type))
                        {
                            e.queueAction(new Action(action.type, new Object[]{x+e.getX()-xAvg,y+e.getY()-yAvg}));
                        }
                    }
                }
            }
            else
            {
                for(Entity e : selected)
                {
                    if(e.canAcceptAction(action.type))
                    {
                        e.queueAction(action);
                    }
                }
            }
        }
        
        protected void add(Entity[] additions)
        {
            for(Entity e : additions)
            {
                this.selected.add(e);
            }
        }
        
        protected void remove(Entity[] removals)
        {
            //TODO: remove entities from selection. e.g. ctrl deselect
        }
        
        protected Entity[] getSelection()
        {
            return selected.toArray(new Entity[selected.size()]);
        }
        
        @Override
        public String toString()
        {
            String entityList = "Entity List\n{\n";
            for(Entity e : selected)
            {
                entityList+=e+"\n";
            }
            return entityList+"}";
        }
    }
    
    public void doWork(long timeDiff, boolean paused){}
}
