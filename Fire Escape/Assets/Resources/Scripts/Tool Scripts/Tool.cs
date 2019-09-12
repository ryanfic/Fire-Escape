using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Tool : MonoBehaviour
{
    //returns the name of the tool
    public abstract string Name {get;}
    protected Camera cam;
    protected LayerMask targetLayer;
    public float depth = 10;
    //protected bool isMouseOverUI = false;
    protected SimulationObjectLists objLists;

    protected void Awake() 
    {
        targetLayer = LayerMask.GetMask("Ground");
    }


    protected Vector3 GetMouseCameraPoint(){
        //if there is a target layer
        if(targetLayer.value != 0)
        {
            //target the target layer
            return GetMouseCameraPoint(targetLayer);
        }
        else
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            //Debug.DrawRay(ray.origin, ray.direction * 100, Color.yellow, 10f);
            return ray.origin + ray.direction * depth;
        }
    }

    protected Vector3 GetMouseCameraPoint(LayerMask tgtLayer){
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray,out hit,Mathf.Infinity,tgtLayer.value)){
            //Debug.Log("Hit!");
            //Debug.Log("Hit on: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
            return hit.point;
        }
        else{
            //Debug.Log("No hit...");
            return ray.origin + ray.direction * depth;
        }
    }

    public void switchCamera(Camera _camera)
    {
        cam = _camera;
    }
    public void addSimObjLists(SimulationObjectLists _objLists)
    {
        objLists = _objLists;
    }

    /*public void SetIsMouseOverUI(bool _isMouseOverUI)
    {
        isMouseOverUI = _isMouseOverUI;
    }*/
}
