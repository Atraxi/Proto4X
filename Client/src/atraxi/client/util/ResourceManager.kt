package atraxi.client.util

import atraxi.core.util.Globals
import atraxi.core.util.Logger

import javax.imageio.ImageIO
import java.awt.image.BufferedImage
import java.io.File
import java.io.FileInputStream
import java.io.FileNotFoundException
import java.io.IOException
import java.io.InputStream

/**
 * Created by Atraxi on 3/10/2015.
 */
object ResourceManager {
    //TODO: pre-load this - is this even needed? keep watching it as size increases
    //TODO: throw some sort of managed error if the file isn't found
    //TODO: implement imageObserver or similar, and verify all resources are properly loaded - done already via null check loading?
    private var images = arrayOfNulls<BufferedImage>(Globals.Identifiers.values().size)

    fun resetLoadedImages() {
        images = arrayOfNulls<BufferedImage>(Globals.Identifiers.values().size)
    }

    fun getImage(id: Globals.Identifiers): BufferedImage {
        //All images will be pre-loaded, null-check is for a utility method for artists/texture packs/debugging to be able to reload textures
        if (images[id.ordinal] == null) {
            try {
                when (id) {
                    Globals.Identifiers.background1A -> images[id.ordinal] = loadImage("background1A.png")
                    Globals.Identifiers.background1B -> images[id.ordinal] = loadImage("background1B.png")
                    Globals.Identifiers.background1C -> images[id.ordinal] = loadImage("background1C.png")
                    Globals.Identifiers.background1D -> images[id.ordinal] = loadImage("background1D.png")
                    Globals.Identifiers.background2A -> images[id.ordinal] = loadImage("background2A.png")
                    Globals.Identifiers.background2B -> images[id.ordinal] = loadImage("background2B.png")
                    Globals.Identifiers.background2C -> images[id.ordinal] = loadImage("background2C.png")
                    Globals.Identifiers.background2D -> images[id.ordinal] = loadImage("background2D.png")
                    Globals.Identifiers.background3A -> images[id.ordinal] = loadImage("background3A.png")
                    Globals.Identifiers.background3B -> images[id.ordinal] = loadImage("background3B.png")
                    Globals.Identifiers.background3C -> images[id.ordinal] = loadImage("background3C.png")
                    Globals.Identifiers.background3D -> images[id.ordinal] = loadImage("background3D.png")
                    Globals.Identifiers.background4A -> images[id.ordinal] = loadImage("background4A.png")
                    Globals.Identifiers.background4B -> images[id.ordinal] = loadImage("background4B.png")
                    Globals.Identifiers.background4C -> images[id.ordinal] = loadImage("background4C.png")
                    Globals.Identifiers.background4D -> images[id.ordinal] = loadImage("background4D.png")
                    Globals.Identifiers.buttonDefault -> images[id.ordinal] = loadImage("baseButtonClass.png")
                    Globals.Identifiers.buttonHover -> images[id.ordinal] = loadImage("baseButtonClassHover.png")
                    Globals.Identifiers.buttonClick -> images[id.ordinal] = loadImage("baseButtonClassClick.png")
                    Globals.Identifiers.menuBackground -> images[id.ordinal] = loadImage("baseMenuClass.png")
                    Globals.Identifiers.entityShipDefault -> images[id.ordinal] = loadImage("baseShipClass.png")
                    Globals.Identifiers.infoPanelDefault -> images[id.ordinal] = loadImage("baseShipClass.png")
                    Globals.Identifiers.entityStructureDefault -> images[id.ordinal] = loadImage("baseBuildingClass.png")
                    Globals.Identifiers.gridDefault -> images[id.ordinal] = loadImage("gridDefault.png")
                    Globals.Identifiers.gridHover -> images[id.ordinal] = loadImage("gridHover.png")
                    Globals.Identifiers.gridClick -> images[id.ordinal] = loadImage("gridClick.png")
                    Globals.Identifiers.gridSelected -> images[id.ordinal] = loadImage("gridSelected.png")
                    Globals.Identifiers.hexagonClick -> images[id.ordinal] = loadImage("hexagonClick.png")
                    Globals.Identifiers.hexagonHover -> images[id.ordinal] = loadImage("hexagonHover.png")
                    Globals.Identifiers.hexagonDefault -> images[id.ordinal] = loadImage("hexagonDefault.png")
                    Globals.Identifiers.hexagonSelected -> images[id.ordinal] = loadImage("hexagonSelected.png")
                }
            } catch (e: IOException) {
                e.printStackTrace()
            }

        }
        return images[id.ordinal]
    }

    @Throws(IOException::class)
    private fun loadImage(imageName: String): BufferedImage {
        val inputStream = getStreamForResource(imageName)
        val image = ImageIO.read(inputStream)
        inputStream.close()
        return image
    }

    @Throws(FileNotFoundException::class)
    private fun getStreamForResource(resourceName: String): InputStream {
        val file = File("resources" + File.separator + resourceName)
        if (file.canRead()) {
            return FileInputStream(File("resources" + File.separator + resourceName))
        } else {
            Logger.log(Logger.LogLevel.warning, arrayOf("Unable to find resource $resourceName, using default.", "\tSearched at: " + file.absolutePath))
            return ResourceManager::class.java.getResourceAsStream("/resources/" + resourceName)
        }
    }
}
