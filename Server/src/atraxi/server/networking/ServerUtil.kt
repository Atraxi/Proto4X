package atraxi.server.networking

import atraxi.core.Player
import atraxi.core.entities.action.definitions.Action
import atraxi.core.util.Globals
import atraxi.core.util.Logger
import atraxi.server.Game
import org.json.JSONArray
import org.json.JSONObject

import java.io.BufferedReader
import java.io.DataOutputStream
import java.io.IOException
import java.io.InputStreamReader
import java.lang.reflect.InvocationTargetException
import java.net.ServerSocket
import java.net.Socket
import java.net.SocketException
import java.util.ArrayList
import java.util.HashMap
import java.util.Timer
import java.util.TimerTask

/**
 * Created by Atraxi on 1/05/2016.
 */
class ServerUtil(private val playerCount: Int) : Runnable {
    private val clientConnections: MutableMap<Player, ClientConnection>
    private var endTurnCount: Int = 0

    init {
        clientConnections = HashMap<Player, ClientConnection>()
        endTurnCount = 0
    }

    override fun run() {
        val players = Game.playerList
        try {
            ServerSocket(6789).use { welcomeSocket ->

                while (true) {
                    val connectionSocket = welcomeSocket.accept()
                    val inFromClient = BufferedReader(InputStreamReader(connectionSocket.inputStream))
                    try {
                        val playerDataRaw = inFromClient.readLine()
                        val playerData = JSONObject(playerDataRaw)
                        val player = players.get(playerData.getInt(Globals.INSTANCE.getJSON_KEY_INITIALIZATION_PlayerIndex()))
                        if (!clientConnections.containsKey(player)) {
                            player.name = playerData.getString(Globals.INSTANCE.getJSON_KEY_INITIALIZATION_PlayerName())
                            val clientConnection = ClientConnection(connectionSocket, player)
                            clientConnections.put(player, clientConnection)
                            Thread(clientConnection, "Client " + player.name!!).start()

                            Timer().schedule(
                                    object : TimerTask() {
                                        override fun run() {
                                            val jsonObject = JSONObject()
                                            jsonObject.put(Globals.INSTANCE.getJSON_KEY_MessageType(), Globals.INSTANCE.getJSON_VALUE_MessageType_GameData())

                                            val worldsJSON = JSONArray()
                                            for (i in 0..Game.worldCount - 1) {
                                                worldsJSON.put(Game.getWorld(i).serializeForPlayer(player))
                                            }
                                            jsonObject.put(Globals.INSTANCE.getJSON_KEY_MessagePayload_WorldData(), worldsJSON)

                                            //TODO: serialize players
                                            jsonObject.put(Globals.INSTANCE.getJSON_KEY_MessagePayload_PlayerData(), JSONArray())
                                            sendToPlayer(player, jsonObject)
                                        }
                                    },
                                    500)

                        } else {
                            Logger.log(Logger.LogLevel.warning, arrayOf("A connection to player in slot " + playerData.getInt(
                                    Globals.INSTANCE.getJSON_KEY_INITIALIZATION_PlayerIndex()) + " already exists, " +
                                    "new connection refused"))
                        }
                    } catch (e: IOException) {
                        e.printStackTrace()
                    }

                }
            }
        } catch (e: IOException) {
            e.printStackTrace()
        }

    }

    fun sendToPlayer(player: Player, message: JSONObject) {
        clientConnections[player].sendMessage(message)
    }

    /**
     * A connection to a single client, from the perspective of the server
     */
    private inner class ClientConnection internal constructor(private val socket: Socket, private val player: Player) : Runnable {

        override fun run() {
            try {
                val inFromClient = BufferedReader(InputStreamReader(socket.inputStream))
                Logger.log(Logger.LogLevel.info, arrayOf("Server connected to " + player.name + ". Listening for data."))
                while (!socket.isClosed) {
                    val playerMessage: String?
                    try {
                        playerMessage = inFromClient.readLine()
                    } catch (e: SocketException) {
                        Logger.log(Logger.LogLevel.warning, arrayOf("Connection to player " + player.name + " has dropped"))
                        clientConnections.remove(player)
                        return
                    }

                    if (playerMessage != null) {
                        Logger.log(Logger.LogLevel.debug, arrayOf("Message recieved from " + player.name + "\n" + playerMessage))

                        val receivedObject = JSONObject(playerMessage)
                        //What type of message did we just receive?
                        when (receivedObject.getString(Globals.INSTANCE.getJSON_KEY_MessageType())) {
                        //The player performed some type of Action
                            Globals.INSTANCE.getJSON_VALUE_MessageType_Action() -> if (!player.isTurnFinished) {
                                try {
                                    //Deserialize the action, must be done via reflection therefore
                                    val actionClass = Class.forName(receivedObject.getString(Globals.INSTANCE.getJSON_KEY_MessagePayload_Action_ClassName()))
                                    val action = actionClass.getConstructor().newInstance() as Action
                                    action.fromJSON(receivedObject.getJSONObject(Globals.INSTANCE.getJSON_KEY_MessagePayload_ActionData()))
                                    player.queueAction(action)
                                } catch (e: ClassNotFoundException) {
                                    Logger.log(Logger.LogLevel.debug,
                                            arrayOf("Action of type " + receivedObject.getString(
                                                    Globals.INSTANCE.getJSON_KEY_MessagePayload_Action_ClassName()) + " could not be " +
                                                    "found"))
                                    e.printStackTrace()
                                } catch (e: NoSuchMethodException) {
                                    Logger.log(Logger.LogLevel.debug, arrayOf("No default constructor accessible for action of type " + receivedObject.getString(
                                            Globals.INSTANCE.getJSON_KEY_MessagePayload_Action_ClassName())))
                                    e.printStackTrace()
                                } catch (e: IllegalAccessException) {
                                    e.printStackTrace()
                                } catch (e: InstantiationException) {
                                    e.printStackTrace()
                                } catch (e: InvocationTargetException) {
                                    e.printStackTrace()
                                }

                            } else {
                                Logger.log(Logger.LogLevel.warning, arrayOf("Received action from player " + player.name + " after they have ended their turn"))
                                sendMessage(JSONObject())//TODO error message, turn already finished
                            }
                            Globals.INSTANCE.getJSON_VALUE_MessageType_EndTurn() -> {
                                player.endTurn()
                                synchronized(clientConnections) {
                                    endTurnCount++
                                    if (endTurnCount >= playerCount) {
                                        Game.processTurn()
                                        endTurnCount = 0
                                    }
                                }
                            }
                            Globals.INSTANCE.getJSON_VALUE_MessageType_Error() ->
                                //TODO: react to errors reported by the client
                                Logger.log(Logger.LogLevel.debug, arrayOf(receivedObject.toString(1)))
                        }
                    }
                }
            } catch (e: IOException) {
                e.printStackTrace()
            }

        }

        fun sendMessage(message: JSONObject) {
            try {
                val outToClient = DataOutputStream(socket.outputStream)
                outToClient.writeBytes(message + "\n")
            } catch (e: IOException) {
                e.printStackTrace()
            }

        }
    }
}