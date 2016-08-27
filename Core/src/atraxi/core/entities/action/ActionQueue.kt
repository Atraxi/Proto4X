package atraxi.core.entities.action

import atraxi.core.entities.action.definitions.Action

/**
 * A FIFO linked list for queued actions
 */
class ActionQueue {
    /**
     * Return the Action at the head of the list, without removing it from the list
     * @return
     */
    var head: Action? = null
        private set
    private var tail: Action? = null

    /**
     * Add a new Action to the end of the list
     * @param action
     */
    fun queueAction(action: Action) {
        if (isEmpty) {
            head = action
            tail = action
        } else {
            tail.nextAction = action
            tail = action
        }
    }

    /**
     * Remove the Action at the head of the list, and return it
     * @return
     */
     fun pullAction(): Action? {
        if (head == null) {
            return null
        } else {
            val temp = head
            head = temp.nextAction
            return temp
        }
    }

    val isEmpty: Boolean
        get() = head == null

    /**
     * Replace the current queue with a new queue initialized to one node;
     * @param action
     */
    fun replaceQueue(action: Action) {
        head = action
        tail = action
    }
}