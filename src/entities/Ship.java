package entities;

public class Ship extends Entity
{
    private double              orientationTarget;
    private double              targetX, targetY;
    private static final double MAXROTATIONSPEED = 0.05;
    private static final double MAXTRAVELSPEED   = 2.0;
    
    public Ship(String type, int x, int y)
    {
        super(type, x, y);
        targetX = x;
        targetY = y;
    }
    
    @Override
    public void doWork(long timeDiff, boolean paused)
    {
        if ( !paused)
        {
            // the amount we need to rotate
            double orientationDelta = orientationTarget - orientation;
            double orientationDirection = Math.signum(orientationDelta);
            orientationDelta = Math.abs(orientationDelta);
            // keep within the range -Pi < x < Pi
            if (orientationDelta > Math.PI)
            {
                orientationDelta -= 2 * Math.PI;
                orientationDirection *= Math.signum(orientationDelta);
                orientationDelta = Math.abs(orientationDelta);
            }
            if (orientationDelta > 0.001)
            {
                orientation += orientationDirection
                        * Math.min(orientationDelta, MAXROTATIONSPEED);
            }
            // if we are not where we were told to be
            else if ((x != targetX) || (y != targetY))
            {
                // TODO: acceleration and deceleration
                double xDiff = targetX - x;
                double yDiff = targetY - y;
                double pythagorasPart = Math.sqrt((xDiff * xDiff)
                        + (yDiff * yDiff));
                if (pythagorasPart < MAXTRAVELSPEED)
                {
                    x = targetX;// x+=xDiff;
                    y = targetY;// y+=yDiff;
                }
                else
                {
                    x += ((MAXTRAVELSPEED * xDiff) / pythagorasPart);
                    y += ((MAXTRAVELSPEED * yDiff) / pythagorasPart);
                }
                // TODO: refactor to allow drag for target orientation
                // else if()
                // {
                //
                // }
            }
        }
    }
    
    @Override
    public void rightClickCommand(double x, double y)
    {
        x -= getImage().getWidth(null) / 2;
        y -= getImage().getHeight(null) / 2;
        targetX = x;
        targetY = y;
        x -= this.x;
        y -= this.y;
        orientationTarget = Math.atan2(y, x);
        System.out.println("Right Click Command -> Ship:" + this + "\n\tx:" + x
                + " y:" + y + " orientationTarget:" + orientationTarget
                + " orintationDelta:" + (orientationTarget - orientation));
    }
}
