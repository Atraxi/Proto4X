package atraxi.game.UI;

import atraxi.game.Proto;

import javax.swing.ImageIcon;
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

    public Menu(Image background,int x, int y)
    {
        this.menuBackground=background;
        this.x=x;
        this.y=y;
        //buttons = new Button[]{new Button(new ImageIcon("resources/"+"testButton"+".png").getImage(),(int)(Proto.screen_Width*0.8),(int)(Proto.screen_Height*0.4))};
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
    public boolean mouseEntered (MouseEvent e){return false;}

    @Override
    public boolean mouseExited (MouseEvent e) {return false;}

    @Override
    public boolean mousePressed (MouseEvent e)
    {
        for(Button button : buttons)
        {
            if(button.mousePressed(e))
            {
                return true;
            }
        }
        return false;
    }

    @Override
    public boolean mouseReleased (MouseEvent e)
    {
        return false;
    }

    @Override
    public boolean mouseDragged (MouseEvent e)
    {

        return false;
    }

    @Override
    public boolean mouseMoved (MouseEvent e)
    {

        return false;
    }
}
