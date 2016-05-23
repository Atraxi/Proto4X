package atraxi.client.ui;

import atraxi.client.util.RenderUtil;
import atraxi.client.util.ResourceManager;
import atraxi.core.util.Globals;

import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;
import java.awt.image.BufferedImage;

/**
 * Created by Atraxi on 6/10/2015.
 */
public class InfoPanel implements UIElement
{
    private Rectangle dim;
    private Globals.Identifiers background;
    private Globals.Identifiers icon;
    private int health;
    private int supply;
    private int ammunition;
    private String[] specialModules;


    public InfoPanel(Rectangle dim, Globals.Identifiers icon, int health, int supply, int ammunition)
    {
        this(dim, Globals.Identifiers.infoPanelDefault, icon, health, supply, ammunition, new String[0]);
    }

    public InfoPanel(Rectangle dim, Globals.Identifiers background, Globals.Identifiers icon, int health, int supply, int ammunition,
                     String[] specialModules)
    {
        this.dim = dim;
        this.background = background;
        this.icon = icon;
        this.health = health;
        this.supply = supply;
        this.ammunition = ammunition;
        this.specialModules = specialModules;
    }

    @Override
    public UIElement mousePressed(MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseReleased(MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseDragged(MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseMoved(MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent)
    {
        return null;
    }

    @Override
    public void paint(RenderUtil render)
    {
        //TODO RenderUtil with a single public method taking a lambda equivalent of this paint method, and a Rectangle to set Graphics2D clip area
        render.drawImage(background, dim.x, dim.y, dim);
        render.drawImage(icon, dim.x + 5, dim.y + 5, dim);
        BufferedImage icon = ResourceManager.getImage(this.icon);
        for (String module : specialModules)
        {
            render.drawString(module, icon.getWidth() + 10 + dim.x, 5 + dim.y, dim);
        }
        render.drawString(Integer.toString(health), 5 + dim.x, icon.getHeight(null) + 10 + dim.y, dim);
        render.drawString(Integer.toString(supply), 5 + dim.x, icon.getHeight(null) + 20 + dim.y, dim);
        render.drawString(Integer.toString(ammunition), 5 + dim.x, icon.getHeight(null) + 30 + dim.y, dim);
        if (Globals.debug.getDetailedInfoLevel() > 4)
        {
            render.drawRect(dim.x, dim.y, dim.width, dim.height);
        }
    }
}
