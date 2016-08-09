package atraxi.client.ui

import atraxi.client.Game
import atraxi.client.Proto
import atraxi.client.ui.wrappers.WorldUIWrapper
import atraxi.client.util.RenderUtil
import atraxi.client.util.ResourceManager
import atraxi.core.Player
import atraxi.core.entities.Entity
import atraxi.core.entities.action.ActionMoveTestImpl
import atraxi.core.entities.action.definitions.Action
import atraxi.core.util.Globals
import atraxi.core.util.Logger
import atraxi.core.world.World

import java.awt.Graphics2D
import java.awt.Point
import java.awt.Rectangle
import java.awt.event.KeyEvent
import java.awt.event.KeyListener
import java.awt.event.MouseEvent
import java.awt.event.MouseListener
import java.awt.event.MouseMotionListener
import java.awt.event.MouseWheelEvent
import java.awt.event.MouseWheelListener
import java.awt.geom.AffineTransform
import java.awt.geom.NoninvertibleTransformException
import java.io.IOException
import java.math.BigDecimal
import java.util.Random

class UserInterfaceHandler(user: Player, defaultWorldIndex: Int) : KeyListener, MouseListener, MouseWheelListener, MouseMotionListener {

    init {
        UserInterfaceHandler.user = user

        currentWorldIndex = defaultWorldIndex
    }

    /**
     * Draw a layer of background tiles with a set scroll speed. Allows for a parallel illusion with multiple layers at varying speeds.
     * @param speed
     */
    private fun paintBackgroundAtScrollSpeed(speed: Float, graphics2D: Graphics2D) {
        //for()
    }

    /**
     * Paint the background layer, everything else will draw above this
     * @param g2d
     */
    fun paintBackground(g2d: Graphics2D) {
        val viewArea = doubleArrayOf(0.0, 0.0, Proto.screenWidth.toDouble(), 0.0, Proto.screenWidth.toDouble(), Proto.screenHeight.toDouble(), 0.0, Proto.screenHeight.toDouble())

        try {
            worldTransform.inverseTransform(viewArea, 0, viewArea, 0, 4)
            val indexes = arrayOf(World.convertAxialToOffset(WorldUIWrapper.getGridTileIndexFromPixelLocation(viewArea[0].toInt(), viewArea[1].toInt(), Game.getWorld(currentWorldIndex))), World.convertAxialToOffset(WorldUIWrapper.getGridTileIndexFromPixelLocation(viewArea[2].toInt(), viewArea[3].toInt(), Game.getWorld(currentWorldIndex))), World.convertAxialToOffset(WorldUIWrapper.getGridTileIndexFromPixelLocation(viewArea[4].toInt(), viewArea[5].toInt(), Game.getWorld(currentWorldIndex))), World.convertAxialToOffset(WorldUIWrapper.getGridTileIndexFromPixelLocation(viewArea[6].toInt(), viewArea[7].toInt(), Game.getWorld(currentWorldIndex))))
            val xMin = Math.min(indexes[0].x, Math.min(indexes[1].x, Math.min(indexes[2].x, indexes[3].x)))
            val xMax = Math.max(indexes[0].x, Math.max(indexes[1].x, Math.max(indexes[2].x, indexes[3].x)))

            val yMin = Math.min(indexes[0].y, Math.min(indexes[1].y, Math.min(indexes[2].y, indexes[3].y)))
            val yMax = Math.max(indexes[0].y, Math.max(indexes[1].y, Math.max(indexes[2].y, indexes[3].y)))

            var i = 0
            while (i < 10) {
                paintBackgroundAtScrollSpeed(i.toFloat(), g2d)
                i = i * 2
            }
        } catch (e: NoninvertibleTransformException) {
            Logger.log(Logger.LogLevel.error, arrayOf("Camera has entered an invalid state, resetting."))
            e.printStackTrace()
            //TODO: save current worldTransform for error reporting
            worldTransform.setToIdentity()
        }

    }

    override fun keyReleased(paramKeyEvent: KeyEvent) {
    }

