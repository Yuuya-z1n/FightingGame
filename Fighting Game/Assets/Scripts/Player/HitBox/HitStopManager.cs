using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    private bool isHitStopping = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TriggerHitStop(float duration)
    {
        if (isHitStopping) return;
        StartCoroutine(HitStopCoroutine(duration));
    }

    private IEnumerator HitStopCoroutine(float duration)
    {
        isHitStopping = true;

        Time.timeScale = 0.0f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1.0f;

        isHitStopping = false;
    }
}
