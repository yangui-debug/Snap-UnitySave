using System.Collections;
using UnityEngine;

public class BottomJumpAnim : MonoBehaviour
{
    [SerializeField] private Animation a1;
    [SerializeField] private Animation a2;
    [SerializeField] private TrailRenderer[] trails;

    private float trailTime;

    private void Awake()
    {
        if (trailTime == 0)
        {
            trailTime = trails[0].time;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(ClearTrailsNextFrame());
    }

    public void OnTrigger()
    {
        PlayFromFrame(a1, 0);
        PlayFromFrame(a2, 30);

        StartCoroutine(ClearTrailsNextFrame());
    }

    private IEnumerator ClearTrailsNextFrame()
    {
        foreach (var trail in trails)
        {
            trail.time = 0;
        }

        yield return new WaitForEndOfFrame();
        foreach (var trail in trails)
        {
            trail.time = trailTime;
        }
    }

    private void PlayFromFrame(Animation anim, int startFrame)
    {
        float startTime = startFrame / anim.clip.frameRate;
        var state = anim[anim.clip.name];
        state.time = startTime;
        state.enabled = true;
        anim.Play(anim.clip.name);
    }
}