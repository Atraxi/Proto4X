package atraxi.util;

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
    //TODO: pre-load this
    //TODO: throw some sort of error if the file isn't found
    //TODO: implement imageObserver or similar, and verify all resources are properly loaded
    private static BufferedImage[] images = new BufferedImage[ImageID.values().length];

    public static void resetLoadedImages()
    {
        images = new BufferedImage[ImageID.values().length];
    }

    private static BufferedImage getImage(ImageID id)
    {
        //All images will be pre-loaded, null-check is for a utility method for artists/texture packs/debugging to be able to reload textures
        if(images[id.ordinal()] == null)
        {
            try
            {
                switch (id)
                {
                    case background1A:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background1A.png"));
                        break;
                    case background1B:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background1B.png"));
                        break;
                    case background1C:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background1C.png"));
                        break;
                    case background1D:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background1D.png"));
                        break;
                    case background2A:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background2A.png"));
                        break;
                    case background2B:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background2B.png"));
                        break;
                    case background2C:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background2C.png"));
                        break;
                    case background2D:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background2D.png"));
                        break;
                    case background3A:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background3A.png"));
                        break;
                    case background3B:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background3B.png"));
                        break;
                    case background3C:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background3C.png"));
                        break;
                    case background3D:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background3D.png"));
                        break;
                    case background4A:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background4A.png"));
                        break;
                    case background4B:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background4B.png"));
                        break;
                    case background4C:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background4C.png"));
                        break;
                    case background4D:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("background4D.png"));
                        break;
                    case buttonDefault:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("baseButtonClass.png"));
                        break;
                    case buttonHover:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("baseButtonClassHover.png"));
                        break;
                    case buttonClick:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("baseButtonClassClick.png"));
                        break;
                    case menuBackground:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("baseMenuClass.png"));
                        break;
                    case entityShipDefault:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("baseShipClass.png"));
                        break;
                    case infoPanelDefault:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("baseShipClass.png"));
                        break;
                    case entityStructureDefault:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("baseBuildingClass.png"));
                        break;
                    case gridDefault:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("gridDefault.png"));
                        break;
                    case gridHover:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("gridHover.png"));
                        break;
                    case gridClick:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("gridClick.png"));
                        break;
                    case gridSelected:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("gridSelected.png"));
                        break;
                    case hexagonClick:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("hexagonClick.png"));
                        break;
                    case hexagonHover:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("hexagonHover.png"));
                        break;
                    case hexagonDefault:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("hexagonDefault.png"));
                        break;
                    case hexagonSelected:
                        images[id.ordinal()] = ImageIO.read(getStreamForResource("hexagonSelected.png"));
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

    private static InputStream getStreamForResource(String resourceName) throws FileNotFoundException
    {
        File file = new File("resources\\" + resourceName);
        if(file.canRead())
        {
            return new FileInputStream(new File("resources\\" + resourceName));
        }
        else
        {
            Logger.log(Logger.LogLevel.warning, new String[]{"Unable to find resource " + resourceName + ", using default.", "Checked at: " + file.getAbsolutePath()});
            return ResourceManager.class.getResourceAsStream("/resources/" + resourceName);
        }
    }

    /**
     * Allows access to the {@link BufferedImage} represented by this enum via {@link ImageID#getImage()}.
     */
    public enum ImageID
    {
        background1A,background1B,background1C,background1D,
        background2A,background2B,background2C,background2D,
        background3A,background3B,background3C,background3D,
        background4A,background4B,background4C,background4D,
        buttonDefault,buttonHover,buttonClick,
        menuBackground,
        entityShipDefault,
        infoPanelDefault, entityStructureDefault,
        gridClick, gridHover, gridDefault, gridSelected,
        hexagonClick, hexagonHover, hexagonDefault, hexagonSelected;

        /**
         * @return The {@link BufferedImage} represented by this enum.
         */
        public BufferedImage getImage()
        {
            return ResourceManager.getImage(this);
        }
    }
}
