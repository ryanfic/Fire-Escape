using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowFactoryTool : EvacuationObjectFactoryTool
{
    public override string Name {get {return "WindowFactory";}}
    
    void Awake()
    {
        base.Awake();
        //arrowPrefab = (GameObject)Resources.Load("Prefabs/Arrow", typeof(GameObject));
        evacuationObjectPrefab = (GameObject)Resources.Load("Prefabs/Window", typeof(GameObject));
        evacuationObjectHeight = 2f;
        
    }
    protected override void setObjectInfo()
    {
        newEvacuationObject.name = "Window";
        newEvacuationObject.tag = "Window";
        newEvacuationObject.layer = LayerMask.NameToLayer("Window");
        //add the window to the UI controller's window list
        objLists.addWindow(newEvacuationObject);

    }
}
