package atraxi.game;

import java.awt.Color;
import java.awt.Graphics;
import java.awt.Rectangle;

public class UserInterfaceHandler
{
    private Rectangle selectionArea = null;
    
    public UserInterfaceHandler()
    {
        
    }

    public void paint(Graphics g)
    {
        //Graphics2D g2d = (Graphics2D) g;
        if(selectionArea!=null)
        {
            g.setColor(Color.WHITE);
            g.drawRect(selectionArea.x, selectionArea.y, selectionArea.width, selectionArea.height);
            
        }
    }
    
    public void setSelectionArea(Rectangle selectionArea)
    {
        this.selectionArea = selectionArea;
    }
}
