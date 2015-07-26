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
                    boolean wasAccessible = f.isAccessible();
                    if(setAccessible)
                    {
                        f.setAccessible(true);
                    }
                    result += f.get(f).toString();
                    if(setAccessible && !wasAccessible)
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
    
    public static boolean accessSuperclassBoolean(Object o, String fieldName)
    {
        try
        {
            Field[] fields = o.getClass().getSuperclass().getDeclaredFields();
            for(Field f : fields)
            {
                if(f.getName().equals(fieldName))
                {
                    f.setAccessible(true);
                    return f.getBoolean(o);
                }
            }
        }
        catch (IllegalArgumentException e)
        {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }
        catch (IllegalAccessException e)
        {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }
        catch (SecurityException e)
        {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }
        return false;
    }
    
    public static double accessSuperclassInt(Object o, String fieldName)
    {
        try
        {
            Field[] fields = o.getClass().getSuperclass().getDeclaredFields();
            for(Field f : fields)
            {
                if(f.getName().equals(fieldName))
                {
                    f.setAccessible(true);
                    return f.getInt(o);
                }
            }
        }
        catch (IllegalArgumentException e)
        {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }
        catch (IllegalAccessException e)
        {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }
        catch (SecurityException e)
        {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }
        return 0;
    }
}
