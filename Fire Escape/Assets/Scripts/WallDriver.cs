using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallDriver : MonoBehaviour
{
    public float tolerance;
    Wall thewall;
    public LineRenderer line;
    void Start()
    {
        line = gameObject.GetComponent<LineRenderer>();

    }
    void Update() {
        Vector3 point = new Vector3(0.1f,0,0);
        
    }

}
