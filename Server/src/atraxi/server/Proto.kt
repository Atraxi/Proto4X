package atraxi.server

import atraxi.core.Player
import atraxi.core.util.DebugState
import atraxi.core.util.Globals
import atraxi.core.util.Logger
import atraxi.core.world.World

import java.util.ArrayList
import java.util.Random

object Proto {
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
                else -> Logger.log(Logger.LogLevel.debug, arrayOf("Unknown argument \"" + args[i] + "\""))
            }
            i++
        }

        Globals.random = Random(Globals.SEED)

        val user = Player("")
        val players = ArrayList<Player>()
        players.add(user)

        val worlds = ArrayList<World>()

        //Hexagonal - assumes regular hexagon with points at top/bottom with all points tightly bound to image dimensions, traverse points clockwise from top center
        worlds.add(World(Globals.random!!.nextInt().toLong(), 1000000, 1000000))

        val game = Game(players, worlds)
        Logger.log(Logger.LogLevel.debug, arrayOf("Server started"))
    }
}
