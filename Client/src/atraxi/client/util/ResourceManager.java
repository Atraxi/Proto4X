package atraxi.client.util;

import atraxi.core.util.Globals;
import atraxi.core.util.Logger;

import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;

/**
 * Created by Atraxi on 3/10/2015.
 */
public class ResourceManager
{
    //TODO: pre-load this - is this even needed? keep watching it as size increases
    //TODO: throw some sort of managed error if the file isn't found
    //TODO: implement imageObserver or similar, and verify all resources are properly loaded - done already via null check loading?
    private static BufferedImage[] images = new BufferedImage[Globals.Identifiers.values().length];

    public static void resetLoadedImages()
    {
        images = new BufferedImage[Globals.Identifiers.values().length];
    }

    public static BufferedImage getImage(Globals.Identifiers id)
    {
        //All images will be pre-loaded, null-check is for a utility method for artists/texture packs/debugging to be able to reload textures
        if(images[id.ordinal()] == null)
        {
            try
            {
                switch (id)
                {
                    case background1A:
                        images[id.ordinal()] = loadImage("background1A.png");
                        break;
                    case background1B:
                        images[id.ordinal()] = loadImage("background1B.png");
                        break;
                    case background1C:
                        images[id.ordinal()] = loadImage("background1C.png");
                        break;
                    case background1D:
                        images[id.ordinal()] = loadImage("background1D.png");
                        break;
                    case background2A:
                        images[id.ordinal()] = loadImage("background2A.png");
                        break;
                    case background2B:
                        images[id.ordinal()] = loadImage("background2B.png");
                        break;
                    case background2C:
                        images[id.ordinal()] = loadImage("background2C.png");
                        break;
                    case background2D:
                        images[id.ordinal()] = loadImage("background2D.png");
                        break;
                    case background3A:
                        images[id.ordinal()] = loadImage("background3A.png");
                        break;
                    case background3B:
                        images[id.ordinal()] = loadImage("background3B.png");
                        break;
                    case background3C:
                        images[id.ordinal()] = loadImage("background3C.png");
                        break;
                    case background3D:
                        images[id.ordinal()] = loadImage("background3D.png");
                        break;
                    case background4A:
                        images[id.ordinal()] = loadImage("background4A.png");
                        break;
                    case background4B:
                        images[id.ordinal()] = loadImage("background4B.png");
                        break;
                    case background4C:
                        images[id.ordinal()] = loadImage("background4C.png");
                        break;
                    case background4D:
                        images[id.ordinal()] = loadImage("background4D.png");
                        break;
                    case buttonDefault:
                        images[id.ordinal()] = loadImage("baseButtonClass.png");
                        break;
                    case buttonHover:
                        images[id.ordinal()] = loadImage("baseButtonClassHover.png");
                        break;
                    case buttonClick:
                        images[id.ordinal()] = loadImage("baseButtonClassClick.png");
                        break;
                    case menuBackground:
                        images[id.ordinal()] = loadImage("baseMenuClass.png");
                        break;
                    case entityShipDefault:
                        images[id.ordinal()] = loadImage("baseShipClass.png");
                        break;
                    case infoPanelDefault:
                        images[id.ordinal()] = loadImage("baseShipClass.png");
                        break;
                    case entityStructureDefault:
                        images[id.ordinal()] = loadImage("baseBuildingClass.png");
                        break;
                    case gridDefault:
                        images[id.ordinal()] = loadImage("gridDefault.png");
                        break;
                    case gridHover:
                        images[id.ordinal()] = loadImage("gridHover.png");
                        break;
                    case gridClick:
                        images[id.ordinal()] = loadImage("gridClick.png");
                        break;
                    case gridSelected:
                        images[id.ordinal()] = loadImage("gridSelected.png");
                        break;
                    case hexagonClick:
                        images[id.ordinal()] = loadImage("hexagonClick.png");
                        break;
                    case hexagonHover:
                        images[id.ordinal()] = loadImage("hexagonHover.png");
                        break;
                    case hexagonDefault:
                        images[id.ordinal()] = loadImage("hexagonDefault.png");
                        break;
                    case hexagonSelected:
                        images[id.ordinal()] = loadImage("hexagonSelected.png");
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

    private static BufferedImage loadImage(String imageName) throws IOException
    {
        InputStream inputStream = getStreamForResource(imageName);
        BufferedImage image = ImageIO.read(inputStream);
        inputStream.close();
        return image;
    }

    private static InputStream getStreamForResource(String resourceName) throws FileNotFoundException
    {
        File file = new File("resources" + File.separator + resourceName);
        if(file.canRead())
        {
            return new FileInputStream(new File("resources" + File.separator + resourceName));
        }
        else
        {
            Logger.log(Logger.LogLevel.warning, new String[]{"Unable to find resource " + resourceName + ", using default.", "\tSearched at: " + file.getAbsolutePath()});
            return ResourceManager.class.getResourceAsStream("/resources/" + resourceName);
        }
    }
}
