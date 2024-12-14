using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SmokeCountDisplay : MonoBehaviour
{
    public Text counterText;
    public GameObject smokeSmellDetectorGO;
    private SmokeSmellDetector detector;
    void Awake()
    {
        if (smokeSmellDetectorGO == null)
        {
            Debug.Log("No detector found...removing self!");
            Destroy(counterText);
            Destroy(this);
        }
        else
        {
            detector = smokeSmellDetectorGO.GetComponent<SmokeSmellDetector>();
        }
        /*
        smokeParticlesCount = 0;
        Object triggerAsObject = GameObject.Find("SmokeSmellDetectorTriggers");
        if (triggerAsObject != null)
        {
            Debug.Log("Found trigger object!");

        }
        else
        {
            Debug.Log("No trigger found...removing self!");
            Destroy(this);

        }
        */
    }

    public void Update()
    {
        if (detector == null)
        {
            Debug.Log("No detector found on update...removing self!");
            Destroy(counterText);
            Destroy(this);
        }
        else
        {
            counterText.text = "Smoke Level: " + detector.smokeParticlesCount.ToString();
        }
    }
}
