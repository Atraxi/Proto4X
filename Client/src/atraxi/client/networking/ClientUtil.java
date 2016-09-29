package atraxi.client.networking;

import atraxi.client.Proto;
import atraxi.core.Player;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;
import atraxi.core.world.World;
import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.ConnectException;
import java.net.Socket;
import java.net.UnknownHostException;
import java.util.ArrayList;

/**
 * Created by Atraxi on 1/05/2016.
 */
public class ClientUtil implements Runnable
{//TODO: attempt auto reconnect if connection fails unexpectedly
    private Socket socket;

    public ClientUtil(Player player) throws IOException, UnknownHostException, ConnectException
    {
        socket = new Socket("localhost", 6789);

        JSONObject object = new JSONObject();
        object.put(Globals.JSON_KEY_INITIALIZATION_PlayerName, player.getName())
              .put(Globals.JSON_KEY_INITIALIZATION_PlayerIndex, 0);//TODO: request all available player slots, user picks one
              //.put(Globals.JSON_KEY_MessagePayload_Action_ClassName, ActionMove.class.getName());

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
                String receivedMessage = inFromServer.readLine();
                if(receivedMessage != null)
                {
                    Logger.log(Logger.LogLevel.debug, new String[]{"Message received from server", receivedMessage});

                    JSONObject receivedObject = new JSONObject(receivedMessage);
                    //What type of message did we just receive?
                    switch(receivedObject.getString(Globals.JSON_KEY_MessageType))
                    {
                        case Globals.JSON_VALUE_MessageType_GameData:
                            JSONArray worldsArrayJSON = receivedObject.getJSONArray(Globals.JSON_KEY_MessagePayload_WorldData);
                            ArrayList<World> worlds = new ArrayList<World>(worldsArrayJSON.length());
                            for(int i = 0; i < worldsArrayJSON.length(); i++)
                            {
                                try
                                {
                                    worlds.add(World.deserialize(worldsArrayJSON.getJSONObject(i)));
                                }
                                catch(ClassNotFoundException e)
                                {
                                    JSONObject errorMessage = new JSONObject();
                                    errorMessage.put(Globals.JSON_KEY_MessageType, Globals.JSON_VALUE_MessageType_Error)
                                                .put(Globals.JSON_KEY_MessagePayload_Error, Globals.JSON_VALUE_Error_UnknownType);
                                    sendToServer(errorMessage);
                                    e.printStackTrace();
                                    System.exit(0);
                                }
                                catch(InstantiationException e)
                                {
                                    JSONObject errorMessage = new JSONObject();
                                    errorMessage.put(Globals.JSON_KEY_MessageType, Globals.JSON_VALUE_MessageType_Error)
                                                .put(Globals.JSON_KEY_MessagePayload_Error, Globals.JSON_VALUE_Error_NonInstantiatableType);
                                    sendToServer(errorMessage);
                                    e.printStackTrace();
                                    System.exit(0);
                                }
                            }

                            JSONArray playersArrayJSON = receivedObject.getJSONArray(Globals.JSON_KEY_MessagePayload_PlayerData);
                            ArrayList<Player> players = new ArrayList<>(playersArrayJSON.length());
                            //TODO: deserialize players

                            Proto.getPROTO().initGame(worlds, players);
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

    public void sendToServer(JSONObject message) throws IOException
    {//TODO: figure out why the IOException can be thrown, try handle it, only throw if unrecoverable.
        DataOutputStream outToServer = new DataOutputStream(socket.getOutputStream());
        outToServer.writeBytes(message + "\n");
    }
}
