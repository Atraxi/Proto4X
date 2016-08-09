package atraxi.core.util

/**
 * Created by Atraxi on 19/05/2016.
 */
class DebugState(val isExpandedZoomEnabled: Boolean,
                 /**
                  * @return An indication of how verbose various info displays should be, where 0 is the absolute minimum
                  */
                 val detailedInfoLevel: Int) {

    override fun toString(): String {
        return "DebugState{" +
                "expandedZoom=" + isExpandedZoomEnabled +
                ", detailedInfoLevel=" + detailedInfoLevel +
                '}'
    }
}
