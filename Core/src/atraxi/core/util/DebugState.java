package atraxi.core.util;

/**
 * Created by Atraxi on 19/05/2016.
 */
public class DebugState
{
    private boolean expandedZoom;
    private int infoLevel;

    public DebugState(boolean expandedZoom, int infoLevel)
    {
        this.expandedZoom = expandedZoom;
        this.infoLevel = infoLevel;
    }

    public boolean isExpandedZoomEnabled()
    {
        return expandedZoom;
    }

    /**
     * @return An indication of how verbose various info displays should be, where 0 is the absolute minimum
     */
    public int getDetailedInfoLevel()
    {
        return infoLevel;
    }

    @Override
    public String toString()
    {
        return "DebugState{" +
               "expandedZoom=" + expandedZoom +
               ", detailedInfoLevel=" + infoLevel +
               '}';
    }
}
