package atraxi.client.ui.wrappers;

import atraxi.client.ui.UIElement;
import atraxi.client.ui.UserInterfaceHandler;
import atraxi.client.util.CheckedRender;
import atraxi.client.util.ResourceManager.ImageID;
import atraxi.core.util.Globals;
import atraxi.core.util.Logger;
import atraxi.core.world.GridTile;

import java.awt.Point;
import java.awt.Polygon;
import java.awt.Rectangle;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;

/**
 * Created by Atraxi on 19/05/2016.
 */
public class GridTileUIWrapper implements UIElement
{
    private ImageID imageDefault, imageHover, imageClick, imageSelected;
    //previousState is used to roll rollback a mouse press+drag
    private GridTileUIWrapper.TileState state, previousState;
    private int xCoord, yCoord;
    private Polygon dim;
    private GridTile gridTile;
    private int XCoord;
    private int YCoord;

    private GridTileUIWrapper(GridTile gridTile, ImageID imageDefault, ImageID imageHover, ImageID imageClick, ImageID imageSelected)
    {
        this.gridTile = gridTile;
        this.imageDefault = imageDefault;
        this.imageHover = imageHover;
        this.imageClick = imageClick;
        this.imageSelected = imageSelected;
        state = GridTileUIWrapper.TileState.DEFAULT;
    }

    GridTileUIWrapper(GridTile gridTile, Polygon dim, ImageID imageDefault, ImageID imageHover, ImageID imageClick, ImageID imageSelected)
    {
        this(gridTile, imageDefault, imageHover, imageClick, imageSelected);
        Rectangle dimBounds = dim.getBounds();
        //horizontally is the same as rectangles
        xCoord = gridTile.getXIndex() * dimBounds.width
                 //except offset by half the width of a hex every second row
                 //if xIndex is even, then add 0, else add dimBounds.width/2
                 + ((gridTile.getYIndex() % 2) * dimBounds.width / 2);
        yCoord = gridTile.getYIndex() * (3 * dimBounds.height / 4);
        this.dim = new Polygon(dim.xpoints, dim.ypoints, dim.npoints);
        this.dim.translate(xCoord, yCoord);
    }

    @Override
    public UIElement mousePressed(MouseEvent paramMouseEvent)
    {
        boolean mouseInBounds = WorldUIWrapper.getGridTileIndex(paramMouseEvent.getX(), paramMouseEvent.getY(), WorldUIWrapper.getTileBounds().width, WorldUIWrapper
                .getTileBounds().height)
                .equals(new Point(gridTile.getXIndex(),  gridTile.getYIndex()));
        if(mouseInBounds)
        {
            //store previous tile state in-case the click is cancelled
            //hover is not a meaningful state for us to revert to, so just set to default for that case
            if(state == TileState.HOVER)
            {
                previousState = TileState.DEFAULT;
            }
            else
            {
                previousState = state;
            }
            state = TileState.PRESSED;
            return this;
        }
        //should never happen unless math fails to determine the correct tile
        else
        {
            Logger.log(Logger.LogLevel.debug, new String[]{"World.GridTile mousePressed called without mouse in bounds. Should never happen. Indicates an error or edge case " +
                                                           "in click coordinates -> tile index math"});
            state = TileState.DEFAULT;
            return null;
        }
    }

    @Override
    public UIElement mouseReleased(MouseEvent paramMouseEvent)
    {
        //if we are finishing up from a mousePressed() event on this tile
        if(state == TileState.PRESSED)
        {
            boolean mouseInBounds = getGridTileIndex(paramMouseEvent.getX(), paramMouseEvent.getY(), tileBounds.width, tileBounds.height).equals(new Point(gridTile.getXIndex(),
                                                                                                                                                           gridTile.getYIndex()));
            //if the start (assumed at this point since we are 'pressed') of the click and the end are both on this tile
            //i.e. we are clicked
            if(mouseInBounds)
            {
                //set the selected state
                state = TileState.SELECTED;
                UserInterfaceHandler.setSelectedTile(this);
                return this;
            }
            //the click was cancelled
            else
            {
                //revert the state
                state = previousState;
            }
        }
        //mouseReleased() always fires on a previously pressed tile (i.e. mousePressed())
        //therefore mouseReleased() on a 'selected' tile can be used to tell us to clear selection
        else if(state == TileState.SELECTED)
        {
            state = TileState.DEFAULT;
            previousState = state;
        }
        return null;
    }

    @Override
    public UIElement mouseDragged(MouseEvent paramMouseEvent)
    {
        return null;
    }

    @Override
    public UIElement mouseMoved(MouseEvent paramMouseEvent)
    {
        boolean mouseInBounds = getGridTileIndex(paramMouseEvent.getX(), paramMouseEvent.getY(), tileBounds.width, tileBounds.height).equals(new Point(gridTile.getXIndex(),
                                                                                                                                                       gridTile.getYIndex()));
        if(mouseInBounds)
        {
            if(state == TileState.DEFAULT)
            {
                state = TileState.HOVER;
            }
            return this;
        }
        else if(state == TileState.HOVER)
        {
            state = TileState.DEFAULT;
        }
        return null;
    }

    @Override
    public UIElement mouseWheelMoved(MouseWheelEvent paramMouseWheelEvent)
    {
        return null;
    }

    @Override
    public void paint(CheckedRender render)
    {
        if(Globals.debug.getDetailedInfoLevel() > 3)
        {
            render.drawString("x:" + gridTile.getXIndex(), xCoord + 50, yCoord + 50, dim.getBounds());
            render.drawString("y:" + gridTile.getYIndex(), xCoord + 50, yCoord + 60, dim.getBounds());
        }
        switch(state)
        {
            case DEFAULT:
                render.drawImage(imageDefault, xCoord, yCoord, null);
                break;
            case HOVER:
                render.drawImage(imageHover, xCoord, yCoord, null);
                break;
            case PRESSED:
                render.drawImage(imageClick, xCoord, yCoord, null);
                break;
            case SELECTED:
                render.drawImage(imageSelected, xCoord, yCoord, null);
        }
        if(gridTile.getEntity() != null)
        {
            gridTile.getEntity().paint(render);
        }
    }

    public int getXCoord()
    {
        return XCoord;
    }

    public int getYCoord()
    {
        return YCoord;
    }

    private enum TileState
    {
        DEFAULT, HOVER, PRESSED, SELECTED
    }
}