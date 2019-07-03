using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall:MonoBehaviour
{
    public Vector3 startPoint;
    public Vector3 endPoint;

    private Vector3 midPoint;

    public Vector3 dir; //direction of the wall
    
    private float errorDist; //the maximum distance from the wall that a new point can be to be considered on the wall
    public int numPoints;

    private enum quadrant{
        I,II,III,IV
    }


    /*
        Constructor
        Requires 2 points to create a wall
     */
    public Wall(Vector3 _startPoint, Vector3 _endPoint, float _errorDist){
        _endPoint.y = _startPoint.y;
        startPoint = _startPoint;
        endPoint = _endPoint;
        midPoint = (startPoint+endPoint)/2;
        dir = endPoint - startPoint;
        errorDist = _errorDist;
        numPoints = 2;
    }
    public Vector3 getStartPoint(){
        return startPoint;
    }
    public Vector3 getEndPoint(){
        return endPoint;
    }
    public Vector3 getMidPoint()
    {
        return midPoint;
    }

    /*
        Checks if a given point is within errorDist of the wall
        Assumes that height of point&wall does not matter
     */
    public bool isAlongWall(Vector3 point){
        //pretend the point is at the same height as the wall vector
        point.y = dir.y;
        //if the distance from the wall vector is less than the tolerable error distance
        if(Vector3.Distance(Vector3.Project(point,dir),point)<errorDist){
            //the point is along the wall
            return true;

        }
        //if the distance from the wall vector is greater than the tolerable error distance
        else
        {
            //the point is not along the wall
            return false;
        }
    }

    /*
        Detects if a given point is before the wall line, within the wall line, or after the wall line
        Returns a negative value if the point comes before the wall line, 0 if the point is within the wall line, or a positive value if the point comes
        after the wall line

        Assumes the height of the wall&point do not matter
     */
    public int getLineRegion(Vector3 point){
        //pretend the point is at the same height as the wall vector
        point.y = dir.y;
        Vector3 fromStart = point-startPoint; //the direction from the start point to the point
        Vector3 fromEnd = point-endPoint; //the direction from the end point to the point

        //if the angle between the direction of the wall and the direction from the start point to the point is greater than 90
        if(Vector3.Angle(dir,fromStart)>90){
            //the point is before the start point
            return -1;
        }
        //if the angle between the direction of the wall and the direction from the end point is greater than or equal to 90
        else if(Vector3.Angle(dir,fromEnd)>=90){
            //the point is between the end and start
            return 0;
        }
        //if the point is the same as the end point
        else if(endPoint==point){
            //the point is between the end and start
            return 0;
        }
        //if the angle is not the same as the end point
        else{
            //the point is after the end point
            return 1;
        }
    }
    
    /*
        Adds a point to the wall if the point is along the wall, and if it is outside the current endpoints, updates the endpoints

        Assumes the height of the wall & point do not matter
     */
    public void add(Vector3 point){
        //pretend the point is at the same height as the wall vector
        point.y = dir.y;
        //if the distance from the wall vector is less than the tolerable error distance
        if(isAlongWall(point)){
            int region = getLineRegion(point); //the part of the wall line the point is on
            //if the region is less than 0, the point is before the start of the wall line
            if(region<0)
            {
                //update the direction
                updateDir(point-startPoint,true);
                //update the start point
                startPoint = Vector3.Project(point,dir);
            }
            //if the region is greater than 0, the point is after the end of the wall line
            else if(region>0)
            {
                //update the direction
                updateDir(point-endPoint,false);
                //update the end point
                endPoint = Vector3.Project(point,dir);
            }
        }
    }

    /*
        Adds a point to the dir variable
     */
    private void updateDir(Vector3 point,bool beforeStart){
        //if the point is before the start point
        if(beforeStart){
            //reflect the point so that it is in the same direction as dir (this avoids destroying the dir vector)
            point = Vector3.Reflect(point,dir);
        }
        //update dir with the direction 
        dir=(dir*numPoints+point)/(numPoints+1);
        numPoints++;
    }


    /*
        Gets the Plane Geometry Quadrant (top right = I, top left = II, bottom left = III, bottom right = IV)
     */
    private quadrant getQuadrant(Vector3 point){
        //if x is greater than (or equal to) 0, the point is in quad I or quad IV
        if(point.x>=0){
            //if z is greater than (or equal to) 0, the point is in quad I
            if(point.z>=0){
                return quadrant.I;
            }
            //if z is not greater than (or equal to) 0, the point is in quad IV
            else
            {
                return quadrant.IV;
            }
        }
        //if x is less than 0, the point is in quad II or III
        else{
            //if z is greater than (or equal to) 0, the point is in quad II
            if(point.z>=0){
                return quadrant.II;
            }
            //if z is less than 0, the point is in quad III
            else{
                return quadrant.III;
            }
        }
    }
}