    override fun keyPressed(paramKeyEvent: KeyEvent) {
        when (paramKeyEvent.keyCode) {
            KeyEvent.VK_B -> if (selectedLocation != null) {
                //selectedLocation.queueAction(new ActionBuild(null));
                Logger.log(Logger.LogLevel.debug, arrayOf("build temporarily disabled"))
            }
            KeyEvent.VK_PAUSE -> {
                Game.paused = !Game.paused
                if (Game.paused) {
                    Logger.log(Logger.LogLevel.debug, arrayOf("Game Paused."))
                } else {
                    Logger.log(Logger.LogLevel.debug, arrayOf("Game Resumed."))
                }
            }
            KeyEvent.VK_ESCAPE -> {
                Logger.log(Logger.LogLevel.debug, arrayOf("escape"))
                //TODO: a proper 'quit' method to save, prompt user etc
                System.exit(0)
            }
            KeyEvent.VK_A -> {
                Logger.log(Logger.LogLevel.debug, arrayOf("Creating test menu"))
                uiStack.push(UIStack.newTestMenu)
            }
            KeyEvent.VK_SPACE -> {
                isScrollEnabled = !isScrollEnabled
                if (isScrollEnabled) {
                    Logger.log(Logger.LogLevel.debug, arrayOf("Camera pan enabled"))
                } else {
                    Logger.log(Logger.LogLevel.debug, arrayOf("Camera pan disabled"))
                }
            }
            KeyEvent.VK_R -> {
                Logger.log(Logger.LogLevel.info, arrayOf("Reloading all resource files"))
                ResourceManager.resetLoadedImages()
            }
            KeyEvent.VK_ENTER -> Game.endTurn()
            KeyEvent.VK_P -> worldTransform.rotate(Math.PI / 16)
        }//            case KeyEvent.VK_0:
        //                Logger.log(LogLevel.debug, new String[]{"Starting multiplayer atraxi.server thread"});
        //                serverUtil = new ServerUtil(2);
        //                Thread serverThread = new Thread(serverUtil);
        //                serverThread.setName("Server");
        //                serverThread.start();
        //                break;
        //            case KeyEvent.VK_9:
        //                Logger.log(LogLevel.debug, new String[]{"Starting multiplayer atraxi.client thread"});
        //                try
        //                {
        //                    clientUtil = new ClientUtil(user);
        //                    Thread clientThread = new Thread(clientUtil);
        //                    clientThread.setName("Client");
        //                    clientThread.start();
        //                }
        //                catch(ConnectException e)
        //                {
        //                    Logger.log(LogLevel.debug, new String[]{"Failed to connect to the atraxi.server"});
        //                    e.printStackTrace();
        //                }
        //                catch(IOException e)
        //                {
        //                    Logger.log(LogLevel.debug, new String[]{"error"});
        //                    e.printStackTrace();
        //                }
        //                break;
        //            case KeyEvent.VK_8:
        //                Logger.log(LogLevel.debug, new String[]{"Attempt sending atraxi.client->atraxi.server"});
        //                try
        //                {
        //                    JSONObject jsonObject = new JSONObject();
        //                    jsonObject.append("message", "atraxi.client->atraxi.server test");
        //                    clientUtil.sendToServer(jsonObject);
        //                }
        //                catch(IOException e)
        //                {
        //                    e.printStackTrace();
        //                }
        //                break;
        //            case KeyEvent.VK_7:
        //                Logger.log(LogLevel.debug, new String[]{"Attempt sending atraxi.server->atraxi.client"});
        //                JSONObject jsonObject = new JSONObject();
        //                jsonObject.append("message", "atraxi.server->atraxi.client test");
        //                serverUtil.sendToPlayer(user, jsonObject);
        //                break;
    }

    override fun keyTyped(paramKeyEvent: KeyEvent) {
    }

