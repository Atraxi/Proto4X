package atraxi.server.networking;

import atraxi.core.Player;
import atraxi.core.entities.action.definitions.Action;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;
import atraxi.server.Game;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.lang.reflect.InvocationTargetException;
import java.net.ServerSocket;
import java.net.Socket;
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
                    Player player = players.get(playerData.getInt(Globals.JSONPlayerIndex));
                    player.setName(playerData.getString(Globals.JSONPlayerName));
                    ClientConnection clientConnection = new ClientConnection(connectionSocket, player);
                    clientConnections.put(player, clientConnection);
                    new Thread(clientConnection).start();
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
     * A connection to a single atraxi.client, from the perspective of the atraxi.server
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
            try
            {
                BufferedReader inFromClient = new BufferedReader(new InputStreamReader(socket.getInputStream()));
                Logger.log(Logger.LogLevel.debug, new String[]{"Server connected to " + player.getName() + ". Listening for data."});
                while(!socket.isClosed())
                {
                    String playerMessage = inFromClient.readLine();
                    if(playerMessage != null)
                    {
                        Logger.log(Logger.LogLevel.debug, new String[]{"Message recieved from " + player.getName() + "\n" + playerMessage});

                        JSONObject receivedObject = new JSONObject(playerMessage);
                        //What type of message did we just receive?
                        switch(receivedObject.getString(Globals.JSONMessageTypeKey))
                        {
                            //The player performed some type of Action
                            case Globals.JSONMessageTypeValue_Action:
                                if(!player.isTurnFinished())
                                {
                                    try
                                    {
                                        //Deserialize the action, must be done via reflection therefore
                                        Class<?> actionClass = Class.forName(receivedObject.getString(Globals.JSONActionClassName));
                                        Action action = (Action) actionClass.getConstructor().newInstance();
                                        action.fromJSON(receivedObject);
                                        player.queueAction(action);
                                    }
                                    catch(ClassNotFoundException e)
                                    {
                                        Logger.log(Logger.LogLevel.debug,
                                                   new String[]{"Action of type " + receivedObject.getString(Globals.JSONActionClassName) + " could not be " +
                                                                "found"});
                                        e.printStackTrace();
                                    }
                                    catch(NoSuchMethodException e)
                                    {
                                        Logger.log(Logger.LogLevel.debug, new String[]{"No default construction accessible for action of type " +
                                                                                       receivedObject.getString(Globals.JSONActionClassName)});
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
                                    sendMessage(new JSONObject());//TODO error message, turn already finished
                                }
                                break;
                            case Globals.JSONMessageTypeValue_EndTurn:
                                player.endTurn();
                                synchronized(clientConnections)
                                {
                                    endTurnCount++;
                                    if(endTurnCount >= playerCount)
                                    {
                                        Game.processTurn();
                                    }
                                }
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