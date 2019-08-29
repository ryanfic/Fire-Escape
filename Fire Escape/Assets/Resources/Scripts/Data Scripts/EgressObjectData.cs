using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EgressObjectData {
    public float[] position;
    public float[] rotation;

    public string objType;

    
    public EgressObjectData(GameObject obj,string _objType){
        objType = _objType;
        position = new float[3];
        rotation = new float[4];
        //if the object is the correct type
        if(obj.layer == LayerMask.NameToLayer(objType))
        {
            //set up the data
            position[0] = obj.transform.position.x;
            position[1] = obj.transform.position.y;
            position[2] = obj.transform.position.z;

            rotation[0] = obj.transform.rotation.x;
            rotation[1] = obj.transform.rotation.y;
            rotation[2] = obj.transform.rotation.z;
            rotation[3] = obj.transform.rotation.w;
        }
        //if the object is not the correct type
        else
        {
            //there was an error
            Debug.Log("Object is of the incorrect type");
            //set up position and rotation with 0 values
            for(int i = 0; i<3; i++)
            {
                position[i] = 0f;
                rotation[i] = 0f;
            }
            rotation[4] = 0f; 
        }
        
    }
}
