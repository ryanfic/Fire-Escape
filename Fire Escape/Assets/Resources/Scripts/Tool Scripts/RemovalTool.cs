using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemovalTool : Tool
{
    public override string Name {get {return "Removal";}}
    
    private int removableObjectLayers; //the layers that have objects that can be deleted
    private Material coverMaterial; //the material for the cover of the current target (should be transparent)
    private float coverUpscale = 0.5f; //how much the cover is increased in size from the original target, to allow the cover to actually cover the target

    private GameObject currentTarget = null; //the current target of the removal tool
    private GameObject targetCover = null; //a visual cover over the current target

    void Awake()
    {
        base.Awake();
        coverMaterial = (Material)Resources.Load("Materials/RemovalCoverMaterial", typeof(Material)); //load the material
        removableObjectLayers = LayerMask.GetMask("Evacuee", "FireExit", "Window","Wall"); //set the layers that have objects that can be deleted
    }

    /*
        This script constantly fires rays at objects on the removable object layers, if the rays strike an object, the current target is updated, and a target cover is created.
        This script also listens for the release of the left mouse button, and if there is a target at that time, that target is deleted from the scene.
     */
    void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        //if the ray hits something on the removable object layers
        if(Physics.Raycast(ray,out hit,Mathf.Infinity,removableObjectLayers)){
            //select the object hit
            addTargetObject(hit.transform.gameObject);
            //if the person lets go of the left mouse button
            if(Input.GetMouseButtonUp(0))
            {
                //delete the current target
                deleteTargetObject();
            }
        }
        //if the ray did not hit something on the removable object layers, but was previously had another object highlighted
        else if (currentTarget != null)
        {
            //remove the currentTarget reference (and the target cover)
            removeTargetObjectReference();
        }
    }

    /*
        When this script is disabled, remove the current target reference (and remove the target cover).
     */
    void OnDisable()
    {
        removeTargetObjectReference();
    }

    /*
        Make an object the current target, if the object exists and is not currently the current target.
     */
    private void addTargetObject(GameObject target)
    {
        //if there is no current target or the new target is not the same as the old target
        if(currentTarget == null ||!(currentTarget==target))
        {
            //set the current target to the new target
            currentTarget = target;
            //change the target cover
            addTargetCover();
        }
        //if the targets are the same, do nothing
    }

    /*
        Delete the current target from the scene. This also removes the reference to the current target (and removes the target cover).
     */
    private void deleteTargetObject()
    {
        //if the current target is not null
        if(!(currentTarget == null))
        {
            //remove the current target from the UI list
            //if the target's layer is the evacuee layer, the target is an evacuee
            if(currentTarget.layer == LayerMask.NameToLayer("Evacuee"))
            {
                objLists.removeEvacuee(currentTarget);
            }
            //if the target's layer is the fire exit layer, the target is a fire exit
            else if(currentTarget.layer == LayerMask.NameToLayer("FireExit"))
            {
                objLists.removeFireExit(currentTarget);
            }
            //if the target's layer is the window layer, the target is a window
            else if(currentTarget.layer == LayerMask.NameToLayer("Window"))
            {
                objLists.removeWindow(currentTarget);
            }
            //if the target's layer is the wall layer, the target is a wall
            else if(currentTarget.layer == LayerMask.NameToLayer("Wall"))
            {
                objLists.removeWall(currentTarget);
            }

            //destroy the current target
            Destroy(currentTarget);
            //remove the current target reference
            removeTargetObjectReference();
        }
    }

    /*
        Removes the current target reference by setting it to null. The target cover is also removed.
     */
    private void removeTargetObjectReference()
    {
        //delete the target cover object, then set currenttarget to null
        removeTargetCover();
        currentTarget = null;

    }

    /*
        Creates an object that has the same shape and orientation as the current target, but the mesh is slightly bigger. This is a cover for the current target, to visually label
        the curent target as the current target.
     */
    private void addTargetCover()
    {
        MeshFilter tcFilter;
        //if there is not already a targetCover
        if(targetCover == null)
        {
            //create a targetCover
            targetCover = new GameObject();
            targetCover.name = "Target Cover";
            targetCover.tag = "TargetCover";
            MeshRenderer tcRenderer = targetCover.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            tcRenderer.material = coverMaterial;
            tcFilter = targetCover.AddComponent(typeof(MeshFilter)) as MeshFilter;
        }
        else
        {
            tcFilter = targetCover.GetComponent(typeof(MeshFilter)) as MeshFilter;
        }
        targetCover.transform.position = currentTarget.transform.position;
        targetCover.transform.rotation = currentTarget.transform.rotation;
        tcFilter.mesh =  (currentTarget.GetComponent(typeof(MeshFilter)) as MeshFilter).mesh;
        targetCover.transform.localScale = new Vector3(currentTarget.transform.localScale.x+coverUpscale,currentTarget.transform.localScale.y+coverUpscale,currentTarget.transform.localScale.z+coverUpscale);
    }

    /*
        Removes the target cover by destroying the target cover game object and setting the reference to null.
     */
    private void removeTargetCover()
    {
        //if there is a target cover
        if(targetCover != null)
        {
            //destroy the target cover
            Destroy(targetCover);
            targetCover = null;
        }
    }
}
