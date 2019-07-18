using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallDriver : MonoBehaviour
{
    public float tolerance;
    public Vector3[] points;
    public int numpoint;
    public int targetLayer;

    public float YVal;

    public Vector3 dir;



    Wall thewall;
    public LineRenderer line;
    void Start()
    {
        line = gameObject.GetComponent<LineRenderer>();
        points = new Vector3[2];
        numpoint = 0;
        targetLayer = 1<<10;
    }
    void Update() {
        RaycastHit hit;
        if(Physics.Raycast(transform.position,transform.TransformDirection(Vector3.forward),out hit, Mathf.Infinity)){
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.red);
            //Debug.Log("P:"+hit.point.x);
            if(numpoint<2)
            {
                if(numpoint == 0){
                    points[0] = hit.point;
                    numpoint++;
                }
                else{
                    if(Vector3.Distance(hit.point,points[0])>0.5f)
                    {
                        Debug.Log("Longer than 0.5");
                        points[1]=hit.point;
                        thewall = new Wall(points[0],points[1],YVal,0.5f,tolerance);
                        numpoint++;
                    }
                }
            
            }
             else{
                
                thewall.add(hit.point,this.transform);

                
                line.SetPosition(0,thewall.getStartPoint());
                line.SetPosition(1,thewall.getEndPoint());
                dir = thewall.getDir();
            }
        }
    }
}
