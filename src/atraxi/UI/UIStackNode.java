package atraxi.ui;

/**
 * Created by Atraxi on 1/09/2015.
 */
public interface UIStackNode extends UIElement
{
    public UIStackNode getNextNode ();

    public UIStackNode getPreviousNode ();

    public void setNextNode(UIStackNode element);

    public void setPreviousNode(UIStackNode element);
}
