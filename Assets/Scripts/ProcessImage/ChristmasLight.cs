using UnityEngine;

// This is a non-MonoBehaviour class to manage individual light behavior
public class ChristmasLight
{
    private GameObject lightObject;
    private float animationSpeed = 1f;
    private float timeOffset = 0f;
    private bool isAnimated = true;

    private Light lightComponent;
    private Renderer rendererComponent;
    private float originalIntensity = 1f;
    private Vector3 originalScale;
    private Color originalEmissionColor;

    public GameObject GameObject => lightObject;

    public ChristmasLight(GameObject lightObj, float speed, float offset)
    {
        lightObject = lightObj;
        animationSpeed = speed;
        timeOffset = offset;

        // Get components
        lightComponent = lightObj.GetComponent<Light>();
        rendererComponent = lightObj.GetComponent<Renderer>();

        // Store original values
        if (lightComponent != null)
        {
            originalIntensity = lightComponent.intensity;
        }

        originalScale = lightObj.transform.localScale;

        if (rendererComponent != null && rendererComponent.material.HasProperty("_EmissionColor"))
        {
            originalEmissionColor = rendererComponent.material.GetColor("_EmissionColor");
        }

        // Add animation updater component to the object
        LightAnimationUpdater updater = lightObj.AddComponent<LightAnimationUpdater>();
        updater.Initialize(this);
    }

    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }

    public void SetAnimationEnabled(bool enabled)
    {
        isAnimated = enabled;

        if (!isAnimated)
        {
            // Reset to original values
            ResetToOriginalValues();
        }
    }

    private void ResetToOriginalValues()
    {
        if (lightComponent != null)
        {
            lightComponent.intensity = originalIntensity;
        }

        if (rendererComponent != null && rendererComponent.material.HasProperty("_EmissionColor"))
        {
            rendererComponent.material.SetColor("_EmissionColor", originalEmissionColor);
        }

        if (lightObject != null)
        {
            lightObject.transform.localScale = originalScale;
        }
    }

    public void UpdateAnimation(float time)
    {
        if (!isAnimated) return;

        // Simple pulsating animation
        float pulse = 0.7f + 0.3f * Mathf.Sin((time + timeOffset) * animationSpeed * 3f);

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
        if (lightObject != null)
        {
            lightObject.transform.localScale = originalScale * (0.9f + 0.1f * pulse);
        }
    }
}