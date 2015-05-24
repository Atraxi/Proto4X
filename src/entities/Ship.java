package entities;

import debug.DebugUtil;
import atraxi.game.Game;

public class Ship extends Entity
{
    private double orientationTarget, angularVelocity;
    private double targetX, targetY;
    private static final double MAXROTATIONSPEED = 0.05;
    private static final double ROTATIONACCELERATION = 0.0001;
    private static final double MAXTRAVELSPEED = 2.0;
    private static final double TRAVELACCELERATION = 0.2;
    
    public Ship(int x, int y)
    {
        super("ship", x, y);
        targetX=x;
        targetY=y;
        angularVelocity = 0;
    }
    
    @Override
    public void rightClickCommand(double x, double y)
    {
        x-=getImage().getWidth(null)/2;
        y-=getImage().getHeight(null)/2;
        targetX=x;
        targetY=y;
        x -= this.x;
        y -= this.y;
        orientationTarget = Math.atan2(y,x);
        System.out.println("Right Click Command -> Ship:"+this+"\n\tx:"+x+" y:"+y+" orientationTarget:"+orientationTarget+" orintationDelta:"+(orientationTarget - orientation));
    }
    
    @Override
    public void doWork(long timeDiff, boolean paused)
    {
        if(!paused)
        {
            //the amount we need to rotate
            double orientationDelta = orientationTarget - orientation;//TODO: adjust orientationDelta to factor in the current angularVelocity
            double orientationDeltaAdjusted = orientationDelta + (angularVelocity * angularVelocity) / ROTATIONACCELERATION;
            
            //TEST:
            
            if(Math.abs(orientationDelta) > 0.001 || Math.abs(angularVelocity) > 0.01)//fix >pi rotations
            {
                angularVelocity = Math.signum(orientationDelta) * 0.05;
                
                orientation += angularVelocity;
            }
            
            
            //remove direction from the equation, 'cleans' up the code by avoiding needing to check +/- for every calculation
            //this does make the math somewhat less intuitive however
            //int direction = (int) Math.signum(orientationDelta);
            //orientationDelta = Math.abs(orientationDelta);
/*            
            //do we need to rotate?
            if(orientationDeltaAdjusted>0.001 || orientationDeltaAdjusted<-0.001 || angularVelocity>0.001 || angularVelocity<-0.001)
            {
              //take the shortest path; if the change in rotation is > pi then subtract a full rotation
                //this inverts the direction to rotate in
                if(orientationDeltaAdjusted>Math.PI)
                {
                    orientationDeltaAdjusted = -1 * Math.signum(orientationDeltaAdjusted) * (2*Math.PI - Math.abs(orientationDeltaAdjusted));
                    //direction = direction * -1;
                }
                
                //are we supposed to be slowing down?
                //is:
                //the velocity next tick
                //greater than:
                //the velocity that can be achieved by constant acceleration over the remaining distance (v^2=ad - substitution of v=d/t and a=v/t)
                if((angularVelocity+ROTATIONACCELERATION)*(angularVelocity+ROTATIONACCELERATION) > orientationDeltaAdjusted*ROTATIONACCELERATION)
                {//decelerate (rotation)
                    //angularVelocity-=(-direction) -*-=+
                    angularVelocity+=Math.signum(orientationDeltaAdjusted)*ROTATIONACCELERATION;
                    orientation += Math.signum(orientationDeltaAdjusted) * (angularVelocity * timeDiff)/Game.DELAY;
                }
                else //we don't need to slow down yet
                {
                    //slightly counter-intuitive, but allows the speed to be above maximum when pushed by an external force 
                    angularVelocity += Math.signum(orientationDeltaAdjusted) * Math.min(MAXROTATIONSPEED - angularVelocity, ROTATIONACCELERATION);
                    
                    //orientation += direction * (Math.min(orientationDelta, angularVelocity) * timeDiff)/Game.DELAY;
                    orientation +=  (angularVelocity * timeDiff)/Game.DELAY;
                    
                    //keep rotation within a range of 2pi, i.e. -pi to pi
                    if(orientation>Math.PI)
                    {
                        orientation-=2*Math.PI;
                    }
                    else if(orientation<-Math.PI)
                    {
                        orientation+=2*Math.PI;
                    }
                }
                
                //if desired change in orientation is approximately 0 (floating point inaccuracy)
                //then force speed to be 0 to prevent any possible drift (however negligible), and orientation to be exactly what is desired
                //  (should I even bother setting orientation exactly? the calculated target will have rounding errors anyway)
                if(!(orientationDelta>0.001 || orientationDelta<-0.001))
                {
                    angularVelocity=0;
                    orientation = orientationTarget;
                }
            }
            //if we are not where we were told to be
            */else if(x!=targetX || y!=targetY)
            {
                //TODO: acceleration and deceleration
                double xDiff = targetX-x;
                double yDiff = targetY-y;
                double pythagorasPart = Math.sqrt(xDiff*xDiff + yDiff*yDiff);
                
                //if()
                
                if(pythagorasPart<MAXTRAVELSPEED)
                {
                    x=targetX;//x+=xDiff;
                    y=targetY;//y+=yDiff;
                }
                else
                {
                    x += ((MAXTRAVELSPEED*xDiff)/pythagorasPart);
                    y += ((MAXTRAVELSPEED*yDiff)/pythagorasPart);
                }
                
            }
            //TODO: refactor to allow drag for target orientation
//            else if()
//            {
//                
//            }
        }
    }
}
