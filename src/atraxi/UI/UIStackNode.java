package atraxi.ui;

/**
 * Created by Atraxi on 1/09/2015.
 */
public interface UIStackNode extends UIElement
{
    UIStackNode getNextNode();

    UIStackNode getPreviousNode();

    void setNextNode(UIStackNode element);

    void setPreviousNode(UIStackNode element);
}
