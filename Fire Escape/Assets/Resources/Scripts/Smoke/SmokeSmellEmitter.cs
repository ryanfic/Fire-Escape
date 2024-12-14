using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSmellEmitter : MonoBehaviour
{
    ParticleSystem ps;

    public SmokeSmellDetector smokeDetector;

    // these lists are used to contain the particles which match
    // the trigger conditions each frame.
    List<ParticleSystem.Particle> inside = new List<ParticleSystem.Particle>();

    /*
    // THIS IS FOR THE IN/OUT FLUX VERSION

    // these lists are used to contain the particles which match
    // the trigger conditions each frame.
    List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();
    List<ParticleSystem.Particle> exit = new List<ParticleSystem.Particle>();

    */

    void Awake()
    {
        if (smokeDetector != null)
        {
            Debug.Log("Smoke detector was found (for emitter)");
        }
        else
        {
            Debug.Log("No smoke detector found...?");
        }
    }

    void OnEnable()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void OnParticleTrigger()
    {
        Debug.Log("We got a trigger! " + Time.time);
        //get
        int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);

        // logic to determine how many particles are inside a specific trigger
        // for a single agent there is only one trigger, so the value is the total number
        int particlesInsideTrigger = numInside;
        if (smokeDetector != null)
        {
            smokeDetector.SetSmokeParticles(particlesInsideTrigger);
        }

        //Debug.Log("numEnter: " + numEnter + ", numExit: " + numExit);

        // set
        ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
    }

    /*
    // THIS IS FOR THE IN/OUT FLUX VERSION
    void OnParticleTrigger()
    {
        //get
        int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter);
        int numExit = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Exit, exit);

        // logic to determine how many particles entered or exited the trigger
        // for a single agent there is only one trigger, so the value is the difference in the entered and exiting particles
        int particleFlux = numEnter - numExit;
        if (smokeDetector != null) {
            smokeDetector.ChangeSmokeParticles(particleFlux);
        }

        //Debug.Log("numEnter: " + numEnter + ", numExit: " + numExit);

        // set
        ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter);
        ps.SetTriggerParticles(ParticleSystemTriggerEventType.Exit, exit);
    }
    */
}
