package atraxi.util;

import atraxi.game.Proto;

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
    public static void log (LogLevel logLevel, String... messages)
    {
        switch (logLevel)
        {
            case debug:
                if(Proto.debug)
                {
                    for(String message : messages)
                    {
                        System.out.println("[debug]" + message);
                    }
                }
                break;
            case info:
                for(String message : messages)
                {
                    System.out.println("[info]" + message);
                }
                break;
            case warning:
                for(String message : messages)
                {
                    System.err.println("[warning]" + message);
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