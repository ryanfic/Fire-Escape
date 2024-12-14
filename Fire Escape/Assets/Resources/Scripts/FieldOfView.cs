using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public float viewRadius;
    [Range(0,360)] //Clamps the value between 0 and 360
    public float viewAngle;

    public LayerMask[] targetMask; //layer that has targets
    public LayerMask obstacleMask; //layer that blocks vision

    [HideInInspector]//variable needs to be public because it is used in editor, but we dont want to see it in the inspector
    public List<Transform> visibleTargets = new List<Transform>();

    public float meshResolution; //the amount of rays shot out per degree in viewAngle
    public int edgeResolveIterations; //used when finding the edge of an object
    public float edgeDstThreshold;

    public MeshFilter viewMeshFilter;
    public MeshCollider meshCollider;
    private Mesh viewMesh;

    void Start(){
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = viewMesh;
        }
        StartCoroutine("FindTargetsWithDelay",.2f);
    }

    void LateUpdate(){ //only update field of view after the object is done changing its angle, stops from being jittery
        DrawFieldOfView();
    }
    //calls the FindVisibleTargets method after delay seconds, repeatedly
    IEnumerator FindTargetsWithDelay(float delay){
        while(true){
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }
    void FindVisibleTargets(){
        //remove everything we have seen before, will read if we see again
        visibleTargets.Clear();
        Collider[] targetsInViewRadius;
        List<Collider> allTargets = new List<Collider>();
        for(int i = 0;i<targetMask.Length;i++){
            Collider[] objectsOnLayer = Physics.OverlapSphere(transform.position,viewRadius,targetMask[i]);
            foreach(Collider obj in objectsOnLayer){
                allTargets.Add(obj);
            }
        }
        targetsInViewRadius = allTargets.ToArray();
        //Physics.OverlapSphere(transform.position,viewRadius,targetMask[0]);
        
        for(int i = 0; i<targetsInViewRadius.Length;i++){
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            //if the target is withing the field of view
            if(Vector3.Angle(transform.forward,dirToTarget)<viewAngle/2){
                float dstToTarget = Vector3.Distance(transform.position, target.position);
                //cast a ray to the target, if there is no collisions with things on the obstacleMask layer, do stuff
                if(!Physics.Raycast(transform.position,dirToTarget,dstToTarget,obstacleMask)){
                    //Stuff to do when we see a target
                    //Lets keep a list of things we see!
                    visibleTargets.Add(target);
                }
            }
        }
    }

    void DrawFieldOfView(){
        int stepCount = Mathf.RoundToInt(viewAngle*meshResolution);//also known as raycount
        float stepAngleSize = viewAngle/stepCount; //how many degrees are in each step
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();
        for(int i = 0; i<=stepCount;i++){
            float angle = transform.eulerAngles.y - viewAngle/2 + stepAngleSize*i; //the angle is the y transform of the object, minus half the field of view angle, and then we add the number of steps in we are
            ViewCastInfo newViewCast = ViewCast(angle);

            //when i = 0, oldViewCast has not been set yet
            if(i>0){
                //to help with the case when two obstacles are hit, but one is farther than the other
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                //check if the previous viewcast hit and the new one did not (edge is between the two viewcasts)
                //or, if the old viewcast hit something and the new one did as well, but the edge distance threshold was exceeded
                if(oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded)){
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if(edge.pointA != Vector3.zero){
                        viewPoints.Add(edge.pointA);
                    }
                    if(edge.pointB != Vector3.zero){
                        viewPoints.Add(edge.pointB);
                    }
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        //for making mesh!
        int vertexCount = viewPoints.Count +1; //how many times the ray was shot out, plus one since we include the origin
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount-2)*3];
        
        //Note that since the mesh will be a child of the player object, the positions of the vertices need to be relative to the player as well
        vertices[0] = Vector3.zero; //the position of the player relative to itself is zero
        //iterate through the list of points
        for(int i = 0; i<vertexCount-1;i++) {//its vertexCount-1 because we already defined one point (the pos of the player)
            vertices[i+1] = transform.InverseTransformPoint(viewPoints[i]); //gets the relative position to the transform of the object

            if(i<vertexCount-2){
                triangles[i*3] = 0;
                triangles[i*3+1] = i+1;
                triangles[i*3+2] = i+2;
            }
        }

        viewMesh.Clear(); //clear the view mesh
        viewMesh.vertices = vertices; //set the view mesh vertices
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    /*
    Finds the edge of an object given two viewcasts
     */
    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast){
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for(int i = 0; i <edgeResolveIterations; i++){
            float angle = (minAngle + maxAngle)/2;
            ViewCastInfo newViewCast = ViewCast (angle);

            bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
            //if the new viewcast hit the object and the edge distance threshold is not exceeded
            if(newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded){
                minAngle = angle;
                minPoint = newViewCast.point;
            } 
            else {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint,maxPoint);
    }

    ViewCastInfo ViewCast(float globalAngle){
        Vector3 dir = DirFromAngle(globalAngle,true);
        RaycastHit hit;

        //if we hit something
        if(Physics.Raycast(transform.position,dir,out hit,viewRadius,obstacleMask)){
            return new ViewCastInfo(true,hit.point,hit.distance,globalAngle); //pass out the info of the collision
        }
        //if we dont hit anything
        else{
            return new ViewCastInfo(false,transform.position +dir *viewRadius, viewRadius,globalAngle); //pass out the direction, but have it go to the end of the radius
        }
    }

    public Vector3 DirFromAngle(float angleInDegrees,bool angleIsGlobal){
        //if the angle is not global, add the y transform
        if(!angleIsGlobal){
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),0,Mathf.Cos(angleInDegrees*Mathf.Deg2Rad));
    }

    public struct ViewCastInfo{
        public bool hit; //if it hit
        public Vector3 point; //position of hit
        public float dst; //distance to hit
        public float angle; //angle that the ray was cast at

        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle){
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    public struct EdgeInfo{
        public Vector3 pointA; //closest point on the obstacle to the edge
        public Vector3 pointB; //closest point to the edge off the obstacle

        public EdgeInfo(Vector3 _pointA, Vector3 _pointB){
            pointA = _pointA;
            pointB = _pointB;
        }
    }
}
