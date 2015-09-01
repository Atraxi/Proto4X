package atraxi.game.UI;

/**
 * Created by Atraxi on 1/09/2015.
 */
public interface UIStackNode extends UIElement
{
    public UIStackNode getNextNode ();

    public void setNextNode(UIStackNode element);
}
