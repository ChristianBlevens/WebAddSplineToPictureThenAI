using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SplineEditor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private RawImage targetImage;
    private RectTransform splineLayerRect;
    private ChristmasLightGenerator lightGenerator;
    private Material splineMaterial;
    private Color splineColor;
    private float splineWidth;

    private List<EdgeDetectionUtility.ImageLine> detectedLines = new List<EdgeDetectionUtility.ImageLine>();
    private List<Vector2> controlPoints = new List<Vector2>();
    private List<ChristmasLightSpline> splines = new List<ChristmasLightSpline>();

    private Vector2 startPoint;
    private Vector2 currentPoint;
    private bool isDrawing = false;
    private float snapDistance = 20f;
    private RectTransform targetRect;

    public void Initialize(
        RawImage image,
        RectTransform splineLayer,
        ChristmasLightGenerator lightGen,
        Material material,
        Color color,
        float width)
    {
        targetImage = image;
        splineLayerRect = splineLayer;
        lightGenerator = lightGen;
        splineMaterial = material;
        splineColor = color;
        splineWidth = width;

        if (targetImage != null)
        {
            targetRect = targetImage.GetComponent<RectTransform>();
        }
    }

    public void SetEdgeLines(List<EdgeDetectionUtility.ImageLine> lines, float snapDist)
    {
        detectedLines = lines;
        snapDistance = snapDist;

        // Convert from texture space to UI space
        if (targetRect != null)
        {
            for (int i = 0; i < detectedLines.Count; i++)
            {
                EdgeDetectionUtility.ImageLine line = detectedLines[i];

                // Adjust coordinates to match UI space
                Vector2 start = ConvertToUISpace(line.Start);
                Vector2 end = ConvertToUISpace(line.End);

                detectedLines[i] = new EdgeDetectionUtility.ImageLine(start, end);
                detectedLines[i].EstimatedDepth = line.EstimatedDepth;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetRect == null) return;

        // Convert screen position to local position in the image
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect, eventData.position, eventData.pressEventCamera, out startPoint);

        currentPoint = startPoint;
        controlPoints.Clear();
        controlPoints.Add(startPoint);

        isDrawing = true;

        // Try to snap to nearest edge
        SnapToNearestEdge(ref startPoint);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing || targetRect == null) return;

        // Convert screen position to local position in the image
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect, eventData.position, eventData.pressEventCamera, out currentPoint);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDrawing || targetRect == null) return;

        // Convert screen position to local position in the image
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect, eventData.position, eventData.pressEventCamera, out Vector2 endPoint);

        // Try to snap end point to nearest edge
        SnapToNearestEdge(ref endPoint);

        // Add final control point
        controlPoints.Add(endPoint);

        // Create the spline
        CreateSpline();

        isDrawing = false;
    }

    private void SnapToNearestEdge(ref Vector2 point)
    {
        EdgeDetectionUtility.ImageLine closestLine = null;
        float minDistance = snapDistance;
        Vector2 closestPoint = point;

        foreach (EdgeDetectionUtility.ImageLine line in detectedLines)
        {
            float distance = line.DistanceToPoint(point);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestLine = line;
                closestPoint = line.ProjectPointOntoLine(point);
            }
        }

        if (closestLine != null)
        {
            point = closestPoint;
        }
    }

    private void CreateSpline()
    {
        if (controlPoints.Count < 2) return;

        // Create a new GameObject for the spline
        GameObject splineObj = new GameObject("ChristmasLightSpline");
        splineObj.transform.SetParent(splineLayerRect);

        // Add the spline component
        ChristmasLightSpline spline = splineObj.AddComponent<ChristmasLightSpline>();
        spline.Initialize(controlPoints, splineMaterial, splineColor, splineWidth);

        // Add depth information
        ApplyDepthToSpline(spline);

        // Initialize lights
        lightGenerator.CreateLightsForSpline(spline);

        // Add to list of splines
        splines.Add(spline);
    }

    private void ApplyDepthToSpline(ChristmasLightSpline spline)
    {
        // Extract depth information from start and end points
        Vector2 startPoint = controlPoints[0];
        Vector2 endPoint = controlPoints[controlPoints.Count - 1];

        float startDepth = GetEstimatedDepthAtPoint(startPoint);
        float endDepth = GetEstimatedDepthAtPoint(endPoint);

        // Pass depth information to the spline
        spline.SetDepthGradient(startDepth, endDepth);
    }

    private float GetEstimatedDepthAtPoint(Vector2 point)
    {
        // Find the closest edge to this point
        EdgeDetectionUtility.ImageLine closestLine = null;
        float minDistance = float.MaxValue;

        foreach (EdgeDetectionUtility.ImageLine line in detectedLines)
        {
            float distance = line.DistanceToPoint(point);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestLine = line;
            }
        }

        // Use the depth of the closest edge
        if (closestLine != null)
        {
            return closestLine.EstimatedDepth;
        }

        // Fallback: use vertical position as depth cue
        return point.y / targetRect.rect.height;
    }

    private Vector2 ConvertToUISpace(Vector2 texturePos)
    {
        if (targetRect == null) return texturePos;

        // Convert from texture space to UI space
        return new Vector2(
            texturePos.x / ((Texture2D)targetImage.texture).width * targetRect.rect.width,
            texturePos.y / ((Texture2D)targetImage.texture).height * targetRect.rect.height
        );
    }
}