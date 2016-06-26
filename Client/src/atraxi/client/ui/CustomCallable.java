package atraxi.client.ui;

/**
 * @param <T1> Input arg
 * @param <T2> Return type
 */
@FunctionalInterface
public interface CustomCallable<T1, T2>
{
    T2 call(T1 arg);
}