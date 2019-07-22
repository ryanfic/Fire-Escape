using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Evacuee : MonoBehaviour
{   
    

    public GameObject searchedZonePrefab;

    public FieldOfView fov;

    public float sensorAngle;
    public float sensorRadius;
    public int edgeResolveIterations;
    public int gapResolveIterations;

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
        agent.Warp(Target.position);
        timer = updateFreq;
        Vector3 leftdir = DirFromAngle(-sensorAngle,false);
        /* Debug.DrawRay(transform.position, leftdir*100,Color.green, 10.0f);
        RaycastHit hit;
        //if we hit something
        if(Physics.Raycast(transform.position,leftdir,out hit,sensorRadius,wallMask)){
            //Debug.Log("hit!");
            bool onWall = leftWall.add(hit.point,this.transform);
            //If the point hit is not on the wall
            if(!onWall){
                //find the edge of the wall
                FindWallEdge(-sensorAngle,hit.point,leftWall);

                //check for gap
                bool gapFound = isGap(hit.point,leftWall);
                Debug.Log("Gap found: " +gapFound);

                //identify gap

            }
            leftWallLine.SetPositions(new Vector3[]{leftWall.getStartPoint(),leftWall.getEndPoint()});
        }*/
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
                //find the edge of the wall
                //FindWallEdge(-sensorAngle,hit.point,leftWall);

                //check for gap
                bool gapFound = isGap(hit.point,leftWall);
                Debug.Log("Gap found: " +gapFound);

                //identify gap

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
            leftWall = new Wall(points[0],points[1],wallYVal,agent.radius*2,wallErrorDst);
            leftWallLine.SetPositions(points);
            leftWallLine.enabled = true;
            Debug.Log("Left Wall Made!");
        }
        //fire to the left
        //fire slightly up from the left
        //set up the right wall
    }

    private void FindWallEdge(float missAngle, Vector3 missPos, Wall theWall){
        Vector3 toWallEdge = theWall.closestEdgePoint(missPos)-transform.position;
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

    public bool isGap(Vector3 missPos, Wall theWall){
        //We have found the edge of the wall
        //Project slightly less than the gap width past the edge of the wall
        //Fire a ray at that position
        //If it hits something along that wall, then there is no gap
        //if it hits something not along that wall, then there is a gap
        //if it hits nothing, then there is a gap
        Vector3 closestEdge = theWall.closestEdgePoint(missPos);
        Vector3 toWallEdge = closestEdge-transform.position;
        toWallEdge.y = 0;
        float minAngle = Vector3.SignedAngle(transform.TransformDirection(Vector3.forward),toWallEdge,Vector3.up);
        float maxAngle; //determined by projecting past the wall by slightly less than the gap width
        Vector3 spotPastWall; //the spot projected past the wall

        //if the closest edge is the end point of the wall
        if(closestEdge==theWall.getEndPoint()){
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





        for(int i = 0; i <gapResolveIterations; i++){
            Debug.DrawRay(transform.position,spotPastWall,Color.cyan, 20f);
            Debug.DrawRay(transform.position,toWallEdge,Color.red,20f);
            float angle = (minAngle + maxAngle)/2;

            //fire at the angle
            RaycastHit hit;
            //Debug.Log(angle);
            //Debug.DrawRay(transform.position, DirFromAngle(angle,true)*100,Color.black, 10.0f);
            //if we hit something
            if(Physics.Raycast(transform.position,DirFromAngle(angle,true),out hit,sensorRadius,wallMask)){
                bool onWall = theWall.add(hit.point,this.transform);
                //If the point hit is not on the wall
                if(!onWall){
                    //update max angle
                    Debug.DrawRay(transform.position, DirFromAngle(angle,true)*100,Color.black, 10.0f);
                    Debug.Log("Missed on step "+i);
                    maxAngle = angle;
                }
                //if we hit something along the wall
                else{
                    Debug.DrawRay(transform.position, DirFromAngle(angle,true)*100,Color.yellow, 10.0f);
                    Debug.Log("Hit on step " +i);
                    return false;

                }
            }
            //if we did not hit anything
            else{
                Debug.DrawRay(transform.position, DirFromAngle(angle,true)*100,Color.black, 10.0f);
                Debug.Log("Missed on step "+i);
                maxAngle = angle;
            }
        }
        return true;
    }

    public Vector3 DirFromAngle(float angleInDegrees,bool angleIsGlobal){
        //if the angle is not global, add the y transform
        if(!angleIsGlobal){
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),0,Mathf.Cos(angleInDegrees*Mathf.Deg2Rad));
    }


}
