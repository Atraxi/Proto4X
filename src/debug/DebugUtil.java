package debug;

import java.lang.reflect.Field;
import java.lang.reflect.Method;

public class DebugUtil
{
    /**
     * 
     * @param o The object to all list fields and methods of
     * @param full Whether to include a list of constructors and methods within the object
     * @param setAccessible Set accessibility for reflecting into this object, if true this is passed to Field.setAccessible(). If set this will be reversed before returning
     * @return A complete breakdown of all fields and (optionally)methods
     */
    public static String fullObjectStateDump(Object o, boolean full, boolean setAccessible)
    {
        
        String result = "";
        result += o.getClass().getSimpleName() + ":\n{";
        try
        {
            Field[] fields = o.getClass().getDeclaredFields();
            for(Field f : fields)
            {
                result += "\n\t" + f.toString() + ":";
                try
                {
                    if(setAccessible)
                    {
                        f.setAccessible(true);
                    }
                    result += f.get(f).toString();
                    if(setAccessible)
                    {
                        f.setAccessible(false);
                    }
                }
                catch (IllegalAccessException e)
                {
                    result += "Not permited access to this variable";
                }
                catch(NullPointerException e)
                {
                    result += "null";
                }
                catch(ExceptionInInitializerError e)
                {
                    result += "Unable to initialize instance for static field";
                }
                
                catch(IllegalArgumentException e)
                {
                    e.printStackTrace();
                }
            }
            if(full)
            {
                result += "\n";
                
                
                Method[] methods = o.getClass().getMethods();
                for(Method m : methods)
                {
                    result += "\n\t" + m.toString();
                }
            }
        }
        catch(SecurityException e)
        {
            result += "\nNot permitted to reflect into objects in this environment.";
        }
        return result + "\n}";
    }
}
