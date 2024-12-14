using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSmellDetector : MonoBehaviour
{
    //[HideInInspector]
    public int smokeParticlesCount;
    public int smokeParticleThreshold = 50;

    void Awake()
    {
        smokeParticlesCount = 0;
        Object triggerAsObject = GameObject.Find("FieldOfView");
        //Object triggerAsObject = GameObject.Find("SmokeSmellDetectorTrigger");
        if (triggerAsObject != null)
        {
            Debug.Log("Found trigger object!");

        }
        else
        {
            Debug.Log("No trigger found for smoke smell detector...removing self!");
            Destroy(this);

        }
    }

    void LateUpdate()
    {
        if (IsSmokeAboveThreshold())
        {
            Debug.Log("WE ARE ABOVE THE [" + smokeParticleThreshold + "] THRESHOLD BECAUSE WE ARE AT [" + smokeParticlesCount + "] PARTICLES!");
        }
    }

    public bool IsSmokeAboveThreshold()
    {
        return smokeParticlesCount >= smokeParticleThreshold;
    }

    public void AddSmokeParticle()
    {
        smokeParticlesCount++;
    }
    public void RemoveSmokeParticle() { smokeParticlesCount--; }
    public void ChangeSmokeParticles(int numParticlesDiff)
    {
        smokeParticlesCount += numParticlesDiff;
    }

    public void SetSmokeParticles(int numParticles)
    {
        smokeParticlesCount = numParticles;
    }
}
