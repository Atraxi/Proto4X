package atraxi.game;

import java.awt.Color;
import java.awt.Graphics;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.geom.Area;

public class UserInterfaceHandler
{
    private Rectangle selectionArea = null;
    private static Area playableArea;
    
    public UserInterfaceHandler()
    {
        playableArea = new Area(new Rectangle(0,0,Proto.screen_Width,Proto.screen_Height));
        playableArea.subtract(new Area(new Rectangle(0,0,Proto.screen_Width,(int)(Proto.screen_Height*0.1))));
        playableArea.subtract(new Area(new Rectangle(0,(int)(Proto.screen_Height*0.9),Proto.screen_Width,(int)(Proto.screen_Height*0.1))));
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
    
    protected static boolean playableAreaContains(Point point)
    {
        return playableArea.contains(point);
    }
}
