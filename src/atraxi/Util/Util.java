package atraxi.util;

import atraxi.util.Logger.LogLevel;
import atraxi.game.Proto;

import java.awt.FontMetrics;
import java.awt.Graphics2D;
import java.awt.Rectangle;
import java.awt.geom.Rectangle2D;
import java.util.ArrayList;
import java.util.Arrays;

/**
 * Created by Atraxi on 10/10/2015.
 */
public class Util
{
    /**
     * Draw the specified String to the screen centered within it's bounding area, checking if it is fully contained within it's provided bounding area, and logging a warning if not
     * @param stringToDraw
     * @param containingArea
     * @param g2d
     */
    public static void drawString(String stringToDraw, Rectangle containingArea, Graphics2D g2d)
    {
        FontMetrics fontMetrics = g2d.getFontMetrics();
        Rectangle2D stringDim = fontMetrics.getStringBounds(stringToDraw, g2d);
        drawString(stringToDraw, (int) (containingArea.getCenterX()-(stringDim.getWidth()/2)), (int) (containingArea.getCenterY()+(stringDim.getHeight()/2)), containingArea, g2d, stringDim);
    }

    /**
     * Draw the specified String to the screen at the given x, y co-ordinates, checking if it is fully contained within it's provided bounding area, and logging a warning if not
     * @param stringToDraw
     * @param x
     * @param y
     * @param containingArea
     * @param g2d
     */
    public static void drawString(String stringToDraw, int x, int y, Rectangle containingArea, Graphics2D g2d)
    {
        FontMetrics fontMetrics = g2d.getFontMetrics();
        Rectangle2D stringDim = fontMetrics.getStringBounds(stringToDraw, g2d);
        drawString(stringToDraw, x, y, containingArea, g2d, stringDim);
    }

    private static void drawString(String stringToDraw, int x, int y, Rectangle containingArea, Graphics2D g2d, Rectangle2D stringDim)
    {
        if(x < containingArea.x || y < containingArea.y || containingArea.getWidth()+containingArea.x < stringDim.getWidth()+y || containingArea.getHeight()+containingArea.y < stringDim.getHeight()+y)
        {//This is excessively detailed logging, but a string overflowing it's container is also a simple oversight that looks sloppy. This safeguards against that happening as long as the logs are watch occasionally
            ArrayList<String> stackTrace = new ArrayList<String>();
            stackTrace.add("String overflowed container");
            Arrays.stream(Thread.currentThread().getStackTrace()).forEach((e)->stackTrace.add(e.toString()));
            Logger.log(LogLevel.warning, stackTrace.toArray(new String[stackTrace.size()]));
        }

        g2d.drawString(stringToDraw, x, y);
        if(Proto.debug)
        {
            g2d.drawRect(x, (int)(y - stringDim.getHeight() + g2d.getFontMetrics().getMaxDescent()),
                         (int) stringDim.getWidth(),
                         (int) stringDim.getHeight());
        }
    }
}
