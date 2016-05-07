package atraxi.networking;

import atraxi.entities.actionQueue.Actions.ActionMove;
import atraxi.game.Game;
import atraxi.game.Player;
import atraxi.util.Logger;
import atraxi.util.RuntimeConstants;
import org.json.JSONStringer;

import java.io.BufferedReader;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.Socket;
import java.net.UnknownHostException;

/**
 * Created by Atraxi on 1/05/2016.
 */
public class ClientUtil implements Runnable
{
    private Socket socket;

    public ClientUtil(Player player) throws IOException, UnknownHostException
    {
        Socket clientSocket = new Socket("localhost", 6789);
        socket = clientSocket;

        DataOutputStream outToServer = new DataOutputStream(clientSocket.getOutputStream());

        JSONStringer jsonStringer = new JSONStringer();
        jsonStringer.object();
        jsonStringer.key(RuntimeConstants.JSONPlayerName).value("testPlayer");
        jsonStringer.key(RuntimeConstants.JSONPlayerIndex).value(Game.getPlayerList().indexOf(player));
        jsonStringer.key(RuntimeConstants.JSONActionClassName).value(ActionMove.class.getName());
        jsonStringer.endObject();

        outToServer.writeBytes(jsonStringer.toString() + "\n");
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

    public void sendToServer(String message) throws IOException
    {
        DataOutputStream outToServer = new DataOutputStream(socket.getOutputStream());
        outToServer.writeBytes(message + '\n');
    }
}
