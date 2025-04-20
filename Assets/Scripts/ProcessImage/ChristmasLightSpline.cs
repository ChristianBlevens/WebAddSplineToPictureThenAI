using UnityEngine;
using System.Collections.Generic;

public class ChristmasLightSpline : MonoBehaviour
{
    private List<Vector2> controlPoints = new List<Vector2>();
    private LineRenderer lineRenderer;
    private float startDepth = 0.5f;
    private float endDepth = 0.5f;
    private float totalLength;

    public List<Vector2> ControlPoints => controlPoints;
    public float TotalLength => totalLength;

    public void Initialize(List<Vector2> points, Material material, Color color, float width, float startDepthValue, float endDepthValue)
    {
        controlPoints = new List<Vector2>(points);
        startDepth = startDepthValue;
        endDepth = endDepthValue;

        CreateLineRenderer(material, color, width);
        CalculateLength();
    }

    private void CreateLineRenderer(Material material, Color color, float width)
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = material;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.positionCount = controlPoints.Count;

        UpdateSplineVisuals();
    }

    private void UpdateSplineVisuals()
    {
        if (lineRenderer == null || controlPoints.Count == 0)
            return;

        // Set positions
        for (int i = 0; i < controlPoints.Count; i++)
        {
            // Calculate z position based on depth gradient
            float t = (float)i / (controlPoints.Count > 1 ? controlPoints.Count - 1 : 1);
            float depth = Mathf.Lerp(startDepth, endDepth, t);

            Vector3 position = new Vector3(controlPoints[i].x, controlPoints[i].y, depth * 5f); // Multiply by 5 for visual effect
            lineRenderer.SetPosition(i, position);
        }
    }

    private void CalculateLength()
    {
        totalLength = 0f;

        for (int i = 1; i < controlPoints.Count; i++)
        {
            totalLength += Vector2.Distance(controlPoints[i - 1], controlPoints[i]);
        }
    }

    // Get a position at normalized distance (0-1) along the spline
    public Vector3 GetPositionAtNormalizedDistance(float normalizedDistance)
    {
        if (controlPoints.Count < 2)
            return Vector3.zero;

        // Clamp input
        normalizedDistance = Mathf.Clamp01(normalizedDistance);

        // Find which segment this falls on
        float targetDistance = normalizedDistance * totalLength;
        float currentDistance = 0f;

        for (int i = 1; i < controlPoints.Count; i++)
        {
            float segmentLength = Vector2.Distance(controlPoints[i - 1], controlPoints[i]);

            if (currentDistance + segmentLength >= targetDistance)
            {
                // We found the segment
                float segmentT = (targetDistance - currentDistance) / segmentLength;
                Vector2 position = Vector2.Lerp(controlPoints[i - 1], controlPoints[i], segmentT);

                // Calculate depth at this position
                float depthT = Mathf.Lerp((float)(i - 1) / (controlPoints.Count - 1),
                                         (float)i / (controlPoints.Count - 1),
                                         segmentT);
                float depth = Mathf.Lerp(startDepth, endDepth, depthT);

                return new Vector3(position.x, position.y, depth * 5f);
            }

            currentDistance += segmentLength;
        }

        // Fallback to last point
        int last = controlPoints.Count - 1;
        return new Vector3(controlPoints[last].x, controlPoints[last].y, endDepth * 5f);
    }
}