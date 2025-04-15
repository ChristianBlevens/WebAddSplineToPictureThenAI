using UnityEngine;
using System.Collections.Generic;

public class ChristmasLightGenerator : MonoBehaviour
{
    private GameObject lightPrefab;
    private float lightSpacing = 20f;
    private float lightScale = 1f;
    private bool animateLights = true;
    private float animationSpeed = 1f;
    private Color[] lightColors = new Color[0];

    private List<GameObject> generatedLights = new List<GameObject>();

    public void SetSettings(
        GameObject prefab,
        float spacing,
        float scale,
        bool animate,
        float speed,
        Color[] colors)
    {
        lightPrefab = prefab;
        lightSpacing = spacing;
        lightScale = scale;
        animateLights = animate;
        animationSpeed = speed;
        lightColors = colors;
    }

    public void CreateLightsForSpline(ChristmasLightSpline spline)
    {
        if (lightPrefab == null || spline == null || spline.TotalLength <= 0)
            return;

        // Calculate how many lights we need
        int lightCount = Mathf.Max(2, Mathf.FloorToInt(spline.TotalLength / lightSpacing));

        // Create parent object for organization
        GameObject lightsParent = new GameObject("Lights_" + spline.gameObject.name);
        lightsParent.transform.SetParent(transform);

        // Create lights
        for (int i = 0; i < lightCount; i++)
        {
            float normalizedDistance = (float)i / (lightCount - 1);
            Vector3 position = spline.GetPositionAtNormalizedDistance(normalizedDistance);

            // Create light game object
            GameObject light = Instantiate(lightPrefab, position, Quaternion.identity, lightsParent.transform);
            light.name = "Light_" + i;
            light.transform.localScale = Vector3.one * lightScale;

            // Set color
            if (lightColors.Length > 0)
            {
                int colorIndex = i % lightColors.Length;
                Renderer renderer = light.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = lightColors[colorIndex];
                }

                // If it has a light component, set that color too
                Light lightComponent = light.GetComponent<Light>();
                if (lightComponent != null)
                {
                    lightComponent.color = lightColors[colorIndex];
                }
            }

            // Add animation component if needed
            if (animateLights)
            {
                ChristmasLightAnimation animator = light.AddComponent<ChristmasLightAnimation>();
                animator.SetAnimationProperties(animationSpeed, i * 0.1f); // offset start time
            }

            generatedLights.Add(light);
        }
    }

    public void ClearAllLights()
    {
        foreach (GameObject light in generatedLights)
        {
            if (light != null)
            {
                Destroy(light);
            }
        }

        generatedLights.Clear();

        // Also destroy any parent objects
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}