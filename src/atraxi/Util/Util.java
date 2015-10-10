package atraxi.util;

import atraxi.util.Logger.LogLevel;
import atraxi.game.Proto;

import java.awt.FontMetrics;
import java.awt.Graphics2D;
import java.awt.Rectangle;
import java.awt.geom.Rectangle2D;

/**
 * Created by Atraxi on 10/10/2015.
 */
public class Util
{
    public static void drawString(String stringToDraw, Rectangle containingArea, Graphics2D g2d)
    {
        FontMetrics fontMetrics = g2d.getFontMetrics();
        Rectangle2D stringDim = fontMetrics.getStringBounds(stringToDraw, g2d);
        if(containingArea.getWidth() < stringDim.getWidth() || containingArea.getHeight() < stringDim.getHeight())
        {
            Logger.log(LogLevel.warning, "String overflowed container");
        }
        g2d.drawString(stringToDraw, (int) (containingArea.getCenterX()-(stringDim.getWidth()/2)), (int) (containingArea.getCenterY()+(stringDim.getHeight()/2)) );
        if(Proto.debug)
        {
            g2d.drawRect((int)(containingArea.getCenterX()-(stringDim.getWidth()/2)), (int)(containingArea.getCenterY()-(stringDim.getHeight()/2) + fontMetrics.getMaxDescent()), (int)stringDim.getWidth(), (int)stringDim.getHeight());
        }
    }
}
