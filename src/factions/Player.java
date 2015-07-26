package factions;

import java.awt.Rectangle;
import java.util.ArrayList;

import atraxi.game.World;
import entities.Entity;

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
    
    public void issueMoveToSelected(int x, int y)
    {
        if(selection.getSelection().length > 0)
        {
            selection.rightClickCommand(x, y);
        }
    }
    
    private class Selection
    {
        private ArrayList<Entity> selected;
        
        public Selection()
        {
            selected = new ArrayList<Entity>();
        }
        
        public void rightClickCommand(int x, int y)
        {
            //TODO: detemine context (move, attack, guard etc)
            if(selected.size()==1)
            {
                selected.get(0).rightClickCommand(x, y);
            }
            else
            {
                //TODO: proper formations, determine destination from formation
                double xAvg = 0, yAvg = 0;
                for(Entity e : selected)
                {
                    xAvg+=e.getX();
                    yAvg+=e.getY();
                }
                xAvg=xAvg/selected.size();
                yAvg=yAvg/selected.size();
                for(Entity e : selected)
                {
                    e.rightClickCommand(x+e.getX()-xAvg, y+e.getY()-yAvg);
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
