package atraxi.client

import atraxi.client.networking.ClientUtil
import atraxi.client.ui.UserInterfaceHandler
import atraxi.client.ui.wrappers.WorldUIWrapper
import atraxi.client.util.ResourceManager
import atraxi.core.Player
import atraxi.core.util.DebugState
import atraxi.core.util.Globals
import atraxi.core.util.Logger
import atraxi.core.world.World

import javax.swing.JFrame
import javax.swing.WindowConstants
import java.awt.AWTException
import java.awt.Dimension
import java.awt.EventQueue
import java.awt.Insets
import java.awt.Polygon
import java.awt.Robot
import java.awt.Toolkit
import java.io.IOException
import java.util.ArrayList
import java.util.Random
import java.util.stream.Collectors

class Proto private constructor() : JFrame() {

    init {
        physicalScreenSize = Toolkit.getDefaultToolkit().screenSize

        Globals.random = Random(Globals.SEED)

        val user = Player("testPlayer")

        try {
            clientUtil = ClientUtil(user)
            Thread(clientUtil, "Client").start()
        } catch (e: IOException) {
            Logger.log(Logger.LogLevel.debug, arrayOf("Failed to initialize connection to server"))
            e.printStackTrace()
            System.exit(0)
        }

    }

    fun setDimensions(width: Int, height: Int) {
        val insets = insets
        contentPane.setSize(width, height)
        setSize(width + insets.left + insets.right, height + insets.top + insets.bottom)
    }

    fun initGame(worldsFromServer: ArrayList<World>, players: ArrayList<Player>) {
        val worlds = ArrayList<WorldUIWrapper>()

        //Hexagonal - assumes regular hexagon with points at top/bottom with all points tightly bound to image dimensions, traverse points clockwise from top center
        worlds.addAll(worldsFromServer.stream().map({ world: World ->
            WorldUIWrapper(world,
                    Polygon(intArrayOf(ResourceManager.getImage(Globals.Identifiers.hexagonDefault).width / 2, ResourceManager.getImage(
                            Globals.Identifiers.hexagonDefault).width, ResourceManager.getImage(
                            Globals.Identifiers.hexagonDefault).width, ResourceManager.getImage(Globals.Identifiers.hexagonDefault).width / 2, 0, 0),
                            intArrayOf(0, ResourceManager.getImage(Globals.Identifiers.hexagonDefault).height / 4, 3 * ResourceManager.getImage(
                                    Globals.Identifiers.hexagonDefault).height / 4, ResourceManager.getImage(
                                    Globals.Identifiers.hexagonDefault).height, 3 * ResourceManager.getImage(
                                    Globals.Identifiers.hexagonDefault).height / 4, ResourceManager.getImage(Globals.Identifiers.hexagonDefault).height / 4),
                            6),
                    Globals.Identifiers.hexagonDefault, Globals.Identifiers.hexagonHover,
                    Globals.Identifiers.hexagonClick,
                    Globals.Identifiers.hexagonSelected)
        }).collect(Collectors.toList<WorldUIWrapper>()))

        val ui = UserInterfaceHandler(user, 0)

        val game = Game(ui, clientUtil, worlds, players)
        addKeyListener(ui) //Must be on frame, not sure why
        //bind mouse to JPanel not JFrame to account for taskbar in mouse coords
        game.addMouseMotionListener(ui)
        game.addMouseListener(ui)
        game.addMouseWheelListener(ui)

        //add a JPanel to this JFrame
        add(game)

        isFocusable = true
        //TODO: toggle windowed mode, add resizing and resolution options. This can probably wait for the main menu
        //setUndecorated(true);
        defaultCloseOperation = WindowConstants.EXIT_ON_CLOSE

        title = "4X_proto"
        isResizable = false
    }

    companion object {
        private val serialVersionUID = 1L
        private var physicalScreenSize: Dimension
        var proto: Proto? = null
            private set

        private val user: Player? = null
        private var clientUtil: ClientUtil? = null

        @JvmStatic fun main(args: Array<String>) {
            Globals.debug = DebugState(false, 0)
            var i = 0
            while (i < args.size) {
                when (args[i]) {
                    "-debug" -> {
                        var expandedZoom = false
                        var infoLevel = 0
                        while (i < args.size - 1 && !args[i + 1].startsWith("-")) {
                            i++
                            val splitArgs = args[i].split("=".toRegex()).dropLastWhile { it.isEmpty() }.toTypedArray()
                            when (splitArgs[0]) {
                                "expandedZoom" -> expandedZoom = java.lang.Boolean.valueOf(splitArgs[1])!!
                                "infoLevel" -> infoLevel = Integer.valueOf(splitArgs[1])!!
                                else -> Logger.log(Logger.LogLevel.warning, arrayOf("Unknown debug flag \"" + args[i] + "\""))
                            }
                        }
                        Globals.debug = DebugState(expandedZoom, infoLevel)
                        Logger.log(Logger.LogLevel.debug, arrayOf("Debug enabled, " + Globals.debug!!))
                    }
                }
                i++
            }

            EventQueue.invokeLater {
                val frame = Proto()
                frame.iconImage = ResourceManager.getImage(Globals.Identifiers.entityShipDefault)
                frame.isLocationByPlatform = true
                frame.isVisible = true
                val insets = frame.insets
                frame.contentPane.setSize((physicalScreenSize.width * 0.75).toInt(), (physicalScreenSize.height * 0.75).toInt())
                frame.setSize((physicalScreenSize.width * 0.75 + insets.left.toDouble() + insets.right.toDouble()).toInt(), (physicalScreenSize.height * 0.75 + insets.top.toDouble() + insets.bottom.toDouble()).toInt())
                Logger.log(Logger.LogLevel.info, arrayOf("screen_Width:" + frame.contentPane.width, "screen_Height:" + frame.contentPane.height))

                try {
                    val robot = Robot()
                    robot.mouseMove(frame.contentPane.width / 2 + frame.contentPane.locationOnScreen.x,
                            frame.contentPane.height / 2 + frame.contentPane.locationOnScreen.y)
                } catch (e: AWTException) {
                    //TODO: environment either doesn't support, or doesn't allow, controlling mouse input. Log this, and disable features or quit if needed
                    e.printStackTrace()
                }

                proto = frame
            }
        }

        /**
         * Get the width of the active game area (i.e. without the title bar etc)
         */
        val screenWidth: Int
            get() = proto!!.contentPane.width

        /**
         * Get the height of the active game area (i.e. without the title bar etc)
         */
        val screenHeight: Int
            get() = proto!!.contentPane.height
    }
}
