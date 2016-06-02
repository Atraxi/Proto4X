package atraxi.client.networking;

import atraxi.client.Game;
import atraxi.core.Player;
import atraxi.core.entities.action.definitions.ActionMove;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.ConnectException;
import java.net.Socket;
import java.net.UnknownHostException;

/**
 * Created by Atraxi on 1/05/2016.
 */
public class ClientUtil implements Runnable
{//TODO: attempt auto reconnect if connection fails unexpectedly
    private Socket socket;

    public ClientUtil(Player player) throws IOException, UnknownHostException, ConnectException
    {
        Socket clientSocket = new Socket("localhost", 6789);
        socket = clientSocket;

        JSONObject object = new JSONObject();
        object.put(Globals.JSON_KEY_PlayerName, player.getName())
              .put(Globals.JSON_KEY_PlayerIndex, Game.getPlayerList().indexOf(player))
              .put(Globals.JSON_KEY_ActionClassName, ActionMove.class.getName());

        sendToServer(object);
    }

    @Override
    public void run()
    {
        try
        {
            BufferedReader inFromServer = new BufferedReader(new InputStreamReader(socket.getInputStream()));
            Logger.log(Logger.LogLevel.debug, new String[]{"Connected to server. Listening for data."});
            while(!socket.isClosed())
            {
                String messageReceived = inFromServer.readLine();
                if(messageReceived != null)
                {
                    Logger.log(Logger.LogLevel.debug, new String[]{"Message recieved from server\n" + messageReceived});
                }
            }
        }
        catch(IOException e)
        {
            e.printStackTrace();
        }
    }

    public void sendToServer(JSONObject message) throws IOException
    {//TODO: figure out why the IOException was thrown, try handle it, only throw if unrecoverable.
        DataOutputStream outToServer = new DataOutputStream(socket.getOutputStream());
        outToServer.writeBytes(message + "\n");
    }
}
