using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Evacuee : MonoBehaviour
{   
    

    public GameObject searchedZonePrefab;

    public FieldOfView fov;

    public float sensorAngle;
    public float sensorRadius;
    public int edgeResolveIterations;

    public LayerMask wallMask;

    public Wall leftWall;

    public Wall rightWall;

    public float wallYVal;


    public enum EvacueeMovementState{
        toExit,toStairs,wandering
    }
    public EvacueeMovementState curMoveState = EvacueeMovementState.wandering;

    




    private List<GameObject> seenExits = new List<GameObject>();
    private List<GameObject> seenStairs = new List<GameObject>();

    void Start(){
        fov = gameObject.GetComponent<FieldOfView>();
        SetUp();
    }

    void Update(){
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

        //Fire rays to the left and right
        Vector3 leftdir = DirFromAngle(-sensorAngle,false);
        RaycastHit hit;
        //if we hit something
        if(Physics.Raycast(transform.position,leftdir,out hit,sensorRadius,wallMask)){
            bool onWall = leftWall.add(hit.point,this.transform);
            //If the point hit is not on the wall
            if(!onWall){
                //find the edge of the wall
                FindWallEdge(-sensorAngle,hit.point,leftWall);
                //check for gap

                //identify gap

            }
        }
        //if we did not hit something
        else{

        }
    }

    private void SetUp(){
        //set up the left wall
        RaycastHit hit;
        Vector3[] points = new Vector3[2];
        points[0].y = -100;
        points[1].y = -100;
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left)*100,Color.red, 10.0f);
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left+ new Vector3(0,0,0.1f))*100,Color.blue, 10.0f);
        if(Physics.Raycast(transform.position,transform.TransformDirection(Vector3.left),out hit,sensorRadius,wallMask)){
            points[0] = hit.point;
        }
        if(Physics.Raycast(transform.position,transform.TransformDirection(Vector3.left+ new Vector3(0,0,0.1f)),out hit,sensorRadius,wallMask)){
            points[1] = hit.point;
        }
        if(points[0].y!=-100&&points[1].y!=-100){
            leftWall = new Wall(points[0],points[1],wallYVal,gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().radius*2,0.5f);
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

    public Vector3 DirFromAngle(float angleInDegrees,bool angleIsGlobal){
        //if the angle is not global, add the y transform
        if(!angleIsGlobal){
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),0,Mathf.Cos(angleInDegrees*Mathf.Deg2Rad));
    }


}
