package atraxi.game.UI;

/**
 * Created by Atraxi on 21/09/2015.
 */
@FunctionalInterface
public interface CustomCallable<T1, T2>
{
    public T2 call(T1 arg);
}
