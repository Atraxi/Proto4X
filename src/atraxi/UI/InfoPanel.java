package atraxi.ui;

import atraxi.util.ResourceManager;
import atraxi.util.ResourceManager.ImageID;
import atraxi.util.Util;

import java.awt.Graphics2D;
import java.awt.Image;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

/**
 * Created by Atraxi on 6/10/2015.
 */
public class InfoPanel implements UIElement
{
    private Rectangle dim;
    private ImageID background;
    private ImageID icon;
    private int health;
    private int supply;
    private int ammunition;
    private String[] specialModules;


    public InfoPanel(Rectangle dim, ImageID background, ImageID icon, int health, int supply, int ammunition, String[] specialModules)
    {
        this.dim = dim;
        this.background = background;
        this.icon = icon;
        this.health = health;
        this.supply = supply;
        this.ammunition = ammunition;
        this.specialModules = specialModules;
    }

    public InfoPanel(Rectangle dim, ImageID icon, int health, int supply, int ammunition)
    {
        this(dim, ImageID.infoPanelDefault, icon, health, supply, ammunition, new String[0]);
    }

    @Override
    public UIElement mousePressed (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseReleased (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseEntered (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseExited (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseDragged (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseMoved (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseWheelMoved (MouseWheelEvent paramMouseWheelEvent)
    {
        return null;
    }

    @Override
    public void paint (Graphics2D graphics)
    {
        graphics.drawImage(ResourceManager.getImage(background), dim.x, dim.y, null);
        graphics.drawImage(ResourceManager.getImage(icon), dim.x+5, dim.y+5, null);
        Image icon = ResourceManager.getImage(this.icon);
        for(String module : specialModules)
        {
            Util.drawString(module, icon.getWidth(null) + 10, 5, dim, graphics);
        }
        Util.drawString(Integer.toString(health), 5, icon.getHeight(null)+10, dim, graphics);
        Util.drawString(Integer.toString(supply), 5, icon.getHeight(null)+20, dim, graphics);
        Util.drawString(Integer.toString(ammunition), 5, icon.getHeight(null)+30, dim, graphics);
    }
}