    override fun mousePressed(paramMouseEvent: MouseEvent) {
        val isUIEventHandled = uiStack.mousePressed(paramMouseEvent) != null

        //if this event was intercepted by overlaid UI elements
        if (!isUIEventHandled) {
            //Convert the mouse coordinates from screen to world coordinates
            transformMouseEvent(paramMouseEvent, worldTransform)

            Game.getWorld(currentWorldIndex).mousePressed(paramMouseEvent)
        }
    }

    override fun mouseDragged(paramMouseEvent: MouseEvent) {
        //See doWork() for usage, store coords for camera pan
        mouseLocation = paramMouseEvent.point

        //if this event was intercepted by overlaid UI elements
        val isUIEventHandled = uiStack.mouseDragged(paramMouseEvent) != null

        if (!isUIEventHandled) {
            //Convert the mouse coordinates from screen to world coordinates
            transformMouseEvent(paramMouseEvent, worldTransform)

            Game.getWorld(currentWorldIndex).mouseDragged(paramMouseEvent)
        }
    }

    override fun mouseMoved(paramMouseEvent: MouseEvent) {
        //See doWork() for usage, store coords for camera pan
        mouseLocation = paramMouseEvent.point

        //if this event was intercepted by overlaid UI elements
        val isUIEventHandled = uiStack.mouseMoved(paramMouseEvent) != null

        if (!isUIEventHandled) {
            //Convert the mouse coordinates from screen to world coordinates
            transformMouseEvent(paramMouseEvent, worldTransform)

            Game.getWorld(currentWorldIndex).mouseMoved(paramMouseEvent)
        }
    }

    override fun mouseWheelMoved(paramMouseWheelEvent: MouseWheelEvent) {
        val isUIEventHandled = uiStack.mouseWheelMoved(paramMouseWheelEvent) != null
        //if this event was intercepted by overlaid UI elements
        if (!isUIEventHandled) {
            val scaleFactor = if (paramMouseWheelEvent.wheelRotation > 0.0)
                0.9 * paramMouseWheelEvent.wheelRotation
            else
                10.0 / 9.0 * -paramMouseWheelEvent.preciseWheelRotation

            if (worldTransform.scaleX < 2 && worldTransform.scaleY < 2 && scaleFactor > 1 || worldTransform.scaleX > 0.5 && worldTransform.scaleY > 0.5 && scaleFactor < 1) {
                worldTransform.translate(paramMouseWheelEvent.x.toDouble(), paramMouseWheelEvent.y.toDouble())
                worldTransform.scale(scaleFactor, scaleFactor)
                worldTransform.translate((-paramMouseWheelEvent.x).toDouble(), (-paramMouseWheelEvent.y).toDouble())
            }
            //Convert the mouse coordinates from screen to world coordinates
            transformMouseEvent(paramMouseWheelEvent, worldTransform)
            Game.getWorld(currentWorldIndex).mouseWheelMoved(paramMouseWheelEvent)
        }
    }

    override //Do not use, partially broken implementation. Moving the mouse between press and release will prevent the event firing
    fun mouseClicked(paramMouseEvent: MouseEvent) {
    }

    override fun mouseReleased(paramMouseEvent: MouseEvent) {
        val isUIEventHandled = uiStack.mouseReleased(paramMouseEvent) != null

        //if this event was intercepted by overlaid UI elements
        if (!isUIEventHandled) {
            //Convert the mouse coordinates from screen to world coordinates
            transformMouseEvent(paramMouseEvent, worldTransform)

            val releasedLocation = Game.getWorld(currentWorldIndex).mouseReleased(paramMouseEvent)

            //if left click
            if (paramMouseEvent.button == MouseEvent.BUTTON1) {
                selectedLocation = releasedLocation
            } else if (paramMouseEvent.button == MouseEvent.BUTTON3) {
                //TODO: refactor? to allow drag for target orientation
                val selectedEntity = Game.getWorld(currentWorldIndex).world.getEntityAtIndex(selectedLocation!!.x, selectedLocation!!.y)
                val action = ActionMoveTestImpl(selectedEntity, user!!, releasedLocation!!)
                if (action.isValid) {
                    user!!.queueAction(action)
                    try {
                        Game.clientUtil.sendToServer(action.toJSON())
                    } catch (e: IOException) {
                        e.printStackTrace()
                    }

                } else {
                    Logger.log(Logger.LogLevel.debug, arrayOf("Invalid action"))
                }
            }//else if right click
        }
    }

