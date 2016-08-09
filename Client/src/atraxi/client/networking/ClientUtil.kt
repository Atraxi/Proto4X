package atraxi.client.networking

import atraxi.client.Proto
import atraxi.core.Player
import atraxi.core.util.Globals
import atraxi.core.util.Logger
import atraxi.core.world.World
import org.json.JSONArray
import org.json.JSONObject

import java.io.BufferedReader
import java.io.DataOutputStream
import java.io.IOException
import java.io.InputStreamReader
import java.net.ConnectException
import java.net.Socket
import java.net.UnknownHostException
import java.util.ArrayList

/**
 * Created by Atraxi on 1/05/2016.
 */
class ClientUtil @Throws(IOException::class, UnknownHostException::class, ConnectException::class)
constructor(player: Player) : Runnable {
    //TODO: attempt auto reconnect if connection fails unexpectedly
    private val socket: Socket

    init {
        val clientSocket = Socket("localhost", 6789)
        socket = clientSocket

        val `object` = JSONObject()
        `object`.put(Globals.JSON_KEY_INITIALIZATION_PlayerName, player.name).put(Globals.JSON_KEY_INITIALIZATION_PlayerIndex, 0)//TODO: request all available player slots, user picks one
        //.put(Globals.JSON_KEY_MessagePayload_Action_ClassName, ActionMove.class.getName());

        sendToServer(`object`)
    }

    override fun run() {
        try {
            val inFromServer = BufferedReader(InputStreamReader(socket.inputStream))
            Logger.log(Logger.LogLevel.debug, arrayOf("Connected to server. Listening for data."))
            while (!socket.isClosed) {
                val receivedMessage = inFromServer.readLine()
                if (receivedMessage != null) {
                    Logger.log(Logger.LogLevel.debug, arrayOf("Message recieved from server\n" + receivedMessage))

                    val receivedObject = JSONObject(receivedMessage)
                    //What type of message did we just receive?
                    when (receivedObject.getString(Globals.JSON_KEY_MessageType)) {
                        Globals.JSON_VALUE_MessageType_GameData -> {
                            val worldsArrayJSON = receivedObject.getJSONArray(Globals.JSON_KEY_MessagePayload_WorldData)
                            val worlds = ArrayList<World>(worldsArrayJSON.length())
                            for (i in 0..worldsArrayJSON.length() - 1) {
                                try {
                                    worlds.add(World.deserialize(worldsArrayJSON.getJSONObject(i)))
                                } catch (e: ClassNotFoundException) {
                                    val errorMessage = JSONObject()
                                    errorMessage.put(Globals.JSON_KEY_MessageType, Globals.JSON_VALUE_MessageType_Error).put(Globals.JSON_KEY_MessagePayload_ErrorMessage, Globals.JSON_VALUE_Error_UnknownType)
                                    sendToServer(errorMessage)
                                } catch (e: InstantiationException) {
                                    val errorMessage = JSONObject()
                                    errorMessage.put(Globals.JSON_KEY_MessageType, Globals.JSON_VALUE_MessageType_Error).put(Globals.JSON_KEY_MessagePayload_ErrorMessage, Globals.JSON_VALUE_Error_NonInstantiatableType)
                                    sendToServer(errorMessage)
                                }

                            }

                            val playersArrayJSON = receivedObject.getJSONArray(Globals.JSON_KEY_MessagePayload_PlayerData)
                            val players = ArrayList<Player>(playersArrayJSON.length())
                            //TODO: deserialize players

                            Proto.proto.initGame(worlds, players)
                        }
                    }
                }
            }
        } catch (e: IOException) {
            e.printStackTrace()
        }

    }

    @Throws(IOException::class)
    fun sendToServer(message: JSONObject) {//TODO: figure out why the IOException can be thrown, try handle it, only throw if unrecoverable.
        val outToServer = DataOutputStream(socket.outputStream)
        outToServer.writeBytes(message + "\n")
    }
}
