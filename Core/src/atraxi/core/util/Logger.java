package atraxi.core.util;

/**
 * Created by Atraxi on 10/10/2015.
 */
public class Logger
{
    //TODO: write to file, print to screen etc

    /**
     *
     * @param logLevel
     * @param messages each line of output, system specific newline added between each String arg
     */
    public static void log (LogLevel logLevel, String[] messages)
    {
        switch (logLevel)
        {
            case debug:
                if(Globals.debug.getDetailedInfoLevel() > 0)
                {
                    for(String message : messages)
                    {
                        System.out.println("[Debug]" + message);
                    }
                }
                break;
            case info:
                for(String message : messages)
                {
                    System.out.println("[Info]" + message);
                }
                break;
            case warning:
                for(String message : messages)
                {
                    System.err.println("[Warning]" + message);
                }
                break;
            case error:
                for(String message : messages)
                {
                    System.err.println("[ERROR]" + message);
                }
                break;
        }
    }

    public enum LogLevel
    {
        debug,info,warning,error
    }
}
