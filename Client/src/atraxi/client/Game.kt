package atraxi.client

import atraxi.client.networking.ClientUtil
import atraxi.client.ui.UserInterfaceHandler
import atraxi.client.ui.wrappers.WorldUIWrapper
import atraxi.client.util.RenderUtil
import atraxi.core.Player
import atraxi.core.util.Globals
import atraxi.core.util.Logger
import org.json.JSONObject

import javax.swing.JPanel
import java.awt.Graphics
import java.awt.Graphics2D
import java.awt.RenderingHints
import java.awt.Toolkit
import java.awt.geom.AffineTransform
import java.io.IOException
import java.math.BigDecimal
import java.util.ArrayList

class Game(uiHandler: UserInterfaceHandler, clientUtil: ClientUtil, worlds: ArrayList<WorldUIWrapper>, players: ArrayList<Player>) : JPanel(), Runnable {

    init {
        Game.uiHandler = uiHandler
        Game.clientUtil = clientUtil
        Game.worlds = worlds
        Globals.players.addAll(players)
        isDoubleBuffered = true
        paused = true
        renderUtil = RenderUtil()
    }

    override fun paint(g: Graphics?) {
        super.paint(g)
        val g2d = g as Graphics2D

        g2d.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON)
        g2d.setRenderingHint(RenderingHints.KEY_RENDERING, RenderingHints.VALUE_RENDER_QUALITY)

        renderUtil.setG2d(g2d)

        if (Globals.debug!!.isExpandedZoomEnabled) {//zoom out, show a box where the edge of the screen would normally be
            g2d.scale(0.75, 0.75)
            g2d.translate(250, 100)
            g2d.drawRect(0, 0, width, height)
        }

        //The background doesn't move at the same speed as the rest of the game objects, due to the desired parallax illusion,
        // so the camera offset is managed inside it's paint method
        //        uiHandler.paintBackground(g2d);
        //        uiHandler.paintBackgroundAtScrollSpeed(1, g2d, renderUtil);

        //Transform camera position/scale/rotation to draw any world objects
        val g2dTransformBackup = g2d.transform
        g2d.transform(UserInterfaceHandler.worldTransform)

        UserInterfaceHandler.paintWorld(renderUtil)

        //Remove/reset camera transform to draw UI
        g2d.transform = g2dTransformBackup

        UserInterfaceHandler.paintScreen(renderUtil, hasTurnEnded)

        //        new InfoPanel(new Rectangle(40, 40, ImageID.infoPanelDefault.getImage().getWidth(null)+200, ImageID.infoPanelDefault.getImage().getHeight(null)+200),
        //                      ImageID.infoPanelDefault,0,0,0).paint(renderUtil);

        Toolkit.getDefaultToolkit().sync()
        g2d.dispose()
    }

    private fun gameLoop(timeAdjustment: BigDecimal) {
        //        for(World world : worlds)
        //        {
        //            for(Entity entity : world.getEntityList())
        //            {
        //                entity.doWork(timeAdjustment, paused);
        //            }
        //        }
        UserInterfaceHandler.doWork(timeAdjustment, paused)
    }

    override fun addNotify() {
        super.addNotify()

        val animator = Thread(this)
        animator.start()
    }

    override fun run() {
        var beforeTime: Long
        var actualFrameTime: Long

        beforeTime = System.nanoTime()

        while (true) {
            val currentTime = System.nanoTime()

            actualFrameTime = currentTime - beforeTime

            if (actualFrameTime > OPTIMALFRAMETIME) {
                Logger.log(Logger.LogLevel.warning, arrayOf("Game running behind, skipping " + (actualFrameTime / OPTIMALFRAMETIME).toInt() + " frames"))
            }
            if (actualFrameTime < MINIMUMFRAMETIME) {
                gameLoop(BigDecimal.valueOf(MINIMUMFRAMETIME).divide(BigDecimal.valueOf(OPTIMALFRAMETIME), 8, BigDecimal.ROUND_DOWN))
                repaint()

                beforeTime = currentTime

                try {
                    //Limit framerate to 120fps
                    Thread.sleep((MINIMUMFRAMETIME - actualFrameTime) / 1000000)
                } catch (e: InterruptedException) {//TODO: handle this
                    e.printStackTrace()
                }

            } else {
                gameLoop(BigDecimal.valueOf(actualFrameTime).divide(BigDecimal.valueOf(OPTIMALFRAMETIME),
                        8,
                        BigDecimal.ROUND_DOWN))
                repaint()

                beforeTime = currentTime
            }
        }
    }

    companion object {
        private val serialVersionUID = 1L
        /**
         * The desired time per frame (in nanoseconds) from which to adjust the speed of all events. 60fps or 1,000,000,000ns/60
         */
        val OPTIMALFRAMETIME: Long = 16666666
        private val MINIMUMFRAMETIME: Long = 8333333
        private var worlds: ArrayList<WorldUIWrapper>? = null
        var paused: Boolean = false
        private var uiHandler: UserInterfaceHandler? = null
        private var renderUtil: RenderUtil
        var clientUtil: ClientUtil? = null
            private set
        private var hasTurnEnded: Boolean = false

        fun getWorld(index: Int): WorldUIWrapper {
            return worlds!![index]
        }

        val worldCount: Int
            get() = worlds!!.size

        fun endTurn() {
            hasTurnEnded = true

            val jsonObject = JSONObject()
            jsonObject.put(Globals.JSON_KEY_MessageType, Globals.JSON_VALUE_MessageType_EndTurn)
            try {
                clientUtil!!.sendToServer(jsonObject)
            } catch (e: IOException) {
                e.printStackTrace()
            }

        }
    }
}