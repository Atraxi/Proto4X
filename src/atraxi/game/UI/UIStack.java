package atraxi.game.UI;

public class UIStack
{
    private UIElement head;
    public UIStack()
    {

    }

    public void push(UIElement newNode)
    {
        if(head!=null)
        {
            head=newNode;
        }
        else
        {
            head.setNextNode(newNode);
        }
    }
}
