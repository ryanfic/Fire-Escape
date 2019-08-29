﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EvacuationObjectFactoryTool : Tool
{


    protected GameObject arrowPrefab;
    protected float evacuationObjectArrowHeight = 0.5f;
    protected GameObject evacuationObjectPrefab;
    protected float evacuationObjectHeight;




    protected Vector3? evacuationObjectSpawnPoint = null; // ? makes the vector3 nullable!
    protected GameObject evacuationObjectArrow;
    
    protected GameObject newEvacuationObject;
    

    public void Awake()
    {
        base.Awake();
        arrowPrefab = (GameObject)Resources.Load("Prefabs/Arrow", typeof(GameObject));
        
    }


    // Update is called once per frame
    public void Update()
    {
        if(Input.GetMouseButtonDown(0)){
            evacuationObjectSpawnPoint = GetMouseCameraPoint();
            //Debug.Log("Spawn point: " + evacuationObjectSpawnPoint);
        }
        else if(Input.GetMouseButton(0)){
            if(!evacuationObjectSpawnPoint.HasValue){
                return;
            }
            drawArrow();

        }
        else if(Input.GetMouseButtonUp(0)){
            if(!evacuationObjectSpawnPoint.HasValue){
                return;
            }
            createObject();
            removeInProgressDirection();

            evacuationObjectSpawnPoint = null;
        }
    }
    void OnDisable() 
    {
        removeInProgressDirection();
    }

    /* private Vector3 GetMouseCameraPoint(){
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

    private Vector3 GetMouseCameraPoint(LayerMask tgtLayer){
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray,out hit,tgtLayer.value)){
            //Debug.Log("Hit!");
            //Debug.Log("Hit on: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
            return hit.point;
        }
        else{
            //Debug.Log("No hit...");
            return ray.origin + ray.direction * depth;
        }
    }*/


    private void drawArrow(){
        Vector3? evacDirEndPoint = GetMouseCameraPoint();
        Vector3 arrDir = evacDirEndPoint.Value - evacuationObjectSpawnPoint.Value;
        //ensure the y value is zero to stop the arrow from pointing in odd directions
        arrDir.y = 0;
        //if there is no difference from the start and end point
        if(arrDir == Vector3.zero)
        {
            arrDir = Vector3.forward;
        }
        Quaternion arrRotation = Quaternion.LookRotation(arrDir);
        //if there is no arrow
        if(evacuationObjectArrow == null)
        {
            //make the arrow
            //the spawn location is the same as the evacuee spawn point, plus the height of the arrow
            Vector3 arrPosition = evacuationObjectSpawnPoint.Value;
            arrPosition.y += evacuationObjectArrowHeight;
            evacuationObjectArrow = Instantiate(arrowPrefab,arrPosition,arrRotation);
            //evacuationObjectArrow.tag = "Arrow";
            evacuationObjectArrow.name = "Direction Arrow";
        }
        //if there is an arrow
        else
        {
            //update the direction
            evacuationObjectArrow.transform.rotation = arrRotation;
        }
    }

    private void createObject(){
        Vector3? evacDirEndPoint = GetMouseCameraPoint();
        //float wallLength = Vector3.Distance(lineStartPoint.Value,lineEndPoint.Value);
        Vector3 evacPosition = evacuationObjectSpawnPoint.Value;
        evacPosition.y += evacuationObjectHeight;
        //increase the y so that the evacuee spawns above the ground
        //evacPosition.y += 0.5f;
        Vector3 evacDir = evacDirEndPoint.Value - evacuationObjectSpawnPoint.Value;
        //make the y value zero to ensure the evacuee does not spawn in odd directions
        evacDir.y = 0;
        if(evacDir == Vector3.zero)
        {
            evacDir = Vector3.forward;
        }

        Quaternion evacRotation = Quaternion.LookRotation(evacDir);
        
        newEvacuationObject = Instantiate(evacuationObjectPrefab,evacPosition,evacRotation);
        setObjectInfo();
        //WallFactory.Instance.createWall(wallLength,wallPosition,wallRotation);
    }
    public void createObject(Vector3 position, Quaternion rotation)
    {
        newEvacuationObject = Instantiate(evacuationObjectPrefab,position,rotation);
        setObjectInfo();
    }

    protected abstract void setObjectInfo();



    private void removeInProgressDirection(){
        Destroy(evacuationObjectArrow);
        evacuationObjectArrow = null;
    }
}
