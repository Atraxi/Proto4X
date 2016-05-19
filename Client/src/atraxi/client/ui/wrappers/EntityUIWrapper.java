package atraxi.client.ui.wrappers;

import atraxi.client.util.CheckedRender;
import atraxi.client.util.ResourceManager.ImageID;
import atraxi.core.entities.Entity;

import java.awt.geom.AffineTransform;

/**
 * Created by Atraxi on 19/05/2016.
 */
public abstract class EntityUIWrapper
{
    private final Entity entity;
    private ImageID imageID;

    public EntityUIWrapper(Entity entity, ImageID imageID)
    {
        this.entity = entity;
        imageID = ImageID.valueOf(entity.getType());
    }

    public ImageID getImageID ()
    {
        return imageID;
    }

    public void paint(CheckedRender render)
    {
        render.drawImage(imageID,
                         AffineTransform.getRotateInstance(entity.getOrientationInRadians(),
                                                                    imageID.getImage().getWidth()/2,
                                                                    imageID.getImage().getHeight()/2),
                         null);
    }
}
