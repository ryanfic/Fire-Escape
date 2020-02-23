using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Evacuee : MonoBehaviour
{   
    public GameObject lineHolderPrefab;
    public GameObject footprintPrefab;

    public FieldOfView fov;
    private SimulationObjectLists objLists;

    public float sensorAngle = 45f;
    public float sensorRadius = 150f;
    public bool familiar = false; //is the agent familiar to the building

    public bool leftSensorTripped; //if a gap has been detected on the left side of the agent
    public bool rightSensorTripped; //if a gap has been detected on the right side of the agent

    public int edgeResolveIterations = 6;
    public int gapResolveIterations = 5;

    public float gapScanDegrees = 0.5f; //fidelity of the scan for walls in a gap

    public LayerMask wallMask;

    public Wall leftWall;

    public Wall rightWall;

    public LineRenderer rightWallLine;
    public LineRenderer leftWallLine;

    public float wallYVal = 1.5f;
    public float wallErrorDst = 0.4f;

    private NavMeshAgent agent;
    public Transform Target = null;
    private List<Vector3> destinations;
    private Vector3 evaDir; //the direction of the agent
    public int leftScanLoc; //the index in the destinations list for the spot where the agent will do a left hand scan
                             //equals int.MaxValue if there is no left scan location
    public int rightScanLoc; //the index in the destinations list for the spot where the agent will do a right hand scan
                              //equals int.MaxValue if there is no right scan location

    private GapInfo leftGapInfo;
    private GapInfo rightGapInfo;

    public bool movingInLeftWallDir = false; //true if agent is moving towards the left wall's endpoint (false if the agent is moving towards the left wall's startpoint)
    public bool movingInRightWallDir = false; //true if agent is moving towards the right wall's endpoint (false if the agent is moving towards the right wall's startpoint)

    public bool leftPeeking;
    public bool rightPeeking;
    public bool leftPeeked;
    public bool rightPeeked;
    public bool hasPeeked;
    public bool movingToRouteStart;

    public float peekRotationSpeed = 75;

    public float peekRotation;

    private Route currentRoute; //startSpot == positive infinity means no route stored
    private RouteSelectSpot prevSelectionSpot;

    private float initializationTime;



    public enum EvacueeMovementState{
        toExit,toStairs,wandering
    }
    public EvacueeMovementState curMoveState = EvacueeMovementState.wandering;

    public float updateFreq = 0.2f;
    private float timer;

    public float footprintFreq = 0.5f;
    private float footprintTimer;




    private List<GameObject> seenExits = new List<GameObject>();
    private List<GameObject> seenWindows = new List<GameObject>();

    void Awake(){
        fov = gameObject.GetComponent<FieldOfView>();
        agent = gameObject.GetComponent<NavMeshAgent>();
        wallMask = LayerMask.GetMask("Wall");
        leftWallLine = gameObject.transform.GetChild(2).GetComponent<LineRenderer>();
        rightWallLine = gameObject.transform.GetChild(1).GetComponent<LineRenderer>();
        //agent.SetDestination(Target.position);
        
        destinations = new List<Vector3>();
        if(Target != null)
        {
            destinations.Add(Target.position);
            evaDir = Target.position - transform.position;

        }
        else
        {

            Vector3 nextDest = gameObject.transform.position+gameObject.transform.forward*5;
            destinations.Add(nextDest);
            evaDir = nextDest - transform.position;
        }
        timer = updateFreq;
        leftSensorTripped = false;
        rightSensorTripped = false;
        leftScanLoc=int.MaxValue;
        rightScanLoc=int.MaxValue;
        leftPeeking = false;
        rightPeeking = false;
        leftPeeked = false;
        rightPeeked = false;
        hasPeeked = false;
        movingToRouteStart = false;
        peekRotation = 0;
        initializationTime = Time.timeSinceLevelLoad;
        footprintTimer = footprintFreq;
    }
    void OnEnable()
    {
        if(familiar){
             GameObject closestExit = getBuildingClosestExit();
            if(closestExit != null){
                move(closestExit.transform.position);
            }
            //move(Target.position);
        }
        else{
            SetUp();
            updateSeenObjects();
            if(Target != null)
            {
                move(Target.position);
            }
            else
            {
                if(seenExits.Count>0)
                {
                    move(seenExits[1].transform.position);
                }
                else{
                    move(gameObject.transform.position+gameObject.transform.forward*100);
                }
            }
        }
    }
    void LateUpdate(){
        if(!familiar){
            if(leftPeeking){
                peek(true);
                updatePeekedObjects();
                //if the peek finished 
                //then the route must be selected
                if(leftPeeked){
                    //and there is no right peeking necessary (rightScanLoc = int.MaxValue)
                    if(rightScanLoc==int.MaxValue){
                        //resolve the route to move down
                        resolveRoute();
                    }
                    //if there still needs to be a right peek
                    else{
                        //move to the right scan location
                        move(destinations[rightScanLoc]);
                    }
                }
            }
            else if(rightPeeking){
                peek(false);
                updatePeekedObjects();
                //if the peek finished
                if(rightPeeked){
                    //and there is no left peeking necessary (leftScanLoc = int.MaxValue)
                    if(leftScanLoc==int.MaxValue){
                        //resolve the route to move down
                        resolveRoute();
                    }
                    //if there still needs to be a left peek
                    else{
                        //move to the left scan location
                        move(destinations[leftScanLoc]);
                    }
                }
            }
            else{
                updateSeenObjects();
            }
            
            //if the agent sees a new stair, use it
            timer+=Time.deltaTime;
            if(timer>=updateFreq){
                timer = 0;
            //if we are not peeking
            if(!leftPeeking&&!rightPeeking){
                if(movingToRouteStart){
                    if(isAtDestination()){
                        //reset moving to route start flag
                        movingToRouteStart = false;
                        //rotate the agent to the proper orientation
                        transform.rotation = Quaternion.LookRotation(currentRoute.routeDirection);
                        //update evaDir
                        evaDir = currentRoute.routeDirection;
                        //set up left and right walls
                        leftWall = currentRoute.routeLeftWall;
                        leftWallLine.SetPositions(new Vector3[]{leftWall.getStartPoint(),leftWall.getEndPoint()});
                        rightWall = currentRoute.routeRightWall;
                        rightWallLine.SetPositions(new Vector3[]{rightWall.getStartPoint(),rightWall.getEndPoint()});

                        //update moving in left/right wall dir
                        //if the left wall's direction is opposite the route direction (greater than 90 degrees)
                        if(Vector3.Angle(leftWall.getDir(),currentRoute.routeDirection)>90){
                            //we are moving not in the left wall direction
                            movingInLeftWallDir = false;
                        }
                        //if not
                        else{
                            //we are moving in the left wall direction
                            movingInLeftWallDir = true;
                        }
                        //if the right wall's direction is opposite the route direction (greater than 90 degrees)
                        if(Vector3.Angle(rightWall.getDir(),currentRoute.routeDirection)>90){
                            //we are moving not in the left wall direction
                            movingInRightWallDir = false;
                        }
                        //if not
                        else{
                            //we are moving in the left wall direction
                            movingInRightWallDir = true;
                        }

                        //set up seen Windows and exits
                        seenExits = currentRoute.routeSeenExits;
                        seenWindows = currentRoute.routeSeenWindows;

                        //start moving
                        //figure out where to move next
                        destinations[destinations.Count-1] = findNextDestination();
                        Debug.Log("Next location.... " + destinations[destinations.Count-1]);
                        move(destinations[destinations.Count-1]);
                    }
                }
                else{
                    //if a left side gap needs to be resolved
                    if(leftSensorTripped){
                        //if the left scan location is closer than the right scan location (true if there is no right scan location)
                        //Debug.Log("left scan loc = " + leftScanLoc);
                        //Debug.Log("right scan loc = " + rightScanLoc);
                        //Debug.Log("Destinations: " +destinations.Count);
                        if(leftScanLoc<rightScanLoc){
                            //if the agent is at the scan location
                            if(isAtDestination()){
                                //Debug.Log("At destination!");
                                //you may need to make the agent face the direction it was moving
                                //identify gap on left side
                                leftGapInfo = identifyGap(movingInLeftWallDir?leftWall.getEndPoint():leftWall.getStartPoint());

                                //Start peeking to the left
                                leftPeeking = true;


                                //THIS MAY NEED TO BE DONE AFTER PEEKING

                                /* //remove left scan location from the destinations list
                                destinations.Remove(destinations[leftScanLoc]);
                                leftScanLoc = int.MaxValue;
                                //if there is a rightScanLoc
                                if(rightScanLoc<int.MaxValue){
                                    rightScanLoc--;
                                }*/
                                //reset left sensor tripped
                                //leftSensorTripped = false;

                                //Figure out where to go next
                                //peek down left gap to see if there are any stimuli

                                //if the closest end point of the wall is the end of the wall
                                /* if(leftGapInfo.closestWallPointIsEnd){
                                    leftWall = leftGapInfo.wallList[leftGapInfo.closestWallIndex];
                                    leftWallLine.SetPositions(new Vector3[]{leftWall.getStartPoint(),leftWall.getEndPoint()});

                                }*/
                            }
                        }
                    }
                    //if a left side gap does not need to be resolved
                    else{
                        //sense to the left
                        senseSide(true);
                        /* //Fire rays to the left and right
                        Vector3 leftdir = DirFromAngle(-sensorAngle,false);
                        Debug.DrawRay(transform.position, leftdir*100,Color.green, 10.0f);
                        RaycastHit hit;
                        //if we hit something
                        if(Physics.Raycast(transform.position,leftdir,out hit,sensorRadius,wallMask)){
                            //Debug.Log("hit!");
                            bool onWall = leftWall.add(hit.point,this.transform);
                            //If the point hit is not on the wall
                            if(!onWall){
                                //if there is a next wall and the point is on the next wall
                                if(leftWall.getNextWall() != null&&leftWall.getNextWall().add(hit.point,this.transform)){
                                    //set the current left wall to the next wall
                                    leftWall = leftWall.getNextWall();
                                }
                                //if there is no next wall or the point was not on the next
                                else{
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
                                        //trip left sensor
                                        leftSensorTripped = true;
                                        setScanLocation(true);

                                        //identify gap
                                        //identifyGap(wallEdge,leftWall);
                                    }
                                    //if there is no gap
                                    else{

                                    }
                                }
                            }
                            leftWallLine.SetPositions(new Vector3[]{leftWall.getStartPoint(),leftWall.getEndPoint()});
                        }
                        //if we did not hit something
                        else{

                        }*/
                    }
                    if(rightSensorTripped){
                        //if the right scan location is closer than the left scan location (true if there is no left scan location)
                        if(rightScanLoc<leftScanLoc){
                            //if the agent is at the scan location
                            if(isAtDestination()){
                                //you may need to make the agent face the direction it was moving
                                //identify gap on right side
                                rightGapInfo = identifyGap(movingInRightWallDir?rightWall.getEndPoint():rightWall.getStartPoint());

                                //Start peeking to the right
                                rightPeeking = true;

                                //THIS MAY NEED TO BE DONE AFTER PEEKING

                                /*destinations.Remove(destinations[rightScanLoc]);
                                rightScanLoc = int.MaxValue;
                                //if there is a leftScanLoc
                                if(leftScanLoc<int.MaxValue){
                                    leftScanLoc--;
                                }
                                //reset right sensor tripped
                                rightSensorTripped = false;*/
                                
                            }
                        }
                    }
                    //if a right side gap does not need to be resolved
                    else{
                        //sense to the right
                        senseSide(false);
                    }
                    //if the agent has reached its destination without tripping any sensors
                    if(!leftSensorTripped&&!rightSensorTripped&&isAtDestination()){
                        //if the previous route selection spot has another route
                        if(prevSelectionSpot.nextRoute>=0){
                            Debug.Log("Attempting another route");
                            //move to the previous route's start
                            movingToRouteStart = true;
                            currentRoute = prevSelectionSpot.possibleRoutes[prevSelectionSpot.nextRoute];
                            move(currentRoute.startSpot);
                            prevSelectionSpot.nextRoute--;
                        }
                        //otherwise
                        else{
                            Debug.Log("TURNING AROUND");
                            //turn around
                            transform.rotation = Quaternion.LookRotation(-evaDir);
                            evaDir = -evaDir;
                            movingInLeftWallDir = !movingInLeftWallDir;
                            movingInRightWallDir = !movingInRightWallDir;
                            Wall temp = leftWall;
                            leftWall = rightWall;
                            rightWall = temp;

                            //figure out where to move next
                            destinations[destinations.Count-1] = findNextDestination();
                            move(destinations[destinations.Count-1]);
                        }
                    }
                }
                

            }
            }
        }
        footprintTimer+=Time.deltaTime;
        if(footprintTimer>=footprintFreq){
            footprintTimer = 0;
            GameObject footprint = Instantiate(footprintPrefab) as GameObject;
            footprint.transform.position = transform.position;
            if(objLists != null)
            {
                objLists.addFootprint(footprint);
            }
        }
        
    }
    public void addSimObjList(SimulationObjectLists _simObjLists)
    {
        objLists = _simObjLists;
    }

    public float getInitTime(){
        return initializationTime;
    }
    private GameObject getBuildingClosestExit(){
        float shortestdistance = Mathf.Infinity;
        GameObject closestExit = null;
        GameObject[] exits = GameObject.FindGameObjectsWithTag("Exit");
        foreach(GameObject exit in exits)
        {
            if(Vector3.Distance(exit.transform.position,transform.position) < shortestdistance)
            {
                shortestdistance = Vector3.Distance(exit.transform.position,transform.position);
                closestExit = exit;
            }
        }
        return closestExit;
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
        movingInLeftWallDir = true;

        points[0].y = -100;
        points[1].y = -100;
        //set up the right wall
        if(Physics.Raycast(transform.position,transform.TransformDirection(Vector3.right),out hit,sensorRadius,wallMask)){
            points[0] = hit.point;
            points[0].y = wallYVal;
        }
        if(Physics.Raycast(transform.position,transform.TransformDirection(Vector3.right+ new Vector3(0,0,0.1f)),out hit,sensorRadius,wallMask)){
            points[1] = hit.point;
            points[1].y = wallYVal;
        }
        if(points[0].y!=-100&&points[1].y!=-100){
            rightWall = new Wall(points[0],points[1],wallYVal,agent.radius*2+0.1f,wallErrorDst);
            rightWallLine.SetPositions(points);
            rightWallLine.enabled = true;
            Debug.Log("Right Wall Made!");
        }
        movingInRightWallDir = true;
    }

    private void updateSeenObjects(){
        //clear seenExits
        //seenExits.Clear();

        //if the agent sees any exits
        foreach(Transform item in fov.visibleTargets)
        {   
            //Check if it is an exit
            if(item.gameObject.layer == LayerMask.NameToLayer("FireExit")){
                //check if it is already seen
                if(!seenExits.Contains(item.gameObject)){
                    //add the exit to seenExits
                    seenExits.Add(item.gameObject);
                }
            }
            
            //check if it is an Window
            else if(item.gameObject.layer == LayerMask.NameToLayer("Window")){
                //check if it is already seen
                if(!seenWindows.Contains(item.gameObject)){
                    //add the exit to seenExits
                    seenWindows.Add(item.gameObject);
                }
            }
        }
        //if the agent sees stairs, add it to the seenStairs
    }

    /*
        Use the the appropriate sensor to sense the side in question

        @param senseLeft if we are sensing the left side
     */
    private void senseSide(bool senseLeft){
        Vector3 dir; //the direction to fire the scanner
        RaycastHit hit;
        //if we are sensing the left side
        if(senseLeft){
            //Fire ray to the left
            dir = DirFromAngle(-sensorAngle,false);
            //if we hit something
            if(Physics.Raycast(transform.position,dir,out hit,sensorRadius,wallMask)){
                //Debug.Log("hit!");
                bool onWall = leftWall.add(hit.point,this.transform);
                //If the point hit is not on the wall
                if(!onWall){
                    //if there is a next wall and the point is on the next wall
                    if(leftWall.getNextWall() != null&&leftWall.getNextWall().add(hit.point,this.transform)){
                        //set the current left wall to the next wall
                        leftWall = leftWall.getNextWall();
                    }
                    //if there is no next wall or the point was not on the next
                    else{
                        //get the closest edge of the wall to the miss point
                        Vector3 wallEdge = leftWall.closestEdgePoint(hit.point);
                        //find the edge of the wall
                        FindWallEdge(-sensorAngle,wallEdge,leftWall);
                        wallEdge = leftWall.closestEdgePoint(hit.point);//update the wallEdge
                        //check for gap
                        bool gapFound = isGap(wallEdge,leftWall);
                        Debug.Log("Left Gap found: " +gapFound);

                        //if a gap is found
                        if(gapFound){
                            //trip left sensor
                            leftSensorTripped = true;
                            setScanLocation(true);

                            //identify gap
                            //identifyGap(wallEdge,leftWall);
                        }
                        //if there is no gap
                        /*else{
                            //set up a new wall
                            leftWall = new Wall(wallEdge,hit.point,wallYVal,agent.radius*2+0.1f,wallErrorDst);
                        }*/
                    }
                }
                leftWallLine.SetPositions(new Vector3[]{leftWall.getStartPoint(),leftWall.getEndPoint()});
            }
            //if we did not hit something
            else{

            }
        }
        //if we are sensing the right side
        else{
            //Fire ray to the right
            dir = DirFromAngle(sensorAngle,false);
            
            //if we hit something
            if(Physics.Raycast(transform.position,dir,out hit,sensorRadius,wallMask)){
                //Debug.Log("hit!");
                bool onWall = rightWall.add(hit.point,this.transform);
                //If the point hit is not on the wall
                if(!onWall){
                    //if there is a next wall and the point is on the next wall
                    if(rightWall.getNextWall() != null&&rightWall.getNextWall().add(hit.point,this.transform)){
                        //set the current left wall to the next wall
                        rightWall = rightWall.getNextWall();
                    }
                    //if there is no next wall or the point was not on the next
                    else{
                        //get the closest edge of the wall to the miss point
                        Vector3 wallEdge = rightWall.closestEdgePoint(hit.point);
                        //find the edge of the wall
                        FindWallEdge(sensorAngle,wallEdge,leftWall);
                        wallEdge = rightWall.closestEdgePoint(hit.point);//update the wallEdge
                        //check for gap
                        bool gapFound = isGap(wallEdge,rightWall);
                        Debug.Log("Right Gap found: " +gapFound);

                        //if a gap is found
                        if(gapFound){
                            //trip left sensor
                            rightSensorTripped = true;
                            setScanLocation(false);

                            //identify gap
                            //identifyGap(wallEdge,leftWall);
                        }
                        //if there is no gap
                        /*else{
                            //set up a new wall
                            rightWall = new Wall(wallEdge,hit.point,wallYVal,agent.radius*2+0.1f,wallErrorDst);
                        }*/
                    }
                }
                rightWallLine.SetPositions(new Vector3[]{rightWall.getStartPoint(),rightWall.getEndPoint()});
            }
            //if we did not hit something
            else{

            }
        }
        //Debug.DrawRay(transform.position, dir*100,Color.green, updateFreq*5);
    }

    /*
        Move the evacuee to a given location

        @param location the location the agent is to move to
     */
    private void move(Vector3 location){
        //agent.Warp(location);
        //Debug.Log("MOVING");
        agent.SetDestination(location);
    }

    private Vector3 findNextDestination(){
        Vector3 result;
        //if there are any seen exits
        if(seenExits.Count>0){
            //the next destination is the closest exit
            Debug.Log("Destination is Exit");
            result = getClosestObject(seenExits,transform.position).transform.position;
            
        }
        //if there are any seen Windows
        else if(seenWindows.Count>0){
            //the next destination is the closest Window
            Debug.Log("Destination is Window");
            result = getClosestObject(seenWindows,transform.position).transform.position;
            
        }
        //otherwise
        else{
            //fire a ray forward
            RaycastHit hit;
            //Debug.DrawRay(transform.position, DirFromAngle(angle,true)*100,Color.black, 10.0f);
            //if we hit something
            if(Physics.Raycast(transform.position,transform.forward,out hit,sensorRadius,wallMask)){
                //the next destination is the hit point (minus the )
                Debug.Log("Destination is Forward");
                result = hit.point+(Vector3.Normalize(transform.position-hit.point))*2f;
            }
            else{
                //WILL NEED TESTING
                //the next destination is forward from this agent
                result = transform.position+transform.forward*sensorRadius;

            }
        }
        result.y = 0;
        Debug.Log("Next Destination: " + result);
        return result;
    }

    /*
        Is the agent at its desination
     */
    private bool isAtDestination(){
        // Check if we've reached the destination
        //if the path is not pending
        if (!agent.pathPending)
        {
            //if the remaining distance is less than the stopping distance
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                //if there is a path or the velocity of the agent is 0
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {   
                    //Debug.Log("Reached the spot!");
                    return true;
                }
               // else{Debug.Log("Has a path or has a velocity");}
            }
            //else{Debug.Log("Outside stopping distance");}
        }
        //else{Debug.Log("Path Pending");}
        return false;
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
            if(Physics.Raycast(transform.position,DirFromAngle(angle,false),out hit,sensorRadius,wallMask)){
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

        //get the forward wall data
        RaycastHit fwdhit;
        //
        Physics.Raycast(transform.position,transform.forward,out fwdhit,sensorRadius,wallMask);
        //Debug.DrawRay(transform.position, transform.forward*sensorRadius,Color.black, 10.0f);


        //binary search between min and max angles
        for(int i = 0; i <gapResolveIterations; i++){
            float angle = (minAngle + maxAngle)/2;

            //fire at the angle
            RaycastHit hit;
            //if we hit something
            if(Physics.Raycast(transform.position,DirFromAngle(angle,false),out hit,sensorRadius,wallMask)){
                //if(false){}
                //if the hit point is closer than the wall edge to the agent, the wall juts out towards the agent, there is no gap
                /* Debug.Log("Hit Square " + Mathf.Pow(Vector3.Distance(hit.point,transform.position),2));
                Debug.Log("Fwd Square " + Mathf.Pow(Vector3.Distance(fwdhit.point,transform.position),2));
                Debug.Log("Hit-Fwd Square " + (Mathf.Pow(Vector3.Distance(hit.point,transform.position),2)-Mathf.Pow(Vector3.Distance(fwdhit.point,transform.position),2)));
                Debug.Log("Dst Square " +Mathf.Pow(Vector3.Distance(hit.point,fwdhit.point),2));*/
                if(Mathf.Abs(Mathf.Pow(Vector3.Distance(hit.point,transform.position),2)-Mathf.Pow(Vector3.Distance(fwdhit.point,transform.position),2)-Mathf.Pow(Vector3.Distance(hit.point,fwdhit.point),2))<30f){
                    Debug.Log("There is a forward wall here!");
                    return false;
                }
                else{
                    bool onWall = (theWall.add(hit.point,this.transform)&&theWall.getLineRegion(hit.point)!=0);
                    //If the point hit is not on the wall
                    if(!onWall){
                        //update max angle
                        //Debug.DrawRay(transform.position, DirFromAngle(angle,false)*100,Color.black, 10.0f);
                        //Debug.Log("Missed on step "+i);
                        maxAngle = angle;
                    }
                    //if we hit something along the wall
                    else{
                        //Debug.DrawRay(transform.position, DirFromAngle(angle,false)*100,Color.yellow, 10.0f);
                        //Debug.Log("Hit on step " +i);
                        //Debug.Log(Vector3.Distance(hit.point,wallEdge));
                        //if(Vector3.Distance(hit.point,wallEdge)>0.5f){
                            return false;
                        //}
                    }
                }
                
            }
            //if we did not hit anything
            else{
                //Debug.DrawRay(transform.position, DirFromAngle(angle,false)*100,Color.black, 10.0f);
                //Debug.Log("Missed on step "+i);
                maxAngle = angle;
            }
        }
        //if we fired the gapResolveIterations amount of times and did not hit anything, then there is a gap!
        return true;
    }

    private void setScanLocation(bool isLeftScan){
        //figure out where the agent should move to to scan the wall
        Vector3 spotPastWall;
        //if we want to scan the left wall
        if(isLeftScan){
            //if we are working in the direction of the wall
            if(movingInLeftWallDir){
                //the spot is 1 radius of the agent past the wall
                spotPastWall = leftWall.nextPointAlongWall(agent.radius*5,false);
            }
            //if  we are working on the opposite direction of the wall
            else{
                //the spot is 1 radius of the agent past the wall
                spotPastWall = leftWall.nextPointAlongWall(agent.radius*5,true);
            }
        }
        else{
            //if we are working in the direction of the wall
            if(movingInRightWallDir){
                //the spot is 1 radius of the agent past the wall
                spotPastWall = rightWall.nextPointAlongWall(agent.radius*5,false);
            }
            //if  we are working on the opposite direction of the wall
            else{
                //the spot is 1 radius of the agent past the wall
                spotPastWall = rightWall.nextPointAlongWall(agent.radius*5,true);
            }
        }
        //project the point onto the direction that the agent was moving
        spotPastWall = Vector3.Project(spotPastWall-transform.position,Vector3.Normalize(transform.forward))+transform.position;

        //if we are scanning the left wall
        if(isLeftScan){
            //if the right sensor has been tripped, there is a right scan spot
            if(rightSensorTripped){
                //if the left scan spot is closer than the right scan spot
                if(Vector3.Distance(spotPastWall,transform.position)<Vector3.Distance(destinations[0],transform.position)){
                    //insert the spot as the next destination
                    destinations.Insert(0,spotPastWall);
                    //keep track of the left scan location in the list
                    leftScanLoc = 0;
                    //update the right scan location in the list
                    rightScanLoc = 1;
                    //start moving towards the new closest scan location
                    move(destinations[0]);
                }
                //if the left scan spot is farther than the right scan spot
                else{
                    //insert the spot as the second destination
                    destinations.Insert(1,spotPastWall);
                    //keep track of the left scan location in the list
                    leftScanLoc = 1;
                }
            }
            //if the right sensor has not been tripped, there is only the target destination
            else{
                //insert the spot as the next destination
                destinations.Insert(0,spotPastWall);
                //keep track of the left scan location in the list
                leftScanLoc = 0;
                //start moving towards the new closest scan location
                move(destinations[0]);
            }
        }
        else{
            //if the left sensor has been tripped, there is a left scan spot
            if(leftSensorTripped){
                //if the right scan spot is closer than the left scan spot
                if(Vector3.Distance(spotPastWall,transform.position)<Vector3.Distance(destinations[0],transform.position)){
                    //insert the spot as the next destination
                    destinations.Insert(0,spotPastWall);
                    //update the left scan location in the list
                    leftScanLoc = 1;
                    //keep track of the right scan location in the list
                    rightScanLoc = 0;
                    //start moving towards the new closest scan location
                    move(destinations[0]);
                }
                //if the right scan spot is farther than the leftt scan spot
                else{
                    //insert the spot as the second destination
                    destinations.Insert(1,spotPastWall);
                    //keep track of the right scan location in the list
                    rightScanLoc = 1;
                }
            }
            //if the left sensor has not been tripped, there is only the target destination
            else{
                //insert the spot as the next destination
                destinations.Insert(0,spotPastWall);
                //keep track of the right scan location in the list
                rightScanLoc = 0;
                //start moving towards the new closest scan location
                move(destinations[0]);
            }
        }
    }

    private GapInfo identifyGap(Vector3 wallEdge){
        Debug.Log("Identifying gap");
        /* Vector3 spotPastWall;
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
        move(spotPastWall);*/

        
        //fire rays around, starting from the last spot of the wall and ending straight ahead
        Vector3 lastSpot = wallEdge;
        lastSpot.y=0;
        //Debug.Log("End point of wall: " + lastSpot);
        float angle = Vector3.SignedAngle(transform.forward,lastSpot-transform.position,transform.up);

        //A list of walls to hold all the walls that will be created
        List<Wall> wallList = new List<Wall>();
        RaycastHit hit; //the spot the ray hits
        Vector3 closestWallPoint = new Vector3(); //the wall point closest to the wallEdge
        int closestWallIndex = -1; // the index of the closest wall, initially set to a nonsense number
        float closestWallDst = 99999999;
        int currentWall = 0; //the index of the current wall
        Vector3[] wallPoints = new Vector3[2]; //two points that will construct a new wall
        int pointCount = 0; //the number of points added that will construct a new wall

        //float toStart;
        //float toEnd;

        //if the angle is < 0, the area in question is on the left
        if(angle<0){
            //round the angle up
            angle = Mathf.Ceil(angle);
            //first pass construction of walls
            
            while(angle<0){
                //Debug.Log("angle: " + angle);
                //get the next spot to shoot at
                Vector3 shotSpot = DirFromAngle(angle,false);
                //Debug.Log("Spot: "+shotSpot*19);

                //fire a ray at the spot
                if(Physics.Raycast(transform.position,DirFromAngle(angle,false),out hit,sensorRadius,wallMask)){
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
                //Debug.DrawRay(transform.position, shotSpot*sensorRadius,Color.cyan, 10.0f);
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
        else{
            //round the angle down
            angle = Mathf.Floor(angle);
            //first pass construction of walls
            
            while(angle>0){
                //Debug.Log("angle: " + angle);
                //get the next spot to shoot at
                Vector3 shotSpot = DirFromAngle(angle,false);
                //Debug.Log("Spot: "+shotSpot*19);

                //fire a ray at the spot
                if(Physics.Raycast(transform.position,DirFromAngle(angle,false),out hit,sensorRadius,wallMask)){
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
                            currentWall++;
                        }
                    }
                    

                }
                //Debug.DrawRay(transform.position, shotSpot*sensorRadius,Color.cyan, 10.0f);
                angle-=gapScanDegrees;
            }
        }

        //display the gap scan information
        //Debug.Log("Found " + currentWall + " walls!");
        //Debug.Log("Closest wall: " + closestWallIndex);
        int wallNum = 0;
        foreach(Wall curWall in wallList){
            GameObject newWall = Instantiate(lineHolderPrefab) as GameObject;
            newWall.transform.parent = this.transform;
            LineRenderer line = newWall.GetComponent<LineRenderer>();
            line.enabled = true;
            line.SetPositions(new Vector3[]{curWall.getStartPoint(),curWall.getEndPoint()});
            float dst = Vector3.Distance(curWall.closestEdgePoint(wallEdge),wallEdge);
            //Debug.Log("Wall " + wallNum+ " Distance: " + dst);
            if(closestWallIndex>=0&& /* curWall.getStartPoint() == wallList[numClosestWall].getStartPoint()&& curWall.getEndPoint() == wallList[numClosestWall].getEndPoint()*/curWall.Equals(wallList[closestWallIndex])){
                //Debug.Log("Closest Wall Found: Wall "+wallNum);
                //Material theMat = Resources.Load<Material>("Materials/ClosestWallMaterial"/* , typeof(Material)*/) as Material;
                line.material = Resources.Load<Material>("Materials/ClosestWallMaterial"/* , typeof(Material)*/) as Material;;
            }
            wallNum++;
        }
        
        GapInfo result = new GapInfo(wallList,closestWallIndex,closestWallPoint);
        return result;
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
        //Debug.Log("For " + wallIndex + "th wall:");
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
                /* Debug.Log("Top route");
                Debug.Log("closestWallPoint: " + closestWallPoint);
                Debug.Log("minDst: "+minDst);
                Debug.Log("toStart: " +toStart);
                Debug.Log("toEnd: "+toEnd);*/

                //if the start point is farther than 2 radius of the agent (the agent can fit through gap), and the start point is closer
                //to the wall edge than the current closest wall
                if(toStart<minDst&&toStart>=gap){
                    //attempt to add toStart
                    if(!checkForWall(point,wallList[wallIndex].getStartPoint())){
                        closestWallIndex = wallIndex; // the index of the closest wall, initially set to a nonsense number
                        minDst = toStart;
                        closestWallPoint = wallList[closestWallIndex].getStartPoint();
                    }
                    //else{Debug.Log("Hit a wall between start and edge");}
                }
                //if the start point failed, but the end point has a gap, and the end point is closer to the wall edge
                if(toEnd<minDst&&toEnd>=gap){
                    //attempt to add toEnd
                    if(!checkForWall(point,wallList[wallIndex].getEndPoint())){
                        closestWallIndex = wallIndex; // the index of the closest wall, initially set to a nonsense number
                        minDst = toEnd;
                        closestWallPoint = wallList[closestWallIndex].getEndPoint();
                    }
                    //else{Debug.Log("Hit a wall between end and edge");}
                }
            }
            //if there has not been a closest wall discovered
            else{
                /* Debug.Log("Bottom route");
                Debug.Log("closestWallPoint: " + closestWallPoint);
                Debug.Log("minDst: "+minDst);
                Debug.Log("toStart: " +toStart);
                Debug.Log("toEnd: "+toEnd);*/
                //if there is a gap between the start and the wall
                if(toStart>=gap){
                    //attempt to add toStart
                    if(!checkForWall(point,wallList[wallIndex].getStartPoint())){
                        closestWallIndex = wallIndex; // the index of the closest wall, initially set to a nonsense number
                        minDst = toStart;
                        closestWallPoint = wallList[closestWallIndex].getStartPoint();
                    }
                    //else{Debug.Log("Hit a wall between start and edge");}
                }
                if(toEnd>=gap&&toEnd<minDst){
                    //attempt to add toEnd
                    if(!checkForWall(point,wallList[wallIndex].getEndPoint())){
                        closestWallIndex = wallIndex; // the index of the closest wall, initially set to a nonsense number
                        minDst = toEnd;
                        closestWallPoint = wallList[closestWallIndex].getEndPoint();
                    }
                    //else{Debug.Log("Hit a wall between end and edge");}
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
        //Debug.Log("Returning: " +closestWallPoint);
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

    private void peek(bool leftPeek){
        //Determines if we are rotating to the left or right
        int rotationAxis = leftPeek?-1:1;
        //if we have not hit the maximum rotation angle
        if(!hasPeeked){
            //Debug.Log(Time.deltaTime*rotationSpeed);
            //rotate towards the left/right side
            transform.Rotate(rotationAxis*Vector3.up*Time.deltaTime*peekRotationSpeed);
            peekRotation+=Time.deltaTime*peekRotationSpeed;

            //if we have hit the maximum rotation angle
            if(peekRotation>=90){
                //correct any overturn
                float correction = 90-peekRotation;
                transform.Rotate(-Vector3.up*correction);
                peekRotation+=correction;
                //Debug.Log(peekRotation);
                //leftPeek = false;

                //mark that the max rotation angle was hit
                hasPeeked = true;
            }
        }
        //if we have hit the maximum rotation angle
        else{
            //rotate back to the original position
            transform.Rotate(-rotationAxis*Vector3.up*Time.deltaTime*peekRotationSpeed);
            peekRotation-=Time.deltaTime*peekRotationSpeed;
            //if we have overrotated
            if(peekRotation<0){
                //correct the overrotation
                float correction = 0-peekRotation;
                transform.Rotate(Vector3.up*correction);
                //reset the rotation
                peekRotation = 0;
                //Debug.Log(peekRotation);

                //if we were peeking to the left
                if(leftPeeking){
                    //reset left peeking flag
                    leftPeeking = false;
                    leftPeeked = true;

                    //remove left scan location from the destinations list
                    destinations.Remove(destinations[leftScanLoc]);
                    leftScanLoc = int.MaxValue;
                    //if there is a rightScanLoc
                    if(rightScanLoc<int.MaxValue){
                        //shuffle right scan loc
                        rightScanLoc--;
                    }
                    //reset left sensor tripped
                    //leftSensorTripped = false;
                }
                //if we were peeking to the right
                else if(rightPeeking){
                    //reset right peeking flag
                    rightPeeking = false;
                    rightPeeked = true;
                    destinations.Remove(destinations[rightScanLoc]);
                    rightScanLoc = int.MaxValue;
                    //if there is a leftScanLoc
                    if(leftScanLoc<int.MaxValue){
                        //shuffle left scan loc
                        leftScanLoc--;
                    }
                        //reset right sensor tripped
                        //rightSensorTripped = false;
                }
                //reset maximum rotation angle hit flag
                hasPeeked = false;

                //Say what you saw
                foreach(GameObject item in leftPeek?leftGapInfo.gapSeenExits:rightGapInfo.gapSeenExits){
                    Debug.Log("Saw "+item.name+" exit!");
                }
                foreach(GameObject item in leftPeek?leftGapInfo.gapSeenWindows:rightGapInfo.gapSeenWindows){
                    Debug.Log("Saw "+item.name+" window!");
                }
            }
        }
    }


    private void updatePeekedObjects(){
        //if the object has been seen before the peek, it is not included
        //if we are peeking to the left, ensure that leftGapInfo
        //if(leftPeeking?leftGapInfo!=null:rightGapInfo!=null){
            List<GameObject> gapExitsList = leftPeeking?leftGapInfo.gapSeenExits:rightGapInfo.gapSeenExits;
            List<GameObject> gapWindowsList = leftPeeking?leftGapInfo.gapSeenWindows:rightGapInfo.gapSeenWindows;
        
            //if the agent sees any exits
            foreach(Transform item in fov.visibleTargets)
            {   
                //Check if it is an exit
                if(item.gameObject.layer == LayerMask.NameToLayer("FireExit")){
                    //check if it is already seen
                    if(!seenExits.Contains(item.gameObject)&&!gapExitsList.Contains(item.gameObject)){
                        //add the exit to seenExits
                        gapExitsList.Add(item.gameObject);
                    }
                }
                //check if it is an window
                else if(item.gameObject.layer == LayerMask.NameToLayer("Window")){
                    //check if it is already seen
                    if(!seenWindows.Contains(item.gameObject)&&!gapWindowsList.Contains(item.gameObject)){
                        //add the exit to seenExits
                        gapWindowsList.Add(item.gameObject);
                    }
                }
            }
        //}
        
        //if the agent sees stairs, add it to the seenStairs
    }
    private void resolveRoute(){
        //the original direction of the agent is given by the destinations[destinations.Count-1]
        //Vector3 originalDir = destinations[destinations.Count-1] - transform.position;
        Vector3 startSpot;
        Vector3 leftDstCalcSpot = Vector3.positiveInfinity;
        Vector3 rightDstCalcSpot = Vector3.positiveInfinity;
        //Route(Wall _leftWall, Wall _rightWall, Vector3 _startSpot, Vector3 _direction, List<GameObject> _routeSeenExits, List<GameObject> _routeSeenWindows, bool _chosen, int _priority)
        Route leftRoute = new Route(new Wall(Vector3.zero, Vector3.zero, 0f, 0f, 0f), new Wall(Vector3.zero, Vector3.zero, 0f, 0f, 0f), Vector3.positiveInfinity, Vector3.zero, new List<GameObject>(), new List<GameObject>(), false, int.MinValue);
        Route rightRoute = new Route(new Wall(Vector3.zero, Vector3.zero, 0f, 0f, 0f), new Wall(Vector3.zero, Vector3.zero, 0f, 0f, 0f), Vector3.positiveInfinity, Vector3.zero, new List<GameObject>(), new List<GameObject>(), false, int.MinValue);
        Route fwdRoute;
        Wall newLeftWall = leftWall;
        Wall newRightWall = rightWall;
        int routesCreated = 0;
        Quaternion.LookRotation(evaDir);

        //if we peeked to the left
        if(leftPeeked){
            //create a left route
            //get the middle between the left gap's left and right walls
            startSpot = ( (movingInLeftWallDir?leftWall.getEndPoint() :leftWall.getStartPoint())+leftGapInfo.closestWallPoint)/2;
            //Debug.DrawRay(Vector3.zero,startSpot,Color.red,10.0f);
            //put it into halfway of original hallway
            //that is the distance calculation spot
            leftDstCalcSpot = Vector3.Project(startSpot-transform.position,Vector3.Normalize(evaDir))+transform.position;
            //Debug.DrawRay(Vector3.zero,leftDstCalcSpot,Color.blue,10.0f);

            //create a left route
            //what is the right wall? depends on if the closest point is the start or end
            //if the closest point is the end, the right wall is the closestWallIndex
            //if the closest point is the start, the right wall is the previous wall in the list
            
            //Route(Wall _leftWall, Wall _rightWall, Vector3 _startSpot, Vector3 _direction, List<GameObject> _routeSeenExits, List<GameObject> _routeSeenWindows, bool _chosen, int _priority)
            leftRoute = new Route(leftGapInfo.wallList[0], leftGapInfo.closestWallPointIsEnd?leftGapInfo.wallList[leftGapInfo.closestWallIndex]:leftGapInfo.wallList[leftGapInfo.closestWallIndex-1], startSpot, -transform.right, leftGapInfo.gapSeenExits, leftGapInfo.gapSeenWindows, false, 0);
            
            //set up the new left wall for moving forwards
            //if the closest wall point is the end point of the wall
            if(leftGapInfo.closestWallPointIsEnd){
                //if there is a wall after the closest wall
                if(leftGapInfo.closestWallIndex+1<leftGapInfo.wallList.Count){
                    //make that the new left wall
                    newLeftWall = leftGapInfo.wallList[leftGapInfo.closestWallIndex+1];
                }
                //if there are no walls after the closest wall
                else{
                    //make the new left wall the closest wall
                    newLeftWall = leftGapInfo.wallList[leftGapInfo.closestWallIndex];
                }
            }
            //if the closest wall point is the start point of the wall
            else{
                //make the new left wall the closest wall
                 newLeftWall = leftGapInfo.wallList[leftGapInfo.closestWallIndex];
            }
        }
        //if we peeked to the right
        if(rightPeeked){
            //get the middle between the right gap's left and right walls
            startSpot = ( (movingInRightWallDir?rightWall.getEndPoint() :rightWall.getStartPoint())+rightGapInfo.closestWallPoint)/2;
            //Debug.DrawRay(Vector3.zero,startSpot,Color.red,10.0f);
            //put it into halfway of original hallway
            //that is the distance calculation spot
            rightDstCalcSpot = Vector3.Project(startSpot-transform.position,Vector3.Normalize(evaDir))+transform.position;
            //Debug.DrawRay(Vector3.zero,rightDstCalcSpot,Color.blue,10.0f);
            //create a right route
            //what is the left wall? depends on if the closest point is the start or end
            //if the closest point is the end, the left wall is the closestWallIndex
            //if the closest point is the start, the left wall is the previous wall in the list
            rightRoute = new Route(rightGapInfo.closestWallPointIsEnd?rightGapInfo.wallList[rightGapInfo.closestWallIndex]:rightGapInfo.wallList[rightGapInfo.closestWallIndex-1], rightGapInfo.wallList[0], startSpot, transform.right, rightGapInfo.gapSeenExits, rightGapInfo.gapSeenWindows, false, 0);
            
            //set up the new right wall for moving forwards
            //if the closest wall point is the end point of the wall
            if(rightGapInfo.closestWallPointIsEnd){
                //if there is a wall after the closest wall
                if(rightGapInfo.closestWallIndex+1<rightGapInfo.wallList.Count){
                    //make that the new right wall
                    newRightWall = rightGapInfo.wallList[rightGapInfo.closestWallIndex+1];
                }
                //if there are no walls after the closest wall
                else{
                    //make the new right wall the closest wall
                    newRightWall = rightGapInfo.wallList[rightGapInfo.closestWallIndex];
                }
            }
            //if the closest wall point is the start point of the wall
            else{
                //make the new right wall the closest wall
                 newRightWall = rightGapInfo.wallList[rightGapInfo.closestWallIndex];
            }
        }

        //if there was a left and right peak
        if(leftPeeked&&rightPeeked){
            //compare the two routes
            //if the leftRoute has higher priority (compareTo<0) than right route
            if(leftRoute.compareTo(rightRoute,leftDstCalcSpot,rightDstCalcSpot,evaDir)<0){
                //increment the left route's priority
                leftRoute.priority++;
            }
            //if the rightRoute has higher priority than the left route
            else{
                //increment the right route's priority
                rightRoute.priority++;
            }
        }
        //create a forward route
        //Route(Wall _leftWall, Wall _rightWall, Vector3 _startSpot, Vector3 _direction, List<GameObject> _routeSeenExits, List<GameObject> _routeSeenWindows, bool _chosen, int _priority)
        Vector3 fartherWall = leftPeeked?rightPeeked?(Vector3.Distance(leftGapInfo.closestWallPoint,transform.position)<Vector3.Distance(rightGapInfo.closestWallPoint,transform.position))?rightGapInfo.closestWallPoint:leftGapInfo.closestWallPoint:leftGapInfo.closestWallPoint:rightGapInfo.closestWallPoint;

        startSpot = Vector3.Project(fartherWall-transform.position,Vector3.Normalize(evaDir))+transform.position;
        //(Vector3.Distance(leftDstCalcSpot,transform.position)<Vector3.Distance(rightDstCalcSpot,transform.position))?leftDstCalcSpot:rightDstCalcSpot
        fwdRoute = new Route(newLeftWall, newRightWall, startSpot, evaDir, seenExits, seenWindows, false, 0);

        //if we have peeked left
        if(leftPeeked){
            //compare the left route and the forward route
            //if the leftRoute has higher priority (compareTo<0) than fwd route
            if(leftRoute.compareTo(fwdRoute,leftDstCalcSpot,leftDstCalcSpot,evaDir)<0){
                //increment the left route's priority
                leftRoute.priority++;
            }
            //if the fwdRoute has higher priority than the left route
            else{
                //increment the right route's priority
                fwdRoute.priority++;
            }
        }
        //if we have peeked right
        if(rightPeeked){
            //compare the right route and the forward route
            //if the fwdRoute has higher priority (compareTo<0) than right route
            if(fwdRoute.compareTo(rightRoute,rightDstCalcSpot,rightDstCalcSpot,evaDir)<0){
                //increment the fwd route's priority
                fwdRoute.priority++;
            }
            //if the rightRoute has higher priority than the fwd route
            else{
                //increment the right route's priority
                rightRoute.priority++;
            }
        }
        
        List<Route> routes = new List<Route>();
        routes.Add(fwdRoute);
        //if we peeked left
        if(leftPeeked){
            //if the left route has higher priority than the forward route
            if(fwdRoute.priority<leftRoute.priority){
                //put the left route at the end of the list
                routes.Insert(1,leftRoute);
            }
            //if the forward route has higher priority than the left route
            else{
                //put the left route at the start of the list
                routes.Insert(0,leftRoute);
            }
            
            //if we also peeked right
            if(rightPeeked){
                routes.Insert(rightRoute.priority,rightRoute);
            }
        }
        //if we did not peek left (we must have peeked right)
        else{
            //if the right route has higher priority than the forward route
            if(fwdRoute.priority<rightRoute.priority){
                //put the right route at the end of the list
                routes.Insert(1,rightRoute);
            }
            //if the forward route has higher priority than the right route
            else{
                //put the right route at the start of the list
                routes.Insert(0,rightRoute);
            }
        }

        //move down the path that has the highest priority
        routes[routes.Count-1].setChosen(true);
        currentRoute = routes[routes.Count-1];
        move(routes[routes.Count-1].startSpot);
        movingToRouteStart = true;
        
        foreach(Route r in routes){
            Debug.Log("Route: " +r.ToString() + ", Priority " + r.priority);
        }

        //construct a RouteSelectionSpot
        RouteSelectSpot selectSpot = new RouteSelectSpot(routes,(leftPeeked&&rightPeeked)?1:0);
        prevSelectionSpot = selectSpot;

        //reset peeked flags
        leftPeeked = false;
        rightPeeked = false;
        //reset sensor tripped flags
        leftSensorTripped = false;
        rightSensorTripped = false;
    }
    public Vector3 DirFromAngle(float angleInDegrees,bool angleIsGlobal){
        //if the angle is not global, add the y transform
        if(!angleIsGlobal){
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),0,Mathf.Cos(angleInDegrees*Mathf.Deg2Rad));
    }

    public GameObject getClosestObject(List<GameObject> listOfObj, Vector3 point){
        GameObject closest = null;
        float minDst = float.MaxValue; //the distance of the closest item to the point
        foreach(GameObject item in listOfObj){
            float iDst = Vector3.Distance(item.transform.position,point);
            if(iDst<minDst){
                minDst = iDst;
                closest = item;
            }
        }
        return closest;
    }

    private struct GapInfo{
        //A list of walls to hold all the walls that will be created
        public List<Wall> wallList;
        public int closestWallIndex; // the index of the closest wall
        public Vector3 closestWallPoint; //the wall point closest to the previous wall
        public bool closestWallPointIsEnd; //if the closestWallPoint is the endpoint of the wall
        public List<GameObject> gapSeenExits;
        public List<GameObject> gapSeenWindows;


        public GapInfo(List<Wall> _wallList,int _closestWallIndex, Vector3 _closestWallPoint ){
            wallList = _wallList;
            closestWallIndex = _closestWallIndex;
            closestWallPoint = _closestWallPoint;
            closestWallPointIsEnd = (Vector3.Distance(wallList[closestWallIndex].getEndPoint(),closestWallPoint)<0.1f);
            gapSeenExits = new List<GameObject>();
            gapSeenWindows = new List<GameObject>();
        }
        public GapInfo(List<Wall> _wallList, int _closestWallIndex, bool _closestWallPointIsEnd){
            wallList = _wallList;
            closestWallIndex = _closestWallIndex;
            closestWallPointIsEnd = _closestWallPointIsEnd;
            closestWallPoint = closestWallPointIsEnd?wallList[closestWallIndex].getEndPoint():wallList[closestWallIndex].getStartPoint();
            gapSeenExits = new List<GameObject>();
            gapSeenWindows = new List<GameObject>();
        }
    }

    private struct Route{
        public Wall routeLeftWall;
        public Wall routeRightWall;
        public Vector3 startSpot; //Set to positive infinity to represent a null route
        public Vector3 routeDirection;
        public List<GameObject> routeSeenExits;
        public List<GameObject> routeSeenWindows;
        public bool chosen;
        public int priority;
        
        public Route(Wall _leftWall, Wall _rightWall, Vector3 _startSpot, Vector3 _direction, List<GameObject> _routeSeenExits, List<GameObject> _routeSeenWindows, bool _chosen, int _priority){
            routeLeftWall = _leftWall;
            routeRightWall = _rightWall;
            startSpot = _startSpot;
            routeDirection = _direction;
            routeSeenExits = _routeSeenExits;
            routeSeenWindows = _routeSeenWindows;
            chosen = _chosen;
            priority = _priority;
        }
        public GameObject getClosestSeenExit(Vector3 point){
            return getClosestObject(routeSeenExits,point);
        }
        public GameObject getClosestSeenWindow(Vector3 point){
            return getClosestObject(routeSeenWindows,point);
        }
        public float closestSeenExitDst(Vector3 point){
            return Vector3.Distance(point,getClosestSeenExit(point).transform.position);
        }
        public float closestSeenWindowDst(Vector3 point){
            return Vector3.Distance(point,getClosestSeenWindow(point).transform.position);
        }
        private GameObject getClosestObject(List<GameObject> listOfObj, Vector3 point){
            GameObject closest = null;
            float minDst = float.MaxValue; //the distance of the closest item to the point
            foreach(GameObject item in listOfObj){
                float iDst = Vector3.Distance(item.transform.position,point);
                if(iDst<minDst){
                    minDst = iDst;
                    closest = item;
                }
            }
            return closest;
        }
        public float getRouteWidth(){
            
            Vector3 leftClosest = routeLeftWall.closestEdgePoint(routeRightWall.getMidPoint());
            Vector3 rightClosest = routeRightWall.closestEdgePoint(routeLeftWall.getMidPoint());
            float lToR = Vector3.Distance(routeRightWall.projectOntoWall(leftClosest),leftClosest);
            float rToL = Vector3.Distance(routeLeftWall.projectOntoWall(rightClosest),rightClosest);
            if(lToR<rToL){
                return lToR;
            }
            else{
                return rToL;
            }
        }

        /*
            Comparison significant to the 0.001 spot
            negative value means this wall comes first (Higher priority), positive value means this wall comes second (lower priority)

         */
        public int compareTo(Route other,Vector3 thisDstCalcSpot, Vector3 otherDstCalcSpot, Vector3 agentDir){
            Debug.Log("COMPARING");
            //compare exits
            //if this route has exits
            if(routeSeenExits.Count>0){
                //if the other route has exits
                if(other.routeSeenExits.Count>0){
                    //compare the distances of the exits
                    float dstDiff = (closestSeenExitDst(thisDstCalcSpot)-other.closestSeenExitDst(otherDstCalcSpot))*1000;
                    //if the difference is significant
                    if(Mathf.Abs(dstDiff)>1){
                        Debug.Log("Exit " + getClosestSeenExit(thisDstCalcSpot).name + " or Exit " + other.getClosestSeenExit(otherDstCalcSpot).name +" is closer by "+dstDiff);
                        return (int)(dstDiff);
                    }
                    //((other.closestSeenExitDst(otherDstCalcSpot)-closestSeenExitDst(thisDstCalcSpot))*1000);
                }
                //if this route has an exit and the other does not, this route comes first
                else{
                    Debug.Log("Only Exit " + getClosestSeenExit(thisDstCalcSpot).name);
                    return -1;
                }
            }
            //if this route does not have exits
            else{
                //if the other route has exits
                if(other.routeSeenExits.Count>0){
                    Debug.Log("Only Exit " + other.getClosestSeenExit(otherDstCalcSpot).name);
                    //this route comes second
                    return 1;
                }
            }

            //compare Windows
            //if this route has Windows
            if(routeSeenWindows.Count>0){
                //if the other route has exits
                if(other.routeSeenWindows.Count>0){
                    //compare the distances of the Windows
                    float dstDiff = (closestSeenWindowDst(thisDstCalcSpot)-other.closestSeenWindowDst(otherDstCalcSpot))*1000;
                    //if the difference is significant
                    if(Mathf.Abs(dstDiff)>1){
                        Debug.Log("Window " + getClosestSeenWindow(thisDstCalcSpot).name + " or Window " + other.getClosestSeenWindow(otherDstCalcSpot).name +" is closer by "+dstDiff);
                        return (int)(dstDiff);
                    }
                }
                //if this route has an Window and the other does not, this route comes first
                else{
                    Debug.Log("Only Window " + getClosestSeenWindow(thisDstCalcSpot).name);
                    return -1;
                }
            }
            //if this route does not have Windows
            else{
                //if the other route has Windows
                if(other.routeSeenWindows.Count>0){
                    Debug.Log("Only Window " + other.getClosestSeenWindow(otherDstCalcSpot).name);
                    //this route comes second
                    return 1;
                }
            }

            //Compare widths of the routes
            //if the widths difference is significant
            if(Mathf.Abs(getRouteWidth()-other.getRouteWidth())>1){
                //if this route is smaller than the other
                if((getRouteWidth()-other.getRouteWidth())<0){
                    Debug.Log("This width, " + getRouteWidth() + " is smaller than that width, " + other.getRouteWidth());
                    //this route comes later
                    return 1;
                }
                //else if this route is larger than the other
                else if((getRouteWidth()-other.getRouteWidth())>0){
                    Debug.Log("Other width, " + other.getRouteWidth() + " is smaller than this width, " + getRouteWidth());
                    return -1;
                }
            }
            

            //Compare directions
            //ensure all directions have y value of 0
            agentDir.y = 0;
            other.routeDirection.y = 0;
            routeDirection.y = 0;
            //if this route is closer to the direction of the agent
            if(Vector3.Angle(agentDir,routeDirection)<Vector3.Angle(agentDir,other.routeDirection)){
                Debug.Log("This direction is more straight");
                return -1;
            }
            else{
                Debug.Log("Other direction is more straight");
                return 1;
            }
        }
        public string ToString(){
            return "Route start " + startSpot + " Dir " + routeDirection;
        }
        public void setChosen(bool _chosen){
            chosen = _chosen;
        }
    }

    /*
        A group of routes
     */
    private struct RouteSelectSpot{
        public List<Route> possibleRoutes;
        public int nextRoute;
        public RouteSelectSpot(List<Route> _routes, int _nextRoute){
            possibleRoutes = _routes;
            nextRoute = _nextRoute;
        }
    }
    
}
