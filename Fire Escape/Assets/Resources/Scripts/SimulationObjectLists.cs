using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationObjectLists : MonoBehaviour
{
    private List<GameObject> evacueeList = new List<GameObject>();

    private List<GameObject> fireExitList = new List<GameObject>();
    private List<GameObject> windowList = new List<GameObject>();
    private List<GameObject> wallList = new List<GameObject>();
    private List<GameObject> footprintList = new List<GameObject>();

    /* void Update()
    {
        int evaCount = 0, feCount = 0, winCount = 0, wallCount = 0;
        foreach(GameObject evacuee in evacueeList)
        {
            evaCount++;
        }
        foreach(GameObject fireexit in fireExitList)
        {
            feCount++;
        }
        foreach(GameObject window in windowList)
        {
            winCount++;
        }
        foreach(GameObject wall in wallList)
        {
            wallCount++;
        }
        Debug.Log(evaCount + " Evacuees");
        Debug.Log(feCount + " Fire Exits");
        Debug.Log(winCount + " Windows");
        Debug.Log(wallCount + " Walls");
        
    }*/
    public List<GameObject> getEvacueeList()
    {
      return evacueeList;  
    }
    public List<GameObject> getFireExitList()
    {
      return fireExitList;  
    }
    public List<GameObject> getWindowList()
    {
      return windowList;  
    }
    public List<GameObject> getWallList()
    {
      return wallList;  
    }
    public List<GameObject> getFootprintList()
    {
        return footprintList;
    }
    public void addEvacuee(GameObject evacuee)
    {
        //check if the object to add is an evacuee by seeing if the evacuee is on the evacuee layer
        if(evacuee.layer == LayerMask.NameToLayer("Evacuee"))
        {
            //if the object is on the evacuee layer, add the object to the evacuee list
            evacueeList.Add(evacuee);
        }
    }
    public void removeEvacuee(GameObject evacuee)
    {
        //check if the object to remove is an evacuee by seeing if the evacuee is on the evacuee layer
        if(evacuee.layer == LayerMask.NameToLayer("Evacuee"))
        {
            //if the object is on the evacuee layer, check if the evacuee is in the evacuee list
            if(evacueeList.Contains(evacuee))
            {
                //if the evacuee is on the list, remove the evacuee
                evacueeList.Remove(evacuee);
            }
        }
    }
    public void clearEvacueeList()
    {
        evacueeList = new List<GameObject>();
    }
    public void addFireExit(GameObject fireExit)
    {
        //check if the object to add is a fire exit by seeing if the object is on the fire exit layer
        if(fireExit.layer == LayerMask.NameToLayer("FireExit"))
        {
            //if the object is on the fire exit layer, add the object to the fire exit list
            fireExitList.Add(fireExit);
        }
    }
    public void removeFireExit(GameObject fireExit)
    {
        //check if the object to remove is a fire exit by seeing if the object is on the fire exit layer
        if(fireExit.layer == LayerMask.NameToLayer("FireExit"))
        {
            //if the object is on the fire exit layer, check if the object is on the fire exit list
            if(fireExitList.Contains(fireExit))
            {
                //if the fire exit is on the list, remove the fire exit
                fireExitList.Remove(fireExit);
            }
        }
    }
    public void clearFireExitList()
    {
        fireExitList = new List<GameObject>();
    }
    public void addWindow(GameObject window)
    {
        //check if the object to add is a window by seeing if the object is on the window layer
        if(window.layer == LayerMask.NameToLayer("Window"))
        {
            //if the object is on the window layer, add the object to the window list
            windowList.Add(window);
        }
    }
    public void removeWindow(GameObject window)
    {
        //check if the object to remove is a window by seeing if the object is on the window layer
        if(window.layer == LayerMask.NameToLayer("Window"))
        {
            //if the object is on the window layer, check if  the object is on the window list
           if(windowList.Contains(window))
            {
                //if the window is on the list, remove the window
                windowList.Remove(window);
            }
        }
    }
    public void clearWindowList()
    {
        windowList = new List<GameObject>();
    }
    public void addWall(GameObject wall)
    {
        //check if the object to add is a wall by seeing if the object is on the wall layer
        if(wall.layer == LayerMask.NameToLayer("Wall"))
        {
            //if the object is on the wall layer, add the object to the wall list
            wallList.Add(wall);
        }
    }
    public void removeWall(GameObject wall)
    {
        //check if the object to remove is a wall by seeing if the object is on the wall layer
        if(wall.layer == LayerMask.NameToLayer("Wall"))
        {
            //if the object is on the wall layer, check if the object is on the wall list
            if(wallList.Contains(wall))
            {
                //if the wall is on the list, remove the wall
                wallList.Remove(wall);
            }
        }
    }
    public void clearWallList()
    {
        wallList = new List<GameObject>();
    }
    public void addFootprint(GameObject footprint)
    {
        //check if the object to add is a footprint by seeing if the object is on the footprint layer
        if(footprint.layer == LayerMask.NameToLayer("Footprint"))
        {
            //if the object is on the footprint layer, add the object to the footprint list
            footprintList.Add(footprint);
        }
    }
    public void removeFootprint(GameObject footprint)
    {
        //check if the object to remove is a footprint by seeing if the object is on the footprint layer
        if(footprint.layer == LayerMask.NameToLayer("Footprint"))
        {
            //if the object is on the footprint layer, check if the object is on the footprint list
            if(footprintList.Contains(footprint))
            {
                //if the footprint is on the list, remove the footprint
                footprintList.Remove(footprint);
            }
        }
    }

    public void clearFootprintList()
    {
        footprintList = new List<GameObject>();
    }
}
