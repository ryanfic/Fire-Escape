using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Exit : MonoBehaviour
{
    private string file = "evacuationTime.txt";
    private void OnTriggerEnter(Collider other) {
        
        if(other.gameObject.layer==LayerMask.NameToLayer("Evacuee")){
            Debug.Log("EXIT REACHED AT " + Time.timeSinceLevelLoad);
            Debug.Log("Time taken to reach exit: " + (Time.timeSinceLevelLoad - other.gameObject.GetComponent<Evacuee>().getInitTime()));
            WriteTime(file, (Time.timeSinceLevelLoad - other.gameObject.GetComponent<Evacuee>().getInitTime()));
            Destroy(other.gameObject);
        }
        
    }
    public void WriteTime(string file, float time){
        StreamWriter sw = new StreamWriter(file);
        string toadd = "Time to reach exit: " + time;
        sw.WriteLine(toadd);
        sw.Close();
    }
}
