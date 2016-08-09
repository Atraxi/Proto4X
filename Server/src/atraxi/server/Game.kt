package atraxi.server

import atraxi.core.Player
import atraxi.core.util.Logger
import atraxi.core.world.World
import atraxi.server.networking.ServerUtil

import java.math.BigDecimal
import java.util.ArrayList

class Game(players: ArrayList<Player>, worlds: ArrayList<World>) : Runnable {

    init {
        Game.playerList = players
        Game.worlds = worlds
        serverUtil = ServerUtil(players.size)
        Thread(serverUtil, "ServerSocket").start()
    }

    private fun gameLoop(timeAdjustment: BigDecimal) {
        //        for(World world : worlds)
        //        {
        //            for(Entity entity : world.getEntityList())
        //            {
        //                entity.doWork(timeAdjustment, paused);
        //            }
        //        }
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

                beforeTime = currentTime
            }
        }
    }

    companion object {
        /**
         * The desired time per frame (in nanoseconds) from which to adjust the speed of all events. 60fps or 1,000,000,000ns/60
         */
        val OPTIMALFRAMETIME: Long = 16666666
        private val MINIMUMFRAMETIME: Long = 8333333
        var playerList: ArrayList<Player>? = null
            private set
        private var worlds: ArrayList<World>? = null
        private var serverUtil: ServerUtil

        fun getWorld(index: Int): World {
            return worlds!![index]
        }

        val worldCount: Int
            get() = worlds!!.size

        fun processTurn() {
            Logger.log(Logger.LogLevel.debug, arrayOf("End turn received from all players, processing all actions."))
            playerList!!.parallelStream().forEach(Consumer<Player> { it.processSetStatesThisTurn() })
            playerList!!.parallelStream().forEach(Consumer<Player> { it.processBuildsThisTurn() })
            playerList!!.parallelStream().forEach(Consumer<Player> { it.processAttacksThisTurn() })
            playerList!!.parallelStream().forEach(Consumer<Player> { it.processMovesThisTurn() })
        }
    }
}