    override //Not relevant with a custom UI stack
    fun mouseEntered(paramMouseEvent: MouseEvent) {
    }

    override //Not relevant with a custom UI stack
    fun mouseExited(paramMouseEvent: MouseEvent) {
    }

    companion object {
        private var selectedLocation: Point? = null
        private var user: Player? = null
        private val mapImages = arrayOf(Globals.Identifiers.background1A, Globals.Identifiers.background1B, Globals.Identifiers.background1C, Globals.Identifiers.background1D, Globals.Identifiers.background2A, Globals.Identifiers.background2B, Globals.Identifiers.background2C, Globals.Identifiers.background2D, Globals.Identifiers.background3A, Globals.Identifiers.background3B, Globals.Identifiers.background3C, Globals.Identifiers.background3D, Globals.Identifiers.background4A, Globals.Identifiers.background4B, Globals.Identifiers.background4C, Globals.Identifiers.background4D)
        private val edgeScrollArea = 50
        var currentWorldIndex: Int

        //variables related to edge scrolling
        val worldTransform = AffineTransform()
        //initial value is mostly irrelevant, and will be set properly the instant the mouse is moved
        var mouseLocation = Point(200, 200)
            private set

        var uiStack = UIStack()
        private var isScrollEnabled = false

        //Used in paintBackground(), avoids recreating an instance ~100 times a frame
        private val rand = Random()

        val screenLocationX: Int
            get() = worldTransform.translateX.toInt()

        val screenLocationY: Int
            get() = worldTransform.translateY.toInt()

        /**
         * Paint anything that moves relative to objects within the game world (ships, planets, etc)
         * @param renderUtil
         */
        fun paintWorld(renderUtil: RenderUtil) {
            val viewArea = doubleArrayOf(0.0, 0.0, Proto.screenWidth.toDouble(), 0.0, Proto.screenWidth.toDouble(), Proto.screenHeight.toDouble(), 0.0, Proto.screenHeight.toDouble())

            try {
                worldTransform.inverseTransform(viewArea, 0, viewArea, 0, 4)
                val indexes = arrayOf(World.convertAxialToOffset(WorldUIWrapper.getGridTileIndexFromPixelLocation(viewArea[0].toInt(), viewArea[1].toInt(), Game.getWorld(currentWorldIndex))), World.convertAxialToOffset(WorldUIWrapper.getGridTileIndexFromPixelLocation(viewArea[2].toInt(), viewArea[3].toInt(), Game.getWorld(currentWorldIndex))), World.convertAxialToOffset(WorldUIWrapper.getGridTileIndexFromPixelLocation(viewArea[4].toInt(), viewArea[5].toInt(), Game.getWorld(currentWorldIndex))), World.convertAxialToOffset(WorldUIWrapper.getGridTileIndexFromPixelLocation(viewArea[6].toInt(), viewArea[7].toInt(), Game.getWorld(currentWorldIndex))))
                val xMin = Math.min(indexes[0].x, Math.min(indexes[1].x, Math.min(indexes[2].x, indexes[3].x)))
                val xMax = Math.max(indexes[0].x, Math.max(indexes[1].x, Math.max(indexes[2].x, indexes[3].x)))

                val yMin = Math.min(indexes[0].y, Math.min(indexes[1].y, Math.min(indexes[2].y, indexes[3].y)))
                val yMax = Math.max(indexes[0].y, Math.max(indexes[1].y, Math.max(indexes[2].y, indexes[3].y)))

                for (x in Math.max(0, xMin)..Math.min(Game.getWorld(currentWorldIndex).world.sizeX, xMax)) {
                    for (y in Math.max(0, yMin)..Math.min(Game.getWorld(currentWorldIndex).world.sizeX, yMax)) {
                        Game.getWorld(currentWorldIndex).paintTile(renderUtil, World.convertOffsetToAxial(Point(x, y)))
                    }
                }
                Game.getWorld(currentWorldIndex).paint(renderUtil,
                        Point(Math.max(0, xMin), Math.min(Game.getWorld(currentWorldIndex).world.sizeX, xMax)),
                        Point(Math.max(0, yMin), Math.min(Game.getWorld(currentWorldIndex).world.sizeX, yMax)))
            } catch (e: NoninvertibleTransformException) {
                Logger.log(Logger.LogLevel.error, arrayOf("Camera has entered an invalid state, resetting."))
                e.printStackTrace()
                //TODO: save current worldTransform for error reporting
                worldTransform.setToIdentity()
            }

        }

        /**
         * Paint anything that is positioned relative to the screen (probably exclusively UI buttons and menus)
         * @param render
         * *
         * @param hasTurnEnded
         */
        fun paintScreen(render: RenderUtil, hasTurnEnded: Boolean) {
            uiStack.paint(render, hasTurnEnded)
            if (Globals.debug!!.detailedInfoLevel > 2) {
                render.drawString("mouseX:" + mouseLocation.x, 50, 50, Rectangle(Proto.screenWidth, Proto.screenHeight))
                render.drawString("mouseY:" + mouseLocation.y, 50, 60, Rectangle(Proto.screenWidth, Proto.screenHeight))
            }
        }

        fun doWork(timeAdjustment: BigDecimal, paused: Boolean) {
            //TODO: this doesn't feel quite right, experiment with different math. maybe 2 stages of constant speed?
            if (isScrollEnabled && !paused) {
                var xTranslation = 0
                var yTranslation = 0
                if (mouseLocation.x < edgeScrollArea && screenLocationX / worldTransform.scaleX < Game.getWorld(currentWorldIndex).tileWidth / 2) {
                    //Logger.log(LogLevel.info, new String[] {"moving left"});
                    xTranslation = timeAdjustment.multiply(BigDecimal(10)).toInt()
                } else if (mouseLocation.x >= Proto.screenWidth - edgeScrollArea && screenLocationX / worldTransform.scaleX > Proto.screenWidth - Game.getWorld(currentWorldIndex).tileWidth * (Game.getWorld(currentWorldIndex).world.sizeX + 1)) {
                    //Logger.log(LogLevel.info, new String[] {"moving right"});
                    xTranslation = -timeAdjustment.multiply(BigDecimal(10)).toInt()
                }
                if (mouseLocation.y < edgeScrollArea && screenLocationY / worldTransform.scaleY < Game.getWorld(currentWorldIndex).tileHeight / 2) {
                    //Logger.log(LogLevel.info, new String[] {"moving up"});
                    yTranslation = timeAdjustment.multiply(BigDecimal(10)).toInt()
                } else if (mouseLocation.y >= Proto.screenHeight - edgeScrollArea && screenLocationY / worldTransform.scaleY > Proto.screenHeight - 3 * Game.getWorld(currentWorldIndex).tileHeight / 4 * (Game.getWorld(currentWorldIndex).world.sizeY + 1)) {
                    //Logger.log(LogLevel.info, new String[] {"moving down"});
                    yTranslation = -timeAdjustment.multiply(BigDecimal(10)).toInt()
                }
                //TODO: maybe broken, debug this
                val rotation = Math.atan2(worldTransform.shearY, worldTransform.scaleY)
                val cos = Math.cos(rotation)
                val sin = Math.sin(rotation)
                worldTransform.translate(xTranslation * cos - yTranslation * sin,
                        xTranslation * sin + yTranslation * cos)
            }
        }

        private fun transformMouseEvent(event: MouseEvent, transform: AffineTransform) {
            val point = Point(event.x, event.y)
            try {
                transform.createInverse().transform(point, point)
            } catch (e: NoninvertibleTransformException) {
                e.printStackTrace()
            }

            event.translatePoint((point.getX() - event.x).toInt(), point.getY().toInt() - event.y)
        }
    }
}
