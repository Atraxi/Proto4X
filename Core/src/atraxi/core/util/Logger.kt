package atraxi.core.util

/**
 * Created by Atraxi on 10/10/2015.
 */
object Logger {
    //TODO: write to file, print to screen etc

    /**

     * @param logLevel
     * *
     * @param messages each line of output, system specific newline added between each String arg
     */
    fun log(logLevel: LogLevel, messages: Array<String>) {
        when (logLevel) {
            Logger.LogLevel.debug -> if (Globals.debug!!.detailedInfoLevel > 0) {
                for (message in messages) {
                    println("[Debug]" + message)
                }
            }
            Logger.LogLevel.info -> for (message in messages) {
                println("[Info]" + message)
            }
            Logger.LogLevel.warning -> for (message in messages) {
                System.err.println("[Warning]" + message)
            }
            Logger.LogLevel.error -> for (message in messages) {
                System.err.println("[ERROR]" + message)
            }
        }
    }

    enum class LogLevel {
        debug, info, warning, error
    }
}
