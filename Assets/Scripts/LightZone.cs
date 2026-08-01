using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class LightZoneTrigger : MonoBehaviour
{
    public Light2D globalLight;

    public float targetIntensity = 0.15f;
    public Color targetColor = new Color(0.16f, 0.16f, 0.22f); // tím xanh tối
    public float transitionDuration = 1.5f;

    private Coroutine transitionRoutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionLight(targetIntensity, targetColor));
    }

    private IEnumerator TransitionLight(float toIntensity, Color toColor)
    {
        float fromIntensity = globalLight.intensity;
        Color fromColor = globalLight.color;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            globalLight.intensity = Mathf.Lerp(fromIntensity, toIntensity, t);
            globalLight.color = Color.Lerp(fromColor, toColor, t);

            yield return null;
        }

        globalLight.intensity = toIntensity;
        globalLight.color = toColor;
    }
}