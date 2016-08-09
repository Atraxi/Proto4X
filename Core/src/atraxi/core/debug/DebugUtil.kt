package atraxi.core.debug

import java.lang.reflect.Field
import java.lang.reflect.Method

object DebugUtil {
    /**

     * @param o The object to all list fields and methods of
     * *
     * @param full Whether to include a list of constructors and methods within the object
     * *
     * @param setAccessible Set accessibility for reflecting into this object, if true this is passed to Field.setAccessible(). If set this will be reversed before returning
     * *
     * @return A complete breakdown of all fields and (optionally)methods
     */
    fun fullObjectStateDump(o: Any, full: Boolean, setAccessible: Boolean): String {

        var result = ""
        result += o.javaClass.getSimpleName() + ":\n{"
        try {
            val fields = o.javaClass.getDeclaredFields()
            for (f in fields) {
                result += "\n\t" + f.toString() + ":"
                try {
                    val wasAccessible = f.isAccessible()
                    if (setAccessible) {
                        f.setAccessible(true)
                    }
                    result += f.get(f).toString()
                    if (setAccessible && !wasAccessible) {
                        f.setAccessible(false)
                    }
                } catch (e: IllegalAccessException) {
                    result += "Not permited access to this variable"
                } catch (e: NullPointerException) {
                    result += "null"
                } catch (e: ExceptionInInitializerError) {
                    result += "Unable to initialize instance for static field"
                } catch (e: IllegalArgumentException) {
                    e.printStackTrace()
                }

            }
            if (full) {
                result += "\n"


                val methods = o.javaClass.getMethods()
                for (m in methods) {
                    result += "\n\t" + m.toString()
                }
            }
        } catch (e: SecurityException) {
            result += "\nNot permitted to reflect into objects in this environment."
        }

        return result + "\n}"
    }

    fun accessSuperclassBoolean(o: Any, fieldName: String): Boolean {
        try {
            val fields = o.javaClass.getSuperclass().getDeclaredFields()
            for (f in fields) {
                if (f.getName() == fieldName) {
                    f.setAccessible(true)
                    return f.getBoolean(o)
                }
            }
        } catch (e: IllegalArgumentException) {
            // TODO Auto-generated catch block
            e.printStackTrace()
        } catch (e: IllegalAccessException) {
            // TODO Auto-generated catch block
            e.printStackTrace()
        } catch (e: SecurityException) {
            // TODO Auto-generated catch block
            e.printStackTrace()
        }

        return false
    }

    fun accessSuperclassInt(o: Any, fieldName: String): Double {
        try {
            val fields = o.javaClass.getSuperclass().getDeclaredFields()
            for (f in fields) {
                if (f.getName() == fieldName) {
                    f.setAccessible(true)
                    return f.getInt(o).toDouble()
                }
            }
        } catch (e: IllegalArgumentException) {
            // TODO Auto-generated catch block
            e.printStackTrace()
        } catch (e: IllegalAccessException) {
            // TODO Auto-generated catch block
            e.printStackTrace()
        } catch (e: SecurityException) {
            // TODO Auto-generated catch block
            e.printStackTrace()
        }

        return 0.0
    }
}
