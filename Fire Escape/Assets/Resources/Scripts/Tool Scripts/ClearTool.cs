using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearTool : Tool
{
    public override string Name {get {return "Clear";}}

    public void OnEnable()
    {
        clearSimulation();
    }

    public void clearSimulation()
    {
        if(objLists != null)
        {
            //delete everything
            foreach(GameObject evac in objLists.getEvacueeList())
            {
                Destroy(evac);
            }
            objLists.clearEvacueeList();
            foreach(GameObject fe in objLists.getFireExitList())
            {
                Destroy(fe);
            }
            objLists.clearFireExitList();
            foreach(GameObject window in objLists.getWindowList())
            {
                Destroy(window);
            }
            objLists.clearWindowList();
            foreach(GameObject wall in objLists.getWallList())
            {
                Destroy(wall);
            }
            objLists.clearWallList();
            foreach(GameObject footprint in objLists.getFootprintList())
            {
                Destroy(footprint);
            }
            objLists.clearFootprintList();
        }
    }
}
