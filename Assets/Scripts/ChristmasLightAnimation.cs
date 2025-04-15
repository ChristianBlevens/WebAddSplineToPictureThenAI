using UnityEngine;

public class ChristmasLightAnimation : MonoBehaviour
{
    private float animationSpeed = 1f;
    public float timeOffset = 0f;
    private float originalIntensity = 1f;
    private Vector3 originalScale;

    private Light lightComponent;
    private Renderer rendererComponent;

    private void Awake()
    {
        lightComponent = GetComponent<Light>();
        rendererComponent = GetComponent<Renderer>();

        if (lightComponent != null)
        {
            originalIntensity = lightComponent.intensity;
        }

        originalScale = transform.localScale;
    }

    public void SetAnimationProperties(float speed, float offset)
    {
        animationSpeed = speed;
        timeOffset = offset;
    }

    private void Update()
    {
        // Simple pulsating animation
        float pulse = 0.7f + 0.3f * Mathf.Sin((Time.time + timeOffset) * animationSpeed * 3f);

        // Apply to light intensity
        if (lightComponent != null)
        {
            lightComponent.intensity = originalIntensity * pulse;
        }

        // Apply to renderer emission
        if (rendererComponent != null && rendererComponent.material.HasProperty("_EmissionColor"))
        {
            Color baseColor = rendererComponent.material.color;
            rendererComponent.material.SetColor("_EmissionColor", baseColor * pulse);
        }

        // Apply subtle scale change
        transform.localScale = originalScale * (0.9f + 0.1f * pulse);
    }
}