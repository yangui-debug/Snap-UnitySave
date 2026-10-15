using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
[ExecuteAlways]
public class ParticleHandler : MonoBehaviour
{
    public GameObject castParticle;
    public float castFXDuration;
    public GameObject loopingParticle;
    public float loopDuration;
    public GameObject endParticle;

    private void OnEnable()
    {
        Cast();
    }

    public void Cast()
    {
        StopAllCoroutines();
        StartCoroutine(Flow());
    }

    IEnumerator Flow()
    {
        endParticle.SetActive(false);
        castParticle.SetActive(true);
        yield return new WaitForSeconds(castFXDuration);
        loopingParticle.SetActive(true);
        yield return new WaitForSeconds(loopDuration);
        endParticle.SetActive(true);
        castParticle.SetActive(false);
        loopingParticle.SetActive(false);
    }
}





