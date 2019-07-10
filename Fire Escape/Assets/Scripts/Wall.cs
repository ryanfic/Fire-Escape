using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall
{
    private float yVal; //the y value you want for the wall
    public Vector3 startPoint;
    public Vector3 endPoint;

    private Vector3 midPoint;

    public Vector3 dir; //direction of the wall

    private float errorDist; //the maximum distance from the wall that a new point can be to be considered on the wall

    private float gapAllowed;
    public int numPoints;

    private int edgeResolveIterations = 2;

    /*
        Constructor
        Requires 2 points to create a wall

        @param _startPoint the first point of the wall
        @param _endPoint the second point of the wall
        @param _yVal the height of the wall
        @param _errorDist the distance from the wall that a new point can be to be considered on the wall
     */
    public Wall(Vector3 _startPoint, Vector3 _endPoint, float _yVal, float _gapAllowed, float _errorDist){
        _endPoint.y = _yVal;
        _startPoint.y = _yVal;
        startPoint = _startPoint;
        endPoint = _endPoint;
        yVal = _yVal;
        midPoint = (startPoint+endPoint)/2;
        dir = (endPoint - startPoint);
        gapAllowed = _gapAllowed;
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

    public Vector3 getDir(){
        return dir;
    }

    /*
        Checks if a given point is within errorDist of the wall
        Assumes that height of point & wall does not matter

        @param point the point to be checked
        @returns if the point is within errorDist of the wall
     */
    public bool isAlongWall(Vector3 point){
        //if the distance from the wall vector is less than the tolerable error distance
        if(distPerpToWall(point)<errorDist){
            //Debug.Log("Is along wall!");
            //the point is along the wall
            return true;
        }
        //if the distance from the wall vector is greater than the tolerable error distance
        else
        {
            //Debug.Log("Is NOT along wall!");
            //the point is not along the wall
            return false;

        }
    }

    
    /*
        Checks the perpendicular distance of a point to the wall by calculating the distance the point is projected to be projected onto the wall

        Assumes the height of the point & wall do not matter

        @param point the point to be checked
        @returns the perpendicular distance of the point to the wall
    */
    public float distPerpToWall(Vector3 point){
        //make the point relative to the midpoint of the wall (since the direction of the wall is relative to 0)
        point = point-midPoint;
        //pretend the point is at the same height as the wall vector
        point.y = dir.y;

        return Vector3.Distance(Vector3.Project(point,dir),point);
    }

    /*
        Projects the point onto the line of the wall, and calculates the distance of the point from the nearest end point of the wall
        if the point is outside of the wall
        Returns a negative number if the point is before the start point of wall, a positive number if the point is after the end point of the wall,
        and returns 0 if the point is within the wall

        @param the point to be checked
        @returns a negative number if the point is before the start point of wall, a positive number if the point is after the end point of the wall,
        and returns 0 if the point is within the wall
     */
    public float distParaFromWallEnd(Vector3 point)
    {
        point = projectOntoWall(point);
        int region = getLineRegion(point);
        if(region == 0){
            return 0f;
        }
        else if (region<0){
            return -Vector3.Distance(point,startPoint);
        }
        else{
            return Vector3.Distance(point,endPoint);
        }
    }

    /*
        Projects the point onto the line of the wall

        @param point the point to be projected onto the wall
     */
    public Vector3 projectOntoWall(Vector3 point){
        //make point relative to 0
        point = point-midPoint;
        //projects the point onto the direction of the wall
        point = Vector3.Project(point,dir);
        //put the point along the wall line
        point = point+midPoint;
        return point;
    }

    /*
        Detects if a given point is before the wall line, within the wall line, or after the wall line
        Returns a negative value if the point comes before the wall line, 0 if the point is within the wall line, or a positive value if the point comes
        after the wall line

        Assumes the height of the wall&point do not matter

        @param point the point to be checked
        @returns a negative value if the point comes before the wall line, 0 if the point is within the wall line, or a positive value if the point comes
        after the wall line
     */
    public int getLineRegion(Vector3 point){
        //pretend the point is at the same height as the yVal
        point.y = yVal;
        Vector3 fromStart = point-startPoint; //the direction from the start point to the point
        //pretend the point is at the same height as the wall vector
        //fromStart.y = dir.y;
        Vector3 fromEnd = point-endPoint; //the direction from the end point to the point
        //pretend the point is at the same height as the wall vector
        //fromEnd.y = dir.y;

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

        @param point the point to be added to the wall
     */
    public void add(Vector3 point, Transform observer){
        //pretend the point is at the same height as the yVal
        point.y = yVal;
        //if the distance from the wall vector is less than the tolerable error distance
        if(isAlongWall(point)){
            int region = getLineRegion(point); //the part of the wall line the point is on
            //Debug.Log("Region"+region);
            bool hitPointOnWall = true; //if a point along the wall has been hit 
            float distFromEnd = distParaFromWallEnd(point);

            //if the region is less than 0, the point is before the start of the wall line
            if(region<0)
            {

                //while the gap between the start point is greater than the gap allowance, or the observer did not hit another point along the wall
                while(-distFromEnd>gapAllowed&&hitPointOnWall)
                {
                    //fire a ray from the observer at the next spot
                    int shotsFired = 0;
                    float nextShotDist = gapAllowed-0.1f; // the distance to the next spot to shoot at (slightly smaller than the gap distance)
                    RaycastHit hit;
                    //shoot a ray from the observer to the position that is nextShotDist along the wall
                    if(Physics.Raycast(observer.position,nextPointAlongWall(nextShotDist,true)-observer.position,out hit, Mathf.Infinity)){
                        //if the ray hits something, check if the ray hit is along the wall
                        hitPointOnWall = isAlongWall(hit.point);
                    }
                    else{
                        //if the ray did not hit anything, it did not hit something along the wall
                        hitPointOnWall = false;
                    }
                    //while a point on the wall has not been hit and the shots fired is less than the number of resolve iterations
                    while(!hitPointOnWall&&shotsFired<edgeResolveIterations)
                    {
                        //half the distance to the point
                        nextShotDist /= 2;
                        //try firing a ray at that next spot
                        if(Physics.Raycast(observer.position,nextPointAlongWall(nextShotDist,true)-observer.position,out hit, Mathf.Infinity)){
                            //if the ray hit something, and the hit is along the wall, mark that a point on the wall has been hit
                            hitPointOnWall = isAlongWall(hit.point);
                        }
                        shotsFired++;
                    }
                    //if a point on the wall has been hit add that point to the wall
                    if(hitPointOnWall){
                        add(hit.point,observer);
                    }
                    //update the distance from the end
                    distFromEnd = distParaFromWallEnd(point);
                }
                //if, after completing the loop, you have hit something along the wall
                if(hitPointOnWall){
                    //update the direction
                    updateDir(point-startPoint,true);
                    //update the start point
                    startPoint = point; //Vector3.Project(point,dir);
                    //ensure y is the same as yval
                    //startPoint.y = yVal;
                    //update the midpoint
                    updateMid(point);
                    numPoints++;
                }
            }
            //if the region is greater than 0, the point is after the end of the wall line
            else if(region>0)
            {
                //while the gap between the start point is greater than the gap allowance, or the observer did not hit another point along the wall
                while(distFromEnd>gapAllowed&&hitPointOnWall)
                {
                    //fire a ray from the observer at the next spot
                    int shotsFired = 0;
                    float nextShotDist = gapAllowed-0.1f; // the distance to the next spot to shoot at (slightly smaller than the gap distance)
                    RaycastHit hit;
                    //shoot a ray from the observer to the position that is nextShotDist along the wall
                    if(Physics.Raycast(observer.position,nextPointAlongWall(nextShotDist,false)-observer.position,out hit, Mathf.Infinity)){
                        //if the ray hits something, check if the ray hit is along the wall
                        hitPointOnWall = isAlongWall(hit.point);
                    }
                    else{
                        //if the ray did not hit anything, it did not hit something along the wall
                        hitPointOnWall = false;
                    }
                    //while a point on the wall has not been hit and the shots fired is less than the number of resolve iterations
                    while(!hitPointOnWall&&shotsFired<edgeResolveIterations)
                    {
                        //half the distance to the point
                        nextShotDist /= 2;
                        //try firing a ray at that next spot
                        if(Physics.Raycast(observer.position,nextPointAlongWall(nextShotDist,false)-observer.position,out hit, Mathf.Infinity)){
                            //if the ray hit something, and the hit is along the wall, mark that a point on the wall has been hit
                            hitPointOnWall = isAlongWall(hit.point);
                        }
                        shotsFired++;
                    }
                    //if a point on the wall has been hit add that point to the wall
                    if(hitPointOnWall){
                        add(hit.point,observer);
                    }
                    //update the distance from the end
                    distFromEnd = distParaFromWallEnd(point);
                }
                if(hitPointOnWall){
                    //update the direction
                    updateDir(point-endPoint,false);
                    //update the end point
                    endPoint = point;//Vector3.Project(point,dir);
                    //ensure y is the same as yval
                    //endPoint.y=yVal;
                    //update the midpoint
                    updateMid(point);
                    numPoints++;
                }
                
            }
            //cannot put the mid update and numpoints increment out here due to the case where region = 0
        }
    }

    public Vector3 nextPointAlongWall(float distance, bool beforeStart){
        //calculate the direction of the point
        Vector3 point = Vector3.Normalize(dir)*distance; 
        if(beforeStart){
            //change signs of x and z values
            point.x = -point.x;
            point.z = -point.z;
            //make the point before the start point
            point = point + startPoint;
        }
        else{
            //make the point after the end point
            point = point + endPoint;
        }
        return point;

    }


    /*
        Updates the dir variable after adding a point to the wall

        @param toAddDir the vector3 to be added to the direction vector
        @param beforeStart if the vector3 is in the opposite direction as the wall's vector
     */
    private void updateDir(Vector3 toAddDir,bool beforeStart){
        //if the point is before the start point
        if(beforeStart){
            //reflect the point so that it is in the same direction as dir (this avoids destroying the dir vector)
            toAddDir = Vector3.Reflect(toAddDir,Vector3.Normalize(dir));
        }
        //update dir with the direction 
        dir=(dir*numPoints+toAddDir)/(numPoints+1);
        //make direction's y = 0, reflect sometimes messes up and makes the y non-zero
        dir.y=0;
    }

    /*
        Updates the midpoint after adding a point to the wall
        
        @param point the point to be added to the midpoint
     */
    private void updateMid(Vector3 point){
        midPoint = (midPoint*numPoints+point)/(numPoints+1);
        midPoint.y=yVal;
    }

}
