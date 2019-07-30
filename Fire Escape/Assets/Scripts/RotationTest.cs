using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationTest : MonoBehaviour
{
    public bool leftPeeking = true;
    public bool rightPeeking = false;
    public bool hasPeeked = false;
    public float rot = 0;
    public float rotationSpeed;

    // Update is called once per frame
    void Update()
    {
        /* if(leftPeeking){
            peek(true);
            /* if(!hasPeeked){
                //Debug.Log(Time.deltaTime*rotationSpeed);
                transform.Rotate(-Vector3.up*Time.deltaTime*rotationSpeed,Space.World);
                rot+=Time.deltaTime*rotationSpeed;
                if(rot>=90){
                    float correction = 90-rot;
                    transform.Rotate(-Vector3.up*correction,Space.World);
                    rot+=correction;
                    Debug.Log(rot);
                    //leftPeek = false;
                    hasPeeked = true;
                }
            }
            else{
                transform.Rotate(Vector3.up*Time.deltaTime*rotationSpeed,Space.World);
                rot-=Time.deltaTime*rotationSpeed;
                if(rot<0){
                    float correction = 0-rot;
                    transform.Rotate(Vector3.up*correction,Space.World);
                    rot+=correction;
                    Debug.Log(rot);
                    leftPeek = false;
                    hasPeeked = false;
                }
            }*/
        /* }
        else if(rightPeeking){
            peek(false);
        }*/
        transform.rotation = Quaternion.LookRotation(Vector3.left);
    }
    private void peek(bool leftPeek){
        int rotationAxis = leftPeek?-1:1;
        if(!hasPeeked){
            //Debug.Log(Time.deltaTime*rotationSpeed);
            transform.Rotate(rotationAxis*Vector3.up*Time.deltaTime*rotationSpeed,Space.World);
            rot+=Time.deltaTime*rotationSpeed;
            if(rot>=90){
                float correction = 90-rot;
                transform.Rotate(-Vector3.up*correction,Space.World);
                rot+=correction;
                Debug.Log(rot);
                //leftPeek = false;
                hasPeeked = true;
            }
        }
        else{
            transform.Rotate(-rotationAxis*Vector3.up*Time.deltaTime*rotationSpeed,Space.World);
            rot-=Time.deltaTime*rotationSpeed;
            if(rot<0){
                float correction = 0-rot;
                transform.Rotate(Vector3.up*correction,Space.World);
                rot+=correction;
                Debug.Log(rot);
                if(leftPeeking){
                    leftPeeking = false;
                }
                else if(rightPeeking){
                    rightPeeking = false;
                }
                hasPeeked = false;
            }
        }
    }
}
