package atraxi.client.ui

import atraxi.client.Proto
import atraxi.client.util.RenderUtil
import atraxi.core.util.Globals
import atraxi.core.util.Logger

import java.awt.event.MouseEvent
import java.awt.event.MouseWheelEvent

class UIStack : UIElement {
    private var head: UIStackNode? = null
    private var tail: UIStackNode? = null

    override fun mousePressed(paramMouseEvent: MouseEvent): UIElement? {
        if (tail != null) {
            var currentNode = tail
            do {
                val nodePressed = currentNode!!.mousePressed(paramMouseEvent)
                if (nodePressed != null) {
                    return nodePressed
                }
                currentNode = currentNode.previousNode
            } while (currentNode != null)
        }
        return null
    }

    override fun mouseReleased(paramMouseEvent: MouseEvent): UIElement? {
        if (tail != null) {
            var currentNode = tail
            do {
                val nodeReleased = currentNode!!.mouseReleased(paramMouseEvent)
                if (nodeReleased != null) {
                    return nodeReleased
                }
                currentNode = currentNode.previousNode
            } while (currentNode != null)
        }
        return null
    }

    override fun mouseDragged(paramMouseEvent: MouseEvent): UIElement? {
        if (tail != null) {
            var currentNode = tail
            do {
                val nodeDragged = currentNode!!.mouseDragged(paramMouseEvent)
                if (nodeDragged != null) {
                    return nodeDragged
                }
                currentNode = currentNode.previousNode
            } while (currentNode != null)
        }
        return null
    }

    override fun mouseMoved(paramMouseEvent: MouseEvent): UIElement? {
        if (tail != null) {
            var currentNode = tail
            do {
                val nodeMouseMoved = currentNode!!.mouseMoved(paramMouseEvent)
                if (nodeMouseMoved != null) {
                    return nodeMouseMoved
                }
                currentNode = currentNode.previousNode
            } while (currentNode != null)
        }
        return null
    }

    override fun mouseWheelMoved(paramMouseWheelEvent: MouseWheelEvent): UIElement? {
        if (tail != null) {
            var currentNode = tail
            do {
                val nodeScrolled = currentNode!!.mouseWheelMoved(paramMouseWheelEvent)
                if (nodeScrolled != null) {
                    return nodeScrolled
                }
                currentNode = currentNode.previousNode
            } while (currentNode != null)
        }
        return null
    }

    override fun paint(render: RenderUtil, hasTurnEnded: Boolean) {
        if (head != null) {
            var currentNode = head
            do {
                currentNode!!.paint(render, hasTurnEnded)
                currentNode = currentNode.nextNode
            } while (currentNode != null)
        }
    }

    fun push(newNode: UIStackNode) {
        if (head == null) {
            head = newNode
            tail = newNode
        } else {
            tail!!.nextNode = newNode
            newNode.previousNode = tail
            tail = newNode
        }
    }

    fun remove(node: UIStackNode) {
        if (node.nextNode != null) {
            node.nextNode.previousNode = node.previousNode
        } else {
            tail = node.previousNode
        }
        if (node.previousNode != null) {
            node.previousNode.nextNode = node.nextNode
        } else {
            head = node.nextNode
        }
    }

    companion object {

        val newTestMenu: UIStackNode
            get() = Menu(Globals.Identifiers.menuBackground,
                    Proto.screenWidth / 2 - 30,
                    Proto.screenHeight / 2 - 30,
                    arrayOf(Button(
                            Globals.Identifiers.buttonDefault,
                            Globals.Identifiers.buttonHover,
                            Globals.Identifiers.buttonClick,
                            60,
                            80,
                            "empty"
                    ) { menu ->
                        Logger.log(Logger.LogLevel.debug, arrayOf("button 1 clicked"))
                        null
                    }, Button(
                            Globals.Identifiers.buttonDefault,
                            Globals.Identifiers.buttonHover,
                            Globals.Identifiers.buttonClick,
                            60,
                            150,
                            "empty"
                    ) { menu ->
                        Logger.log(Logger.LogLevel.debug, arrayOf("button 2 clicked"))
                        null
                    }, Button(
                            Globals.Identifiers.buttonDefault,
                            Globals.Identifiers.buttonHover,
                            Globals.Identifiers.buttonClick,
                            60,
                            220,
                            "Return to game"
                    ) { menu ->
                        Logger.log(Logger.LogLevel.debug, arrayOf("button 3 clicked", "\tmenu closed"))
                        UserInterfaceHandler.uiStack.remove(menu)
                        null
                    }, Button(
                            Globals.Identifiers.buttonDefault,
                            Globals.Identifiers.buttonHover,
                            Globals.Identifiers.buttonClick,
                            60,
                            290,
                            "Quit game"
                    ) { menu ->
                        Logger.log(Logger.LogLevel.debug, arrayOf("quit game"))
                        System.exit(0)
                        null
                    }))
    }
}
