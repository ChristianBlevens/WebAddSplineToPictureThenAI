using UnityEngine;
using System.Collections.Generic;

public class LightManager
{
    private GameObject lightPrefab;
    private float lightSpacing = 20f;
    private float lightScale = 1f;
    private bool animateLights = true;
    private float animationSpeed = 1f;
    private Color[] lightColors = new Color[0];

    private Dictionary<string, List<ChristmasLight>> lightsBySpline = new Dictionary<string, List<ChristmasLight>>();

    public LightManager(
        GameObject prefab,
        float spacing,
        float scale,
        float speed,
        Color[] colors)
    {
        lightPrefab = prefab;
        lightSpacing = spacing;
        lightScale = scale;
        animationSpeed = speed;
        lightColors = colors;
    }

    public void SetLightSpacing(float spacing)
    {
        lightSpacing = spacing;
    }

    public void SetLightScale(float scale)
    {
        lightScale = scale;
    }

    public void SetAnimationEnabled(bool enabled)
    {
        animateLights = enabled;

        // Update all existing lights
        foreach (var splineLights in lightsBySpline.Values)
        {
            foreach (var light in splineLights)
            {
                light.SetAnimationEnabled(enabled);
            }
        }
    }

    public void UpdateAnimationSpeed(float speed)
    {
        animationSpeed = speed;

        // Update all existing lights
        foreach (var splineLights in lightsBySpline.Values)
        {
            foreach (var light in splineLights)
            {
                light.SetAnimationSpeed(speed);
            }
        }
    }

    public void CreateLightsForSpline(ChristmasLightSpline spline)
    {
        if (lightPrefab == null || spline == null || spline.TotalLength <= 0)
            return;

        // Calculate how many lights we need
        int lightCount = Mathf.Max(2, Mathf.FloorToInt(spline.TotalLength / lightSpacing));

        // Create parent object for organization
        GameObject lightsParent = new GameObject("Lights_" + spline.gameObject.name);

        // Create list to store light references
        List<ChristmasLight> splineLights = new List<ChristmasLight>();

        // Create lights
        for (int i = 0; i < lightCount; i++)
        {
            float normalizedDistance = (float)i / (lightCount - 1);
            Vector3 position = spline.GetPositionAtNormalizedDistance(normalizedDistance);

            // Create light game object
            GameObject lightObj = Object.Instantiate(lightPrefab, position, Quaternion.identity, lightsParent.transform);
            lightObj.name = "Light_" + i;
            lightObj.transform.localScale = Vector3.one * lightScale;

            // Set color
            if (lightColors.Length > 0)
            {
                int colorIndex = i % lightColors.Length;
                Renderer renderer = lightObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = lightColors[colorIndex];
                }

                Light lightComponent = lightObj.GetComponent<Light>();
                if (lightComponent != null)
                {
                    lightComponent.color = lightColors[colorIndex];
                }
            }

            // Create and add the lightweight light controller
            ChristmasLight light = new ChristmasLight(lightObj, animationSpeed, i * 0.1f);
            light.SetAnimationEnabled(animateLights);
            splineLights.Add(light);
        }

        // Store reference to these lights
        lightsBySpline[spline.gameObject.name] = splineLights;
    }

    public void RemoveLightsForSpline(string splineName)
    {
        if (lightsBySpline.TryGetValue(splineName, out List<ChristmasLight> lights))
        {
            // Find parent object
            string parentName = "Lights_" + splineName;
            GameObject parent = GameObject.Find(parentName);

            // Destroy the parent (will destroy all children)
            if (parent != null)
            {
                Object.Destroy(parent);
            }

            // Remove from dictionary
            lightsBySpline.Remove(splineName);
        }
    }

    public void ClearAllLights()
    {
        // Find all light parent objects and destroy them
        foreach (var splineLights in lightsBySpline.Values)
        {
            foreach (var light in splineLights)
            {
                if (light.GameObject != null)
                {
                    Object.Destroy(light.GameObject);
                }
            }
        }

        // Clear the dictionary
        lightsBySpline.Clear();
    }
}