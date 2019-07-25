using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Evacuee : MonoBehaviour
{   
    

    public GameObject searchedZonePrefab;
    public GameObject lineHolderPrefab;

    public FieldOfView fov;

    public float sensorAngle;
    public float sensorRadius;
    public int edgeResolveIterations;
    public int gapResolveIterations;

    public float gapScanDegrees; //fidelity of the scan for walls in a gap

    public LayerMask wallMask;

    public Wall leftWall;

    public Wall rightWall;

    public LineRenderer rightWallLine;
    public LineRenderer leftWallLine;

    public float wallYVal;
    public float wallErrorDst;

    private NavMeshAgent agent;
    public Transform Target;


    public enum EvacueeMovementState{
        toExit,toStairs,wandering
    }
    public EvacueeMovementState curMoveState = EvacueeMovementState.wandering;

    public float updateFreq;
    private float timer;

    




    private List<GameObject> seenExits = new List<GameObject>();
    private List<GameObject> seenStairs = new List<GameObject>();

    void Start(){
        fov = gameObject.GetComponent<FieldOfView>();
        agent = gameObject.GetComponent<NavMeshAgent>();
        //agent.SetDestination(Target.position);
        SetUp();
        move(Target.position);
        timer = updateFreq;
        Vector3 leftdir = DirFromAngle(-sensorAngle,false);
    }

    void LateUpdate(){
         //clear seenExits
        seenExits.Clear();

        //if the agent sees any exits
        foreach(Transform exit in fov.visibleTargets)
        {
            //check if it is already seen
            
            //Debug.Log("Saw " + exit);
            //add the exit to seenExits
            seenExits.Add(exit.gameObject);
        }
        //if the agent sees stairs, add it to the seenStairs
        

        //if the agent sees a new stair, use it
        timer+=Time.deltaTime;
        if(timer>=updateFreq){
            timer = 0;
        //Fire rays to the left and right
        Vector3 leftdir = DirFromAngle(-sensorAngle,false);
        Debug.DrawRay(transform.position, leftdir*100,Color.green, 10.0f);
        RaycastHit hit;
        //if we hit something
        if(Physics.Raycast(transform.position,leftdir,out hit,sensorRadius,wallMask)){
            //Debug.Log("hit!");
            bool onWall = leftWall.add(hit.point,this.transform);
            //If the point hit is not on the wall
            if(!onWall){
                //get the closest edge of the wall to the miss point
                Vector3 wallEdge = leftWall.closestEdgePoint(hit.point);
                //find the edge of the wall
                FindWallEdge(-sensorAngle,wallEdge,leftWall);
                wallEdge = leftWall.closestEdgePoint(hit.point);//update the wallEdge
                //check for gap
                bool gapFound = isGap(wallEdge,leftWall);
                Debug.Log("Gap found: " +gapFound);

                //if a gap is found
                if(gapFound){
                    //identify gap
                    identifyGap(wallEdge,leftWall);
                }
                
                

            }
            leftWallLine.SetPositions(new Vector3[]{leftWall.getStartPoint(),leftWall.getEndPoint()});
        }
        //if we did not hit something
        else{

        }
        }
    }

    private void SetUp(){
        //set up the left wall
        RaycastHit hit;
        Vector3[] points = new Vector3[2];
        points[0].y = -100;
        points[1].y = -100;
        /* Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left)*100,Color.red, 10.0f);
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left+ new Vector3(0,0,0.1f))*100,Color.blue, 10.0f);*/
        if(Physics.Raycast(transform.position,transform.TransformDirection(Vector3.left),out hit,sensorRadius,wallMask)){
            points[0] = hit.point;
            points[0].y = wallYVal;
        }
        if(Physics.Raycast(transform.position,transform.TransformDirection(Vector3.left+ new Vector3(0,0,0.1f)),out hit,sensorRadius,wallMask)){
            points[1] = hit.point;
            points[1].y = wallYVal;
        }
        if(points[0].y!=-100&&points[1].y!=-100){
            leftWall = new Wall(points[0],points[1],wallYVal,agent.radius*2+0.1f,wallErrorDst);
            leftWallLine.SetPositions(points);
            leftWallLine.enabled = true;
            Debug.Log("Left Wall Made!");
        }
        //fire to the left
        //fire slightly up from the left
        //set up the right wall
    }

    /*
        Move the evacuee to a given location

        @param location the location the agent is to move to
     */
    private void move(Vector3 location){
        agent.Warp(location);
    }

    private void FindWallEdge(float missAngle, Vector3 wallEdge, Wall theWall){
        Vector3 toWallEdge = wallEdge-transform.position;
        toWallEdge.y = 0;
        float minAngle = Vector3.SignedAngle(transform.TransformDirection(Vector3.forward),toWallEdge,Vector3.up);
        float maxAngle = missAngle;


        for(int i = 0; i <edgeResolveIterations; i++){
            float angle = (minAngle + maxAngle)/2;

            //fire at the angle
            RaycastHit hit;
            //Debug.DrawRay(transform.position, DirFromAngle(angle,true)*100,Color.black, 10.0f);
            //if we hit something
            if(Physics.Raycast(transform.position,DirFromAngle(angle,true),out hit,sensorRadius,wallMask)){
                bool onWall = leftWall.add(hit.point,this.transform);
                //If the point hit is not on the wall
                if(!onWall){
                    //update max angle
                    maxAngle = angle;
                }
                else{
                    //update min point
                    minAngle = angle;

                }
            }
            else{
                maxAngle = angle;
            }
        }

            //update max and min values

            /* ViewCastInfo newViewCast = ViewCast (angle);

            bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
            //if the new viewcast hit the object and the edge distance threshold is not exceeded
            if(newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded){
                minAngle = angle;
                minPoint = newViewCast.point;
            } 
            else {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }*/
        
    }

    /*
        Detect if a gap is present after missing a wall.

        @param wallEdge is the edge of the wall that is closest to the hit point of the ray that missed the wall
        @param theWall is the wall that was missed
        @returns if a gap is present
     */
    private bool isGap(Vector3 wallEdge, Wall theWall){
        //We have found the edge of the wall
        //Project slightly less than the gap width past the edge of the wall
        //Fire a ray at that position
        //If it hits something along that wall, then there is no gap
        //if it hits something not along that wall, then there is a gap
        //if it hits nothing, then there is a gap
        Vector3 toWallEdge = wallEdge-transform.position;
        toWallEdge.y = 0;
        float minAngle = Vector3.SignedAngle(transform.TransformDirection(Vector3.forward),toWallEdge,Vector3.up);
        float maxAngle; //determined by projecting past the wall by slightly less than the gap width
        Vector3 spotPastWall; //the spot projected past the wall

        //if the closest edge is the end point of the wall
        if(wallEdge==theWall.getEndPoint()){
            //project past the end point edge of the wall
            spotPastWall = theWall.nextPointAlongWall(agent.radius*2-0.1f,false);
        }
        //if the closest edge is the start point of the wall
        else{
            //project past the start point edge of the wall
            spotPastWall = theWall.nextPointAlongWall(agent.radius*2-0.1f,true);
        }
        spotPastWall = spotPastWall - transform.position; //gets the direction of the spot past the wall
        spotPastWall.y = 0; // make the direction have no height value
        //the max angle is the angle between the forward of the evacuee and the projected position
        maxAngle = Vector3.SignedAngle(transform.TransformDirection(Vector3.forward),spotPastWall,Vector3.up);

        //binary search between min and max angles
        for(int i = 0; i <gapResolveIterations; i++){
            float angle = (minAngle + maxAngle)/2;

            //fire at the angle
            RaycastHit hit;
            //if we hit something
            if(Physics.Raycast(transform.position,DirFromAngle(angle,true),out hit,sensorRadius,wallMask)){
                bool onWall = theWall.add(hit.point,this.transform);
                //If the point hit is not on the wall
                if(!onWall){
                    //update max angle
                    //Debug.DrawRay(transform.position, DirFromAngle(angle,true)*100,Color.black, 10.0f);
                    //Debug.Log("Missed on step "+i);
                    maxAngle = angle;
                }
                //if we hit something along the wall
                else{
                    //Debug.DrawRay(transform.position, DirFromAngle(angle,true)*100,Color.yellow, 10.0f);
                    //Debug.Log("Hit on step " +i);
                    return false;

                }
            }
            //if we did not hit anything
            else{
                //Debug.DrawRay(transform.position, DirFromAngle(angle,true)*100,Color.black, 10.0f);
                //Debug.Log("Missed on step "+i);
                maxAngle = angle;
            }
        }
        //if we fired the gapResolveIterations amount of times and did not hit anything, then there is a gap!
        return true;
    }

    private void identifyGap(Vector3 wallEdge, Wall theWall){
        Vector3 spotPastWall;
        //if the edge in question is the end point of the wall, we are working in the direction of the wall
        if(wallEdge == theWall.getEndPoint()){
            //the spot is 1 radius of the agent past the wall
            spotPastWall = theWall.nextPointAlongWall(agent.radius*5,false);
        }
        //if the edge is not the end point of the wall, it is the start point of the wall, and we are working on the opposite direction of the wall
        else{
            //the spot is 1 radius of the agent past the wall
            spotPastWall = theWall.nextPointAlongWall(agent.radius*5,true);
        }
        //project the point onto the direction that the agent was moving
        spotPastWall = Vector3.Project(spotPastWall-transform.position,Vector3.Normalize(transform.forward))+transform.position;
        //move the agent to just past the wall
        move(spotPastWall);

        
        //fire rays around, starting from the last spot of the wall and ending straight ahead
        Vector3 lastSpot = wallEdge;
        lastSpot.y=0;
        Debug.Log("End point of wall: " + lastSpot);
        float angle = Vector3.SignedAngle(transform.forward,lastSpot-transform.position,transform.up);

        //A list of walls to hold all the walls that will be created
        List<Wall> wallList = new List<Wall>();
        RaycastHit hit; //the spot the ray hits
        Vector3 closestWallPoint; //the wall point closest to the wallEdge
        int closestWallIndex = -1; // the index of the closest wall, initially set to a nonsense number
        float closestWallDst = 99999999;
        int currentWall = 0; //the index of the current wall
        Vector3[] wallPoints = new Vector3[2]; //two points that will construct a new wall
        int pointCount = 0; //the number of points added that will construct a new wall

        float toStart;
        float toEnd;

        //if the angle is less than 0, the area in question is on the left
        if(angle<0){
            //round the angle up
            angle = Mathf.Ceil(angle);
            //first pass construction of walls
            
            while(angle<0){
                //Debug.Log("angle: " + angle);
                //get the next spot to shoot at
                Vector3 shotSpot = DirFromAngle(angle,true);
                //Debug.Log("Spot: "+shotSpot*19);

                //fire a ray at the spot
                if(Physics.Raycast(transform.position,DirFromAngle(angle,true),out hit,sensorRadius,wallMask)){
                    //do things to find the right spot

                    bool pointAdded = false;
                    //if it is not the first wall
                    if(currentWall!=0){
                        //try to add the point to the previous wall
                        pointAdded = wallList[currentWall-1].add(hit.point,this.transform);
                    }
                    //if a point was added to the previous wall
                    if(pointAdded){
                        //reset the points to construct the new wall
                        pointCount = 0;
                    }
                    else{
                        //add the hit point to the wallPoint array
                        wallPoints[pointCount] = hit.point;
                        pointCount++;
                        //if the wallPoints is full (pointCount = 2)
                        if(pointCount==2){
                            //reset pointCount
                            pointCount=0;
                            //construct wall
                            wallList.Add(new Wall(wallPoints[0],wallPoints[1],wallYVal,agent.radius*2+0.1f,wallErrorDst));

                            closestWallPoint = getClosestWallPoint(wallList, (currentWall-1),  closestWallIndex, closestWallDst, (agent.radius*2+0.1f), wallEdge);
                            //if the closest wall point is positive infinity, no new closest point was found
                            //if the closest wall point is not positive infinity, a new closest point was found
                            if(closestWallPoint != Vector3.positiveInfinity){
                                //if the distance of the closest wall point is the same as (within 0.01f of) the closest wall distance, the point did not change
                                //if the distance of the closest wall point not within 0.01f of the closest wall distance, the point did change
                                float closestWallPointDst =Vector3.Distance(wallEdge,closestWallPoint);
                                //Debug.Log(currentWall+"th wall ClosestWallPointDst: " + closestWallPointDst);
                                //Debug.Log("closestwalldst: " +closestWallDst);

                                if(Mathf.Abs(closestWallPointDst-closestWallDst) > 0.01f){
                                    closestWallIndex = currentWall-1; // the index of the closest wall, initially set to a nonsense number
                                    closestWallDst = closestWallPointDst;
                                }
                            }
                            /*if(currentWall>0){
                                //check if the last wall is closest to the wallEdge
                                toStart = Vector3.Distance(wallList[currentWall-1].getStartPoint(),wallEdge);
                                toEnd = Vector3.Distance(wallList[currentWall-1].getEndPoint(),wallEdge);
                                //if there has been a closest wall discovered
                                if(numClosestWall>=0){
                                    //compare the previous wall to the closest wall
                                    //if the start point is farther than 2 radius of the agent (the agent can fit through gap), and the start point is closer
                                    //to the wall edge than the current closest wall
                                    if(toStart<closestWallDst&&toStart>=agent.radius*2+0.1f){
                                        //attempt to add toStart
                                        if(!checkForWall(wallEdge,wallList[currentWall-1].getStartPoint())){
                                            numClosestWall = currentWall-1; // the index of the closest wall, initially set to a nonsense number
                                            closestWallDst = toStart;
                                        }
                                    }
                                    //if the start point failed, but the end point has a gap, and the end point is closer to the wall edge
                                    if(toEnd<closestWallDst&&toEnd>=agent.radius*2+0.1f){
                                        //attempt to add toEnd
                                        if(!checkForWall(wallEdge,wallList[currentWall-1].getEndPoint())){
                                            numClosestWall = currentWall-1; // the index of the closest wall, initially set to a nonsense number
                                            closestWallDst = toEnd;
                                        }
                                    }
                                }
                                //if there has not been a closest wall discovered
                                else{
                                    //if there is a gap between the start and the wall
                                    if(toStart>=agent.radius*2+0.1f){
                                        //attempt to add toStart
                                        if(!checkForWall(wallEdge,wallList[currentWall-1].getStartPoint())){
                                            numClosestWall = currentWall-1; // the index of the closest wall, initially set to a nonsense number
                                            closestWallDst = toStart;
                                        }
                                    }
                                    if(toEnd>=agent.radius*2+0.1f&&toEnd<closestWallDst){
                                        //attempt to add toEnd
                                        if(!checkForWall(wallEdge,wallList[currentWall-1].getEndPoint())){
                                            numClosestWall = currentWall-1; // the index of the closest wall, initially set to a nonsense number
                                            closestWallDst = toEnd;
                                        }
                                    }
                                }
                                Debug.Log("Wall " + (currentWall-1));
                                if(toStart<toEnd){
                                    Debug.Log(toStart);
                                }
                                else{
                                    Debug.Log(toEnd);
                                }
                                
                                Debug.Log(numClosestWall + "th Wall is closest");
                            }*/
                            currentWall++;
                        }
                    }
                    

                }
                Debug.DrawRay(transform.position, shotSpot*sensorRadius,Color.cyan, 10.0f);
                angle+=gapScanDegrees;
            }
        }
        /* //after scanning to directly in front of the agent, check if the last wall is closest to the wallEdge
        closestWallPoint = getClosestWallPoint(wallList, currentWall,  closestWallIndex, closestWallDst, (agent.radius*2+0.1f), wallEdge);
        //if the closest wall point is positive infinity, no new closest point was found
        //if the closest wall point is not positive infinity, a new closest point was found
        if(closestWallPoint != Vector3.positiveInfinity){
            //if the distance of the closest wall point is the same as (within 0.01f of) the closest wall distance, the point did not change
            //if the distance of the closest wall point not within 0.01f of the closest wall distance, the point did change
            float closestWallPointDst =Vector3.Distance(wallEdge,closestWallPoint);
            if(closestWallPointDst-closestWallDst > 0.01f){
                closestWallIndex = currentWall; // the index of the closest wall, initially set to a nonsense number
                closestWallDst = closestWallPointDst;
            }
        }*/


        /* Debug.Log("Got before");
                            toStart = Vector3.Distance(wallList[currentWall].getStartPoint(),wallEdge);
                            toEnd = Vector3.Distance(wallList[currentWall].getEndPoint(),wallEdge);

                            Debug.Log("Got after");
                            //if there has been a closest wall discovered
                            if(numClosestWall>=0){
                                //compare the previous wall to the closest wall
                                //if the start point is farther than 2 radius of the agent (the agent can fit through gap), and the start point is closer
                                //to the wall edge than the current closest wall
                                if(toStart<closestWallDst&&toStart>=agent.radius*2+0.1f){
                                    //attempt to add toStart
                                    if(!checkForWall(wallEdge,wallList[currentWall].getStartPoint())){
                                        numClosestWall = currentWall; // the index of the closest wall, initially set to a nonsense number
                                        closestWallDst = toStart;
                                    }
                                }
                                //if the start point failed, but the end point has a gap, and the end point is closer to the wall edge
                                if(toEnd<closestWallDst&&toEnd>=agent.radius*2+0.1f){
                                    //attempt to add toEnd
                                    if(!checkForWall(wallEdge,wallList[currentWall].getEndPoint())){
                                        numClosestWall = currentWall; // the index of the closest wall, initially set to a nonsense number
                                        closestWallDst = toEnd;
                                    }
                                }

                            }
                            //if there has not been a closest wall discovered
                            else{
                                //if there is a gap between the start and the wall
                                if(toStart>=agent.radius*2+0.1f){
                                    //attempt to add toStart
                                    if(!checkForWall(wallEdge,wallList[currentWall].getStartPoint())){
                                        numClosestWall = currentWall-1; // the index of the closest wall, initially set to a nonsense number
                                        closestWallDst = toStart;
                                    }
                                }
                                if(toEnd>=agent.radius*2+0.1f&&toEnd<closestWallDst){
                                    //attempt to add toEnd
                                    if(!checkForWall(wallEdge,wallList[currentWall].getEndPoint())){
                                        numClosestWall = currentWall-1; // the index of the closest wall, initially set to a nonsense number
                                        closestWallDst = toEnd;
                                    }
                                }
                            }*/
        //if the angle is greater than 0, the area in question is on the right
        /* else{
            //round the angle down
            angle = Mathf.Floor(angle);
            while(angle>0){
                //Debug.Log("angle: " + angle);
                Vector3 shotSpot = DirFromAngle(angle,true);
                Debug.Log("Spot: "+shotSpot*19);
                Debug.DrawRay(transform.position, shotSpot*10,Color.cyan, 10.0f);
                angle-=gapScanDegrees;
            }
        }*/
        Debug.Log("Found " + currentWall + " walls!");
        Debug.Log("Closest wall: " + closestWallIndex);
        int wallNum = 0;
        foreach(Wall curWall in wallList){
            GameObject newWall = Instantiate(lineHolderPrefab) as GameObject;
            newWall.transform.parent = this.transform;
            LineRenderer line = newWall.GetComponent<LineRenderer>();
            line.enabled = true;
            line.SetPositions(new Vector3[]{curWall.getStartPoint(),curWall.getEndPoint()});
            float dst = Vector3.Distance(curWall.closestEdgePoint(wallEdge),wallEdge);
            Debug.Log("Wall " + wallNum+ " Distance: " + dst);
            if(closestWallIndex>=0&& /* curWall.getStartPoint() == wallList[numClosestWall].getStartPoint()&& curWall.getEndPoint() == wallList[numClosestWall].getEndPoint()*/curWall.Equals(wallList[closestWallIndex])){
                Debug.Log("Closest Wall Found: Wall "+wallNum);
                //Material theMat = Resources.Load<Material>("Materials/ClosestWallMaterial"/* , typeof(Material)*/) as Material;
                line.material = Resources.Load<Material>("Materials/ClosestWallMaterial"/* , typeof(Material)*/) as Material;;
            }
            wallNum++;
        }
        
        
            //construct walls while this happens
            //keep a pointer to the closest point to the wall
                //the closest point cannot be within agent's width of the other point
                //you have to check if there is a wall between the closest point and the edge of the wall
                    //fire a ray halfway between those points, see if it hits
    }

    /*
        Determine if the new wall is closer to the point, ensuring that there is a gap between the point and the edge of the wall. Returns the point closest to
        the point

        @param wallList the list of walls
        @param wallIndex the index of the wall to be checked against the previous closest point
        @param closestWallIndex the index of the closest wall, a negative value represents no closest wall being determined
        @param minDst the previous closest wall's distance to the point
        @param gap the minimum length of the gap that must be present if a gap exist (agent.radius*2+0.1f)
        @param point distances from this point are calculated and compared (it is the point of interest)

        @returns the wall edge closest to the point, returns positive infinity if there was no closest edge discovered
     */
    private Vector3 getClosestWallPoint(List<Wall> wallList, int wallIndex, int closestWallIndex, float minDst, float gap, Vector3 point){
        Vector3 closestWallPoint = Vector3.positiveInfinity;
        Debug.Log("For " + wallIndex + "th wall:");
        if(wallIndex>=0){
            //check if the new wall is closest to the point
            float toStart = Vector3.Distance(wallList[wallIndex].getStartPoint(),point);
            float toEnd = Vector3.Distance(wallList[wallIndex].getEndPoint(),point);
            //if there has been a closest wall discovered
            if(closestWallIndex>=0){
                //compare the previous wall to the closest wall
                //figure out which point on the closest wall is closest to the point
                //if the distance between the start point of the closest wall is equal (within 0.01f) to the minDst
                if(Mathf.Abs(Vector3.Distance(wallList[closestWallIndex].getStartPoint(),point)-minDst) < 0.01f){
                    //the start point is the current closest wall point
                    closestWallPoint = wallList[closestWallIndex].getStartPoint();
                }
                else{
                    closestWallPoint = wallList[closestWallIndex].getEndPoint();
                }
                Debug.Log("Top route");
                Debug.Log("closestWallPoint: " + closestWallPoint);
                Debug.Log("minDst: "+minDst);
                Debug.Log("toStart: " +toStart);
                Debug.Log("toEnd: "+toEnd);

                //if the start point is farther than 2 radius of the agent (the agent can fit through gap), and the start point is closer
                //to the wall edge than the current closest wall
                if(toStart<minDst&&toStart>=gap){
                    //attempt to add toStart
                    if(!checkForWall(point,wallList[wallIndex].getStartPoint())){
                        closestWallIndex = wallIndex; // the index of the closest wall, initially set to a nonsense number
                        minDst = toStart;
                        closestWallPoint = wallList[closestWallIndex].getStartPoint();
                    }
                    else{
                        Debug.Log("Hit a wall between start and edge");
                    }
                }
                //if the start point failed, but the end point has a gap, and the end point is closer to the wall edge
                if(toEnd<minDst&&toEnd>=gap){
                    //attempt to add toEnd
                    if(!checkForWall(point,wallList[wallIndex].getEndPoint())){
                        closestWallIndex = wallIndex; // the index of the closest wall, initially set to a nonsense number
                        minDst = toEnd;
                        closestWallPoint = wallList[closestWallIndex].getEndPoint();
                    }
                    else{
                        Debug.Log("Hit a wall between end and edge");
                    }
                }
            }
            //if there has not been a closest wall discovered
            else{
                Debug.Log("Bottom route");
                Debug.Log("closestWallPoint: " + closestWallPoint);
                Debug.Log("minDst: "+minDst);
                Debug.Log("toStart: " +toStart);
                Debug.Log("toEnd: "+toEnd);
                //if there is a gap between the start and the wall
                if(toStart>=gap){
                    //attempt to add toStart
                    if(!checkForWall(point,wallList[wallIndex].getStartPoint())){
                        closestWallIndex = wallIndex; // the index of the closest wall, initially set to a nonsense number
                        minDst = toStart;
                        closestWallPoint = wallList[closestWallIndex].getStartPoint();
                    }
                    else{
                        Debug.Log("Hit a wall between start and edge");
                    }
                }
                if(toEnd>=gap&&toEnd<minDst){
                    //attempt to add toEnd
                    if(!checkForWall(point,wallList[wallIndex].getEndPoint())){
                        closestWallIndex = wallIndex; // the index of the closest wall, initially set to a nonsense number
                        minDst = toEnd;
                        closestWallPoint = wallList[closestWallIndex].getEndPoint();
                    }
                    else{
                        Debug.Log("Hit a wall between end and edge");
                    }
                }
            }
            //Debug.Log("Wall " + wallIndex);
            /* if(toStart<toEnd){
                Debug.Log(toStart);
            }
            else{
                Debug.Log(toEnd);
            }*/
            //Debug.Log(closestWallIndex + "th Wall is closest");
        }
        Debug.Log("Returning: " +closestWallPoint);
        return closestWallPoint;
    }

    /*
        Checks if there is a wall between two given points
     */
    private bool checkForWall(Vector3 firstSpot, Vector3 secondSpot){
        //construct a possible wall out of the points
        Wall posWall = new Wall(firstSpot,secondSpot,wallYVal,agent.radius*2+0.1f,wallErrorDst);
        //fire a wall at the middle of the possible wall
        RaycastHit hit;
        if(Physics.Raycast(transform.position,posWall.getMidPoint()-transform.position,out hit,sensorRadius,wallMask)){
            return posWall.checkIsOnWall(hit.point);
        }
        else 
            return false;
    }

    private bool recurWallCheck(Wall toCheck, Vector3 minPoint, Vector3 maxPoint, int recurCount){
        bool recurHit = false;
        RaycastHit hit;
        Vector3 midPoint =(minPoint+maxPoint)/2;
        if(Physics.Raycast(transform.position, midPoint-transform.position,out hit,sensorRadius,wallMask)){
            if(toCheck.checkIsOnWall(hit.point)){
                if(recurCount>1){
                    //check from min to mid point
                    recurHit = recurWallCheck(toCheck, minPoint, maxPoint, recurCount-1);
                    //if min-mid hit
                    if(recurHit){
                        //check from mid to max point
                        recurHit = recurWallCheck(toCheck,midPoint, maxPoint, recurCount-1);
                    }
                }
                else{
                    recurHit = true;
                }
            }
        }
        return recurHit;
    }


    public Vector3 DirFromAngle(float angleInDegrees,bool angleIsGlobal){
        //if the angle is not global, add the y transform
        if(!angleIsGlobal){
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),0,Mathf.Cos(angleInDegrees*Mathf.Deg2Rad));
    }


}
