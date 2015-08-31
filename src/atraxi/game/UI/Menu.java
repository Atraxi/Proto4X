package atraxi.game.UI;

import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.Image;
import java.awt.Toolkit;
import java.awt.event.MouseEvent;

public class Menu implements UIElement
{
    //TODO: implement image scaling, try "image.getScaledInstance(width,height,algorithm(enum));"
    private Button[] buttons;
    private int x, y;
    private Image menuBackground;

    public Menu(Image background,int x, int y,Button[] buttons)
    {
        this.menuBackground=background;
        this.x=x;
        this.y=y;
        this.buttons = buttons;
    }

    public void paint (Graphics g)
    {
        Graphics2D g2d = (Graphics2D) g;
        g2d.drawImage(menuBackground, 0, 0, null);
        for(Button button : buttons)
        {
            g2d.drawImage(button.getImage(), x+button.getX(), y+button.getY(), null);
        }
        Toolkit.getDefaultToolkit().sync();
        g.dispose();
    }

    @Override
    public boolean mouseEntered (MouseEvent paramMouseEvent){return false;}

    @Override
    public boolean mouseExited (MouseEvent paramMouseEvent) {return false;}

    @Override
    public boolean mousePressed (MouseEvent paramMouseEvent)
    {
        for(Button button : buttons)
        {
            if(button.dim.contains(paramMouseEvent.getLocationOnScreen()))
            {
                button.state = Button.ButtonState.PRESSED;
                return true;
            }
        }
        return false;
    }

    @Override
    public boolean mouseReleased (MouseEvent paramMouseEvent)
    {
        for(Button button : buttons)
        {
            if(button.dim.contains(paramMouseEvent.getLocationOnScreen()) && button.state==Button.ButtonState.PRESSED)
            {
                try
                {
                    button.executeAction();
                }
                catch(Exception e)
                {
                    //TODO: Handle this properly, although no exceptions are thrown explicitly (as of writing this comment) so it's unlikely to actually occur
                    e.printStackTrace();
                }
                button.state = Button.ButtonState.HOVER;
                return true;
            }
        }
        return false;
    }

    @Override
    public boolean mouseDragged (MouseEvent paramMouseEvent)
    {
        return false;
    }

    @Override
    public boolean mouseMoved (MouseEvent paramMouseEvent)
    {
        for(Button button : buttons)
        {
            if(button.dim.contains(paramMouseEvent.getPoint()) && button.state == Button.ButtonState.DEFAULT)
            {
                button.state = Button.ButtonState.HOVER;
                return true;
            }
        }
        return false;
    }

    @Override
    public UIElement getNextNode ()
    {
        return null;
    }

    @Override
    public void setNextNode (UIElement element)
    {

    }
}
