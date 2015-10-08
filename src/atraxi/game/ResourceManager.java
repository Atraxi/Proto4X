package atraxi.game;

import javax.swing.ImageIcon;
import java.awt.Image;

/**
 * Created by Atraxi on 3/10/2015.
 */
public class ResourceManager
{
    //TODO: pre-load this
    //TODO: throw some sort of error if the file isn't found
    //TODO: implement imageObserver and similar, and verify all resources are properly loaded
    private static Image[] images = new Image[ImageID.values().length];

    public static void resetLoadedImages()
    {
        images = new Image[ImageID.values().length];
    }

    public static Image getImage(ImageID id)
    {
        //All images will be pre-loaded, null-check is for a utility method for artists/texture packs/debugging to be able to reload textures
        if(images[id.ordinal()] == null)
        {
            switch (id)
            {
                case background1A:
                    images[id.ordinal()] = new ImageIcon("resources/background1A.png").getImage();
                    break;
                case background1B:
                    images[id.ordinal()] = new ImageIcon("resources/background1B.png").getImage();
                    break;
                case background1C:
                    images[id.ordinal()] = new ImageIcon("resources/background1C.png").getImage();
                    break;
                case background1D:
                    images[id.ordinal()] = new ImageIcon("resources/background1D.png").getImage();
                    break;
                case background2A:
                    images[id.ordinal()] = new ImageIcon("resources/background2A.png").getImage();
                    break;
                case background2B:
                    images[id.ordinal()] = new ImageIcon("resources/background2B.png").getImage();
                    break;
                case background2C:
                    images[id.ordinal()] = new ImageIcon("resources/background2C.png").getImage();
                    break;
                case background2D:
                    images[id.ordinal()] = new ImageIcon("resources/background2D.png").getImage();
                    break;
                case background3A:
                    images[id.ordinal()] = new ImageIcon("resources/background3A.png").getImage();
                    break;
                case background3B:
                    images[id.ordinal()] = new ImageIcon("resources/background3B.png").getImage();
                    break;
                case background3C:
                    images[id.ordinal()] = new ImageIcon("resources/background3C.png").getImage();
                    break;
                case background3D:
                    images[id.ordinal()] = new ImageIcon("resources/background3D.png").getImage();
                    break;
                case background4A:
                    images[id.ordinal()] = new ImageIcon("resources/background4A.png").getImage();
                    break;
                case background4B:
                    images[id.ordinal()] = new ImageIcon("resources/background4B.png").getImage();
                    break;
                case background4C:
                    images[id.ordinal()] = new ImageIcon("resources/background4C.png").getImage();
                    break;
                case background4D:
                    images[id.ordinal()] = new ImageIcon("resources/background4D.png").getImage();
                    break;
                case buttonDefault:
                    images[id.ordinal()] = new ImageIcon("resources/baseButtonClass.png").getImage();
                    break;
                case buttonHover:
                    images[id.ordinal()] = new ImageIcon("resources/baseButtonClassHover.png").getImage();
                    break;
                case buttonClick:
                    images[id.ordinal()] = new ImageIcon("resources/baseButtonClassClick.png").getImage();
                    break;
                case menuBackground:
                    images[id.ordinal()] = new ImageIcon("resources/baseMenuClass.png").getImage();
                    break;
                case entityShipDefault:
                    images[id.ordinal()] = new ImageIcon("resources/baseShipClass.png").getImage();
                    break;
                case entityStructureDefault:
                    images[id.ordinal()] = new ImageIcon("resources/baseBuildingClass.png").getImage();
                    break;
            }
        }
        return images[id.ordinal()];
    }

    public enum ImageID
    {
        background1A,background1B,background1C,background1D,
        background2A,background2B,background2C,background2D,
        background3A,background3B,background3C,background3D,
        background4A,background4B,background4C,background4D,
        buttonDefault,buttonHover,buttonClick,
        menuBackground,
        entityShipDefault,
        entityStructureDefault
    }
}
