package atraxi.game;

import java.awt.Dimension;
import java.awt.EventQueue;
import java.awt.Toolkit;
import java.util.ArrayList;

import javax.swing.JFrame;

import debug.DebugKeyIntercept;
import factions.Player;

public class Proto extends JFrame
{
    private static final long serialVersionUID = 1L;
    public static int screen_Width;
    public static int screen_Height;
    private static boolean debug;

    public Proto(String[] args)
    {
        //Future: compiler flags for debug build
        if(args.length>0 && args[0].equals("-debug"))
        {
            System.out.println("Debug enabled");
            debug = true;
        }
        else
        {
            debug = false;
        }
        
        Dimension dim = Toolkit.getDefaultToolkit().getScreenSize();
        screen_Width = dim.width;
        screen_Height = dim.height;
        
        Player user = new Player();
        ArrayList<Player> players = new ArrayList<Player>();
        players.add(user);
        UserInterfaceHandler ui;
        if(debug)
        {
            ui = new DebugKeyIntercept(user);
        }
        else
        {
            ui = new UserInterfaceHandler(user);
        }
        
        //add a JPanel to this JFrame
        add(new Game(players, ui));
        
        setFocusable(true);
        //TODO:toggle windowed mode, add resizing
        //setUndecorated(true);
        setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        setSize(screen_Width, screen_Height);
        setLocationRelativeTo(null);
        setTitle("4X_proto");
        setResizable(false);
    }
    
    public static void main(String[] args)
    {
        EventQueue.invokeLater(new Runnable()
        {
            @Override
            public void run()
            {
                Proto frame = new Proto(args);
                frame.setVisible(true);
            }
        });
    }
}
