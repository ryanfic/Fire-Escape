using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBuilderTool : Tool
{
    public override string Name {get {return "WallBuilder";}}


    public Material lineMaterial;

    private LineRenderer inProgressLine;
    public float verticalOffset = 0.1f;
    public float lineWidth = 1;



    private Vector3? lineStartPoint = null; // ? makes the vector3 nullable!


    void Awake(){
        base.Awake();
        depth = 100f;
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)){
            Vector3 point = GetMouseCameraPoint();
            point.y += verticalOffset;
            lineStartPoint = point;
        }
        else if(Input.GetMouseButton(0)){
            if(!lineStartPoint.HasValue){
                return;
            }
            drawLine();

        }
        else if(Input.GetMouseButtonUp(0)){
            if(!lineStartPoint.HasValue){
                return;
            }
            createWall();
            removeInProgressLine();

            lineStartPoint = null;
        }
    }

    /* private Vector3 GetMouseCameraPoint(){
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        //Debug.DrawRay(ray.origin, ray.direction * 100, Color.yellow, 10f);
        return ray.origin + ray.direction * depth;
    }

    private Vector3 GetMouseCameraPoint(LayerMask tgtLayer){
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray,out hit,tgtLayer.value)){
            Debug.Log("Hit on: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
            return hit.point;
        }
        else{
            return ray.origin + ray.direction * depth;
        }
    }*/

    private void drawLine(){
        Vector3 lineEndPoint = GetMouseCameraPoint();
        lineEndPoint.y += verticalOffset;
        //GameObject gameObject = new GameObject();
        if(inProgressLine == null)
        {
            /* inProgressLine = gameObject.GetComponent(typeof(LineRenderer)) as LineRenderer;
            if(inProgressLine==null)*/
            if((inProgressLine = gameObject.GetComponent(typeof(LineRenderer)) as LineRenderer)==null)
            {
                inProgressLine = gameObject.AddComponent<LineRenderer>();
            }
            
            inProgressLine.material = lineMaterial;
            inProgressLine.SetPositions(new Vector3[]{lineStartPoint.Value,lineEndPoint});
            inProgressLine.startWidth = lineWidth;
            inProgressLine.endWidth = lineWidth;
        }
        else
        {
            inProgressLine.SetPosition(1,lineEndPoint);
        }
        
        
    }

    private void createWall(){
        Vector3? lineEndPoint = GetMouseCameraPoint();
        float wallLength = Vector3.Distance(lineStartPoint.Value,lineEndPoint.Value);
        Vector3 wallPosition = (lineStartPoint.Value+lineEndPoint.Value)/2;
        Vector3 wallDir = lineEndPoint.Value - lineStartPoint.Value;
        Quaternion wallRotation = Quaternion.LookRotation(wallDir);
        //create the wall and add the wall to the UI controller's wall list
        objLists.addWall(WallFactory.Instance.createWall(wallLength,wallPosition,wallRotation));
    }
    public void createWall(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        objLists.addWall(WallFactory.Instance.createWall(scale.z,position,rotation));
    }


    private void removeInProgressLine(){
        inProgressLine.SetPositions(new Vector3[]{Vector3.zero,Vector3.zero});
        inProgressLine = null;
    }

}
