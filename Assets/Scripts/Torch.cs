using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class TorchFlicker : MonoBehaviour
{
    private Light2D light2D;
    private Vector3 startLocalPos;

    [Header("Intensity Flicker")]
    public float baseIntensity = 2.5f;
    public float intensityVariance = 0.6f;
    public float flickerSpeed = 4f;

    [Header("Color Flicker (tùy chọn)")]
    public bool flickerColor = true;
    public Color colorA = new Color(1f, 0.62f, 0.3f);   // cam sáng
    public Color colorB = new Color(1f, 0.45f, 0.15f);  // cam đậm hơn

    [Header("Position Jitter (tùy chọn, rất nhỏ)")]
    public bool jitterPosition = true;
    public float jitterAmount = 0.02f;

    private float seed;

    void Start()
    {
        light2D = GetComponent<Light2D>();
        startLocalPos = transform.localPosition;
        seed = Random.Range(0f, 1000f); // mỗi đuốc lệch pha, không nhấp nháy đồng bộ
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
        light2D.intensity = baseIntensity + (noise - 0.5f) * 2f * intensityVariance;

        if (flickerColor)
        {
            float colorNoise = Mathf.PerlinNoise(seed + 50f, Time.time * flickerSpeed * 0.7f);
            light2D.color = Color.Lerp(colorA, colorB, colorNoise);
        }

        if (jitterPosition)
        {
            float jx = (Mathf.PerlinNoise(seed + 100f, Time.time * flickerSpeed) - 0.5f) * jitterAmount;
            float jy = (Mathf.PerlinNoise(seed + 200f, Time.time * flickerSpeed) - 0.5f) * jitterAmount;
            transform.localPosition = startLocalPos + new Vector3(jx, jy, 0f);
        }
    }
}