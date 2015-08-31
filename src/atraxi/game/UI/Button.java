package atraxi.game.UI;

import java.awt.Image;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.image.ImageObserver;
import java.util.concurrent.Callable;

public class Button implements ImageObserver
{
    private Image image, imageHover, imagePressed;
    private int x, y;
     Rectangle dim;
    protected ButtonState state = ButtonState.DEFAULT;
    private Callable action;

    public Button(Image image, Image imageHover, Image imagePressed,int x,int y, Callable action)
    {
        this.image = image;
        this.imageHover = imageHover;
        this.imagePressed = imagePressed;
        this.x=x;
        this.y=y;
        this.action=action;
        int width = image.getWidth(this);
        int height = image.getHeight(this);
        if(width!=-1 && height!=-1)
        {
            dim=new Rectangle(x,y,width,height);
        }
        else
        {
            dim=new Rectangle(x,y,0,0);
        }
    }

    public Button (Image image, int x, int y,Callable action)
    {
        this(image,image,image,x,y,action);
    }

    public Image getImage ()
    {
        switch (state)
        {
            case HOVER:
                return imageHover;
            case PRESSED:
                return imagePressed;
            default:
                return image;
        }
    }

    protected int getX ()
    {
        return x;
    }

    protected int getY ()
    {
        return y;
    }

    protected void executeAction() throws Exception
    {
        action.call();
    }

    @Override
    public boolean imageUpdate (Image img, int infoflags, int x, int y, int width, int height)
    {
        //TODO: image observer
        return false;
    }

    protected enum ButtonState
    {
        DEFAULT, HOVER, PRESSED
    }
}