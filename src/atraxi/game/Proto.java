package atraxi.game;

import java.awt.Dimension;
import java.awt.EventQueue;
import java.awt.Toolkit;
import java.util.ArrayList;

import javax.swing.JFrame;

import debug.TestSystems;
import factions.Human;
import factions.Player;

public class Proto extends JFrame
{
    private static final long serialVersionUID = 1L;
    public static int screen_Width;
    public static int screen_Height;
    private static boolean debug = false;

    public Proto(ArrayList<Player> players)
    {
        Dimension dim = Toolkit.getDefaultToolkit().getScreenSize();
        final int screen_Width = dim.width;
        final int screen_Height = dim.height;
        
        if(debug)
        {
            addKeyListener(new TestSystems(players).getDebugKeyIntercept());
        }
        else
        {
            addKeyListener((Human)players.get(0));
        }
        
        //add a JPanel to this JFrame
        add(new Game(players));
        
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
        //Future: compiler flags for debug build
        if(args.length>0 && args[0].equals("debug"))
        {
            System.out.println("Debug enabled");
            debug=true;
        }
        
        Human human = new Human();
        ArrayList<Player> players = new ArrayList<Player>();
        players.add(human);
        
        EventQueue.invokeLater(new Runnable()
        {
            @Override
            public void run()
            {
                Proto frame = new Proto(players);
                frame.setVisible(true);
            }
        });
    }
}
