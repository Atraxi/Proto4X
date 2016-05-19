package atraxi.client.util;

import atraxi.client.ui.UIElement;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;

import java.awt.Color;
import java.awt.FontMetrics;
import java.awt.Graphics2D;
import java.awt.Rectangle;
import java.awt.Shape;
import java.awt.geom.AffineTransform;
import java.awt.geom.Rectangle2D;
import java.awt.image.ImageObserver;
import java.util.ArrayList;
import java.util.Arrays;

/**
 * Created by Atraxi on 10/10/2015.
 */
public class CheckedRender
{
    /**
     * The current graphics context
     */
    private Graphics2D g2d;

    public void setG2d(Graphics2D g2d)
    {
        this.g2d = g2d;
    }
    /**
     * Draw the specified String to the screen centered within it's bounding area, checking if it is fully contained within it's provided bounding area, and logging a warning if not
     * @param stringToDraw
     * @param containingArea
     */
    public void drawString(String stringToDraw, Rectangle containingArea)
    {
        FontMetrics fontMetrics = g2d.getFontMetrics();
        Rectangle2D stringDim = fontMetrics.getStringBounds(stringToDraw, g2d);
        drawString(stringToDraw, (int) (containingArea.getCenterX()-(stringDim.getWidth()/2)), (int) (containingArea.getCenterY()+(stringDim.getHeight()/2)), containingArea, stringDim);
    }

    /**
     * Draw the specified String to the screen at the given x, y co-ordinates, checking if it is fully contained within it's provided container area, and logging a warning if not
     * @param stringToDraw
     * @param x
     * @param y
     * @param containingArea
     */
    public void drawString(String stringToDraw, int x, int y, Rectangle containingArea)
    {
        FontMetrics fontMetrics = g2d.getFontMetrics();
        Rectangle2D stringDim = fontMetrics.getStringBounds(stringToDraw, g2d);
        drawString(stringToDraw, x, y, containingArea, stringDim);
    }

    /**
     * Draw the specified String to the screen at the given x, y co-ordinates, checking if the String dimensions are fully contained within it's container bounding area, and logging a warning if not
     * @param stringToDraw
     * @param x
     * @param y
     * @param containingArea
     * @param stringDim
     */
    private void drawString(String stringToDraw, int x, int y, Rectangle containingArea, Rectangle2D stringDim)
    {
        if (containingArea == null)
        {
            throw new NullPointerException("containingArea cannot be null");
        }
        if(Globals.debug.getDetailedInfoLevel() > 0 &&
           (x < containingArea.x ||
                        y < containingArea.y ||
                        containingArea.getWidth() + containingArea.x < stringDim.getWidth() + x ||
                        containingArea.getHeight() + containingArea.y < stringDim.getHeight()+ y))
        {//This is excessively detailed logging, but a string overflowing it's container is also a simple oversight that looks sloppy. This safeguards against that happening as long as the logs are watch occasionally
            //throw new IllegalArgumentException("String overflowed container:\n" + stringToDraw);//Ideally this would be a hard crash (in debug only) but Swing handles the exception internally so it's just obscure console spam
            ArrayList<String> stackTrace = new ArrayList<String>();
            stackTrace.add("String overflowed container");
            stackTrace.add("String:\"" + stringToDraw + "\" x:" + x + " y:" + y);
            Arrays.stream(Thread.currentThread().getStackTrace()).forEach((e)-> {
                if(e.toString().contains("atraxi")) {
                    stackTrace.add(e.toString());
                }
            });
            Logger.log(Logger.LogLevel.warning, stackTrace.toArray(new String[stackTrace.size()]));
        }

        g2d.drawString(stringToDraw, x, y);
        if(Globals.debug.getDetailedInfoLevel() >= 4)
        {
            g2d.drawRect(x, (int)(y - stringDim.getHeight() + g2d.getFontMetrics().getMaxDescent()),
                         (int) stringDim.getWidth(),
                         (int) stringDim.getHeight());
        }
    }

    /**
     * Draw the specified {@link java.awt.image.BufferedImage Image} to the screen at the given x, y co-ordinates in world space, checking if the Image dimensions are fully
     * contained within it's container bounding area, and logging a warning if not
     * @param imageID
     * @param x
     * @param y
     * @param containingArea
     */
    public void drawImage(ResourceManager.ImageID imageID, int x, int y, Rectangle containingArea)
    {
        if(Globals.debug.getDetailedInfoLevel() > 0 && containingArea != null && (
                x < containingArea.x ||
                        y < containingArea.y ||
                        x + imageID.getImage().getWidth() > containingArea.x + containingArea.width ||
                        y + imageID.getImage().getHeight() > containingArea.y + containingArea.height))
        {
            ArrayList<String> stackTrace = new ArrayList<String>();
            stackTrace.add("Image overflowed container");
            stackTrace.add("Image:\"" + imageID.name() + "\" x:" + x + " y:" + y);
            Arrays.stream(Thread.currentThread().getStackTrace()).forEach((e)-> {
                if(e.toString().contains("atraxi")) {
                    stackTrace.add(e.toString());
                }
            });
            Logger.log(Logger.LogLevel.warning, stackTrace.toArray(new String[stackTrace.size()]));
        }
        g2d.drawImage(imageID.getImage(), x, y, null);
    }

    /**
     * Exposes {@link java.awt.Graphics2D#drawRect(int, int, int, int) drawRect()} from the underlying {@link Graphics2D} object.
     * @see
     * @param x
     * @param y
     * @param width
     * @param height
     */
    public void drawRect(int x, int y, int width, int height) {
        g2d.drawRect(x, y, width, height);
    }

    /**
     * Sets the {@link Color}, and calls the {@link java.awt.Graphics2D#fill(Shape) fill()} method for the underlying {@link Graphics2D} object.
     * Then resets the color.
     * @param color
     * @param dim
     */
    public void fill(Color color, Rectangle dim)
    {
        Color originalColor = g2d.getColor();
        if (color != null)
        {
            g2d.setColor(color);
        }

        g2d.fill(dim);

        g2d.setColor(originalColor);
    }

    /**
     * This calls {@link UIElement#paint(CheckedRender render) paint()} on the provided {@link UIElement}, but sets the {@link Graphics2D} clip area set to the {@link Rectangle} dim.
     * The original clip area is stored internally and restored before this method returns.
     * @see Graphics2D#setClip(Shape)
     * @see Graphics2D#getClip()
     * @param uiElement
     * @param dim
     */
    public void paintWithinBounds(UIElement uiElement, Rectangle dim)
    {
        Shape originalClip = g2d.getClip();
        if(dim != null)
        {
            g2d.setClip(dim);
        }

        uiElement.paint(this);

        g2d.setClip(originalClip);
    }

    public void drawImage(ResourceManager.ImageID imageID, AffineTransform transform, ImageObserver observer)
    {
        g2d.drawImage(imageID.getImage(), transform, observer);
    }
}
