package atraxi.game;

import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.Image;
import java.awt.Rectangle;
import java.awt.Toolkit;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;

import javax.swing.ImageIcon;
import javax.swing.JPanel;

import entities.Entity;

public class Menu extends JPanel implements MouseListener
{
    private Button[] buttons;
    public class Button
    {
        private Image image;
        private int x, y;
        private boolean pressed = false;
        protected Rectangle dim;
        public Button(Image image,int x,int y)
        {
            this.image=image;
            this.x=x;
            this.y=y;
            dim=new Rectangle(x,y,image.getWidth(null),image.getHeight(null));
        }
    }

    private Image menuBackground;
    private static final long serialVersionUID = 1L;

    public Menu()
    {
        buttons = new Button[]{new Button(new ImageIcon("resources/"+"testButton"+".png").getImage(),(int)(Proto.screen_Width*0.8),(int)(Proto.screen_Height*0.4))};
    }
    
    @Override
    public void paint(Graphics g)
    {
        super.paint(g);
        Graphics2D g2d = (Graphics2D) g;
        g2d.drawImage(menuBackground, 0, 0, null);
//        for( Entity entity : World.getEntityList())
//        {
//            g2d.drawImage(entity.getImage(), entity.getTransform(), null);
//        }
//        Toolkit.getDefaultToolkit().sync();
        g.dispose();
    }
    
    @Override
    public void mouseClicked(MouseEvent e){}//don't use, mouse movement of any amount invalidates click

    @Override
    public void mouseEntered(MouseEvent e)
    {//TODO: mouseover image change
    }

    @Override
    public void mouseExited(MouseEvent e)
    {//TODO: mouseover image reset
    }

    @Override
    public void mousePressed(MouseEvent e)
    {
        for(Button button : buttons)
        {
            if(button.dim.contains(e.getLocationOnScreen()))
            {
                button.pressed=true;
                //button.image=//load new image, load images once, set by reference
            }
        }
    }

    @Override
    public void mouseReleased(MouseEvent e)
    {
        // TODO Auto-generated method stub
        
    }
}
