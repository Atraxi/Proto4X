package atraxi.client.ui

/**
 * Created by Atraxi on 1/09/2015.
 */
interface UIStackNode : UIElement {
    var nextNode: UIStackNode

    var previousNode: UIStackNode
}
