package atraxi.networking;

import atraxi.game.Game;
import atraxi.game.Player;
import atraxi.util.Logger;
import atraxi.util.RuntimeConstants;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
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
    Map<Player, ClientConnection> clientConnections;

    public ServerUtil(int playerCount)
    {
        clientConnections = new HashMap<>();
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
                    Player player = players.get(playerData.getInt(RuntimeConstants.JSONPlayerIndex));
                    player.setName(playerData.getString(RuntimeConstants.JSONPlayerName));
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

    public void sendToPlayer(Player player, String message)
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
                        //player.queueAction(Action.fromJSON(playerMessage));
                    }
                }
            }
            catch(IOException e)
            {
                e.printStackTrace();
            }
        }

        public void sendMessage(String message)
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