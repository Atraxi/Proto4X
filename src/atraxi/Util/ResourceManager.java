package atraxi.util;

import javax.imageio.ImageIO;
import java.awt.Image;
import java.awt.image.BufferedImage;
import java.io.IOException;

/**
 * Created by Atraxi on 3/10/2015.
 */
public class ResourceManager
{
    //TODO: pre-load this
    //TODO: throw some sort of error if the file isn't found
    //TODO: implement imageObserver or similar, and verify all resources are properly loaded
    private static BufferedImage[] images = new BufferedImage[ImageID.values().length];

    public static void resetLoadedImages()
    {
        images = new BufferedImage[ImageID.values().length];
    }

    public static Image getImage(ImageID id)
    {
        //All images will be pre-loaded, null-check is for a utility method for artists/texture packs/debugging to be able to reload textures
        if(images[id.ordinal()] == null)
        {
            try
            {
                switch (id)
                {
                    case background1A:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background1A.png"));
                        break;
                    case background1B:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background1B.png"));
                        break;
                    case background1C:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background1C.png"));
                        break;
                    case background1D:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background1D.png"));
                        break;
                    case background2A:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background2A.png"));
                        break;
                    case background2B:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background2B.png"));
                        break;
                    case background2C:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background2C.png"));
                        break;
                    case background2D:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background2D.png"));
                        break;
                    case background3A:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background3A.png"));
                        break;
                    case background3B:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background3B.png"));
                        break;
                    case background3C:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background3C.png"));
                        break;
                    case background3D:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background3D.png"));
                        break;
                    case background4A:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background4A.png"));
                        break;
                    case background4B:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background4B.png"));
                        break;
                    case background4C:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background4C.png"));
                        break;
                    case background4D:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/background4D.png"));
                        break;
                    case buttonDefault:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/baseButtonClass.png"));
                        break;
                    case buttonHover:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/baseButtonClassHover.png"));
                        break;
                    case buttonClick:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/baseButtonClassClick.png"));
                        break;
                    case menuBackground:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/baseMenuClass.png"));
                        break;
                    case entityShipDefault:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/baseShipClass.png"));
                        break;
                    case infoPanelDefault:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/baseShipClass.png"));
                        break;
                    case entityStructureDefault:
                        images[id.ordinal()] = ImageIO.read(ResourceManager.class.getResourceAsStream("/resources/baseBuildingClass.png"));
                        break;
                }
            }
            catch(IOException e)
            {
                e.printStackTrace();
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
        infoPanelDefault, entityStructureDefault
    }
}
