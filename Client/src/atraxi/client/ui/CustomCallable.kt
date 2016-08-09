package atraxi.client.ui

/**
 * @param  Input arg
 * *
 * @param  Return type
 */
@FunctionalInterface
interface CustomCallable<T1, T2> {
    fun call(arg: T1): T2
}
