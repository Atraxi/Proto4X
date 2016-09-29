package atraxi.server.networking;

import atraxi.core.Player;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;
import atraxi.server.Game;
import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.lang.reflect.InvocationTargetException;
import java.net.ServerSocket;
import java.net.Socket;
import java.net.SocketException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;

/**
 * Created by Atraxi on 1/05/2016.
 */
public class ServerUtil implements Runnable
{
    private final Map<Player, ClientConnection> clientConnections;
    private int endTurnCount, playerCount;

    public ServerUtil(int playerCount)
    {
        clientConnections = new HashMap<>();
        endTurnCount = 0;
        this.playerCount = playerCount;
    }

    @Override
    public void run()
    {
        ArrayList<Player> players = Game.getPlayerList();
        try(ServerSocket welcomeSocket = new ServerSocket(6789))
        {

            while(true)
            {
                Socket connectionSocket = welcomeSocket.accept();
                BufferedReader inFromClient = new BufferedReader(new InputStreamReader(connectionSocket.getInputStream()));
                try
                {
                    String playerDataRaw = inFromClient.readLine();
                    JSONObject playerData = new JSONObject(playerDataRaw);
                    final Player player = players.get(playerData.getInt(Globals.JSON_KEY_INITIALIZATION_PlayerIndex));
                    if(!clientConnections.containsKey(player))
                    {
                        player.setName(playerData.getString(Globals.JSON_KEY_INITIALIZATION_PlayerName));
                        ClientConnection clientConnection = new ClientConnection(connectionSocket, player);
                        clientConnections.put(player, clientConnection);
                        new Thread(clientConnection, "Client " + player.getName()).start();
                    }
                    else
                    {
                        Logger.log(Logger.LogLevel.warning, new String[]{"A connection to player in slot " + playerData.getInt(Globals.JSON_KEY_INITIALIZATION_PlayerIndex) + " already exists, " +
                                                                         "new connection refused"});
                    }
                }
                catch(IOException e)
                {
                    e.printStackTrace();
                }
            }
        }
        catch(IOException e)
        {
            e.printStackTrace();
        }
    }

    public void sendToPlayer(Player player, JSONObject message)
    {
        clientConnections.get(player).sendMessage(message);
    }

    /**
     * A connection to a single client, from the perspective of the server
     */
    private class ClientConnection implements Runnable
    {
        private final Player player;
        private final Socket socket;

        ClientConnection(Socket socket, Player player)
        {
            this.socket = socket;
            this.player = player;
        }

        @Override
        public void run()
        {
            JSONObject jsonObject = new JSONObject();
            jsonObject.put(Globals.JSON_KEY_MessageType, Globals.JSON_VALUE_MessageType_GameData);

            JSONArray worldsJSON = new JSONArray();
            for(int i = 0; i < Globals.getWorldCount(); i++)
            {
                worldsJSON.put(Globals.getWorld(i).serializeForPlayer(player));
            }
            jsonObject.put(Globals.JSON_KEY_MessagePayload_WorldData, worldsJSON);

            //TODO: serialize players
            jsonObject.put(Globals.JSON_KEY_MessagePayload_PlayerData, new JSONArray());
            sendToPlayer(player, jsonObject);

            try
            {
                BufferedReader inFromClient = new BufferedReader(new InputStreamReader(socket.getInputStream()));
                Logger.log(Logger.LogLevel.info, new String[]{"Server connected to " + player.getName() + ". Listening for data."});
                while(!socket.isClosed())
                {
                    String playerMessage;
                    try
                    {
                        playerMessage = inFromClient.readLine();
                    }
                    catch(SocketException e)
                    {
                        Logger.log(Logger.LogLevel.warning, new String[]{"Connection to player " + player.getName() + " has dropped"});
                        clientConnections.remove(player);
                        return;
                    }
                    if(playerMessage != null)
                    {
                        Logger.log(Logger.LogLevel.debug, new String[]{"Message recieved from " + player.getName() + "\n" + playerMessage});

                        JSONObject receivedObject = new JSONObject(playerMessage);
                        //What type of message did we just receive?
                        switch(receivedObject.getString(Globals.JSON_KEY_MessageType))
                        {
                            //The player performed some type of Action
                            case Globals.JSON_VALUE_MessageType_Action:
                                if(!player.isTurnFinished())
                                {
                                    try
                                    {
                                        //Deserialize the action, must be done via reflection therefore
                                        Class<?> actionClass = Class.forName(receivedObject.getString(Globals.JSON_KEY_MessagePayload_Action_ClassName));
                                        Action action = (Action) actionClass.getConstructor().newInstance();
                                        action.deserialize(receivedObject.getJSONObject(Globals.JSON_KEY_MessagePayload_ActionData));
                                        player.queueAction(action);
                                    }
                                    catch(ClassNotFoundException e)
                                    {
                                        Logger.log(Logger.LogLevel.debug,
                                                   new String[]{"Action of type " + receivedObject.getString(Globals.JSON_KEY_MessagePayload_Action_ClassName) + " could not be " +
                                                                "found"});
                                        e.printStackTrace();
                                    }
                                    catch(NoSuchMethodException e)
                                    {
                                        Logger.log(Logger.LogLevel.debug, new String[]{"No default constructor accessible for action of type " +
                                                                                       receivedObject.getString(Globals.JSON_KEY_MessagePayload_Action_ClassName)});
                                        e.printStackTrace();
                                    }
                                    catch(IllegalAccessException e)
                                    {
                                        e.printStackTrace();
                                    }
                                    catch(InstantiationException e)
                                    {
                                        e.printStackTrace();
                                    }
                                    catch(InvocationTargetException e)
                                    {
                                        e.printStackTrace();
                                    }
                                }
                                else
                                {
                                    Logger.log(Logger.LogLevel.warning, new String[]{"Received action from player " + player.getName() + " after they have ended their turn"});
                                    sendMessage(new JSONObject());//TODO error message, turn already finished
                                }
                                break;
                            case Globals.JSON_VALUE_MessageType_EndTurn:
                                player.endTurn();
                                synchronized(clientConnections)
                                {
                                    endTurnCount++;
                                    if(endTurnCount >= playerCount)
                                    {
                                        Game.processTurn();
                                        endTurnCount = 0;
                                    }
                                }
                                break;
                            case Globals.JSON_VALUE_MessageType_Error:
                                //TODO: react to errors reported by the client
                                Logger.log(Logger.LogLevel.debug, new String[]{receivedObject.toString(1)});
                                break;
                        }
                    }
                }
            }
            catch(IOException e)
            {
                e.printStackTrace();
            }
        }

        public void sendMessage(JSONObject message)
        {
            try
            {
                DataOutputStream outToClient = new DataOutputStream(socket.getOutputStream());
                outToClient.writeBytes(message + "\n");
            }
            catch(IOException e)
            {
                e.printStackTrace();
            }
        }
    }
}