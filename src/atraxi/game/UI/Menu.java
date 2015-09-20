package atraxi.game.UI;

import java.awt.Graphics2D;
import java.awt.Image;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;
import java.awt.image.ImageObserver;

public class Menu implements UIElement, UIStackNode, ImageObserver
{
    //TODO: implement image scaling, try "image.getScaledInstance(width,height,algorithm(enum));"
    private Button[] buttons;
    private int x, y;
    private Image menuBackground;
    private Rectangle dim;
    private UIStackNode nextNode = null, previousNode = null;
    private Point movePoint = null;

    public Menu(Image image,int x, int y,Button[] buttons)
    {
        this.menuBackground=image;
        this.buttons = buttons;


        int width = menuBackground.getWidth(this);
        int height = menuBackground.getHeight(this);
        if(width!=-1 && height!=-1)
        {
            this.x=x-(width/2);
            this.y=y-(height/2);
            dim=new Rectangle(this.x,this.y,width,height);

            for(Button button : buttons)
            {
                button.x+=this.x;
                button.y+=this.y;
                button.dim.setLocation(button.x,button.y);
                button.parentMenu = this;
            }
        }
        else
        {
            this.x=x;
            this.y=y;
            dim=new Rectangle(this.x,this.y,0,0);

            for(Button button : buttons)
            {
                button.x+=this.x;
                button.y+=this.y;
                button.dim.setLocation(button.x,button.y);
            }
        }
    }

    public void paint (Graphics2D g2d)
    {
        g2d.drawImage(menuBackground, x, y, null);
        for(Button button : buttons)
        {
            g2d.drawImage(button.getImage(), button.x, button.y, null);
        }
    }

    @Override
    public UIElement mouseEntered (MouseEvent paramMouseEvent){return null;}

    @Override
    public UIElement mouseExited (MouseEvent paramMouseEvent) {return null;}

    @Override
    public UIElement mousePressed (MouseEvent paramMouseEvent)
    {
        if(dim.contains(paramMouseEvent.getPoint()))
        {
            for(Button button : buttons)
            {
                if(button.dim.contains(paramMouseEvent.getPoint()))
                {
                    button.state = Button.ButtonState.PRESSED;
                    return this;
                }
            }
            if(paramMouseEvent.getButton() == MouseEvent.BUTTON1)
            {
                movePoint = paramMouseEvent.getPoint();
                return this;
            }
        }
        return null;
    }

    @Override
    public UIElement mouseReleased (MouseEvent paramMouseEvent)
    {
        if(movePoint != null)
        {
            int width = menuBackground.getWidth(this);
            int height = menuBackground.getHeight(this);
            this.x-=(movePoint.getX()-paramMouseEvent.getX());
            this.y-=(movePoint.getY()-paramMouseEvent.getY());
            dim=new Rectangle(this.x,this.y,width,height);

            for(Button button : buttons)
            {
                button.x-=(movePoint.getX()-paramMouseEvent.getX());
                button.y-=(movePoint.getY()-paramMouseEvent.getY());
                button.dim.setLocation(button.x,button.y);
            }
            movePoint=null;
            return this;
        }
        for(Button button : buttons)
        {
            if(button.state == Button.ButtonState.PRESSED)
            {
                if(button.dim.contains(paramMouseEvent.getPoint()))
                {
                    try
                    {
                        button.executeAction();
                    }
                    catch(Exception e)
                    {
                        //TODO: Handle this properly, although no exceptions are thrown explicitly (as of writing this comment), and all other exceptions should be handled internally so it's unlikely to actually occur
                        e.printStackTrace();
                    }
                    button.state = Button.ButtonState.HOVER;
                    return this;
                }
                else
                {
                    button.state = Button.ButtonState.DEFAULT;
                }
            }
        }
        if(dim.contains(paramMouseEvent.getPoint()))
        {
            return this;
        }
        else
        {
            return null;
        }
    }

    @Override
    public UIElement mouseDragged (MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseMoved (MouseEvent paramMouseEvent)
    {
        if(dim.contains(paramMouseEvent.getPoint()))
        {
            for(Button button : buttons)
            {
                if(button.dim.contains(paramMouseEvent.getPoint()) && button.state == Button.ButtonState.DEFAULT)
                {
                    System.out.println("mouse moved over button");
                    button.state = Button.ButtonState.HOVER;
                    return this;
                }
                else if(!button.dim.contains(paramMouseEvent.getPoint()) && button.state == Button.ButtonState.HOVER)
                {
                    System.out.println("mouse moved off button");
                    button.state = Button.ButtonState.DEFAULT;
                }
            }
            return this;
        }
        return null;
    }

    @Override
    public UIElement mouseWheelMoved (MouseWheelEvent paramMouseWheelEvent)
    {
        if(dim.contains(paramMouseWheelEvent.getPoint()))
        {
            return this;
        }
        return null;
    }

    @Override
    public UIStackNode getNextNode ()
    {
        return nextNode;
    }

    @Override
    public UIStackNode getPreviousNode ()
    {
        return previousNode;
    }

    @Override
    public void setNextNode (UIStackNode element)
    {
        nextNode = element;
    }

    @Override
    public void setPreviousNode (UIStackNode element)
    {
        previousNode = element;
    }

    @Override
    public boolean imageUpdate (Image img, int infoFlags, int x, int y, int width, int height)
    {
        if(((ImageObserver.WIDTH | ImageObserver.HEIGHT) & infoFlags)  == (ImageObserver.WIDTH | ImageObserver.HEIGHT))
        {
            this.x=x-(width/2);
            this.y=y-(height/2);
            dim = new Rectangle(this.x,this.y,width,height);

            for(Button button : buttons)
            {
                button.x+=this.x;
                button.y+=this.y;
                button.dim.setLocation(button.x,button.y);
            }
            return true;
        }
        else
        {
            return false;
        }
    }
}
