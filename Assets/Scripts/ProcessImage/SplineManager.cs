using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SplineManager
{
    private RawImage targetImage;
    private RectTransform splineLayerRect;
    private Material splineMaterial;
    private Color splineColor;
    private float splineWidth;
    private float snapDistance;
    private LightManager lightManager;

    private Texture2D lineTexture;
    private List<ChristmasLightSpline> splines = new List<ChristmasLightSpline>();

    private List<Vector2> controlPoints = new List<Vector2>();
    private Vector2 startPoint;
    private Vector2 currentPoint;
    private bool isDrawing = false;
    private RectTransform targetRect;
    private Camera uiCamera;

    private PlayerInput playerInput;
    private InputAction interactAction;

    public SplineManager(
        RawImage image,
        RectTransform splineLayer,
        Material material,
        Color color,
        float width,
        float snapDist,
        PlayerInput input,
        LightManager lightGen)
    {
        targetImage = image;
        splineLayerRect = splineLayer;
        splineMaterial = material;
        splineColor = color;
        splineWidth = width;
        snapDistance = snapDist;
        lightManager = lightGen;
        playerInput = input;

        // Get target rect
        if (targetImage != null)
        {
            targetRect = targetImage.GetComponent<RectTransform>();
        }

        // Find the UI camera
        uiCamera = Camera.main;
        if (uiCamera == null || !uiCamera.gameObject.CompareTag("MainCamera"))
        {
            uiCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        }

        // Setup input actions
        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];

            if (interactAction != null)
            {
                interactAction.started += OnInteractStarted;
                interactAction.performed += OnInteractPerformed;
                interactAction.canceled += OnInteractCanceled;
            }
        }
    }

    // Cleanup method to be called when the manager is destroyed
    public void Cleanup()
    {
        if (interactAction != null)
        {
            interactAction.started -= OnInteractStarted;
            interactAction.performed -= OnInteractPerformed;
            interactAction.canceled -= OnInteractCanceled;
        }
    }

    private void OnInteractStarted(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Check if we're clicking on the target image
        if (IsPointerOverImage(mousePosition))
        {
            StartDrawing(mousePosition);
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (isDrawing)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            UpdateDrawing(mousePosition);
        }
    }

    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        if (isDrawing)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            EndDrawing(mousePosition);
        }
    }

    private bool IsPointerOverImage(Vector2 screenPosition)
    {
        if (targetRect == null) return false;

        // Check if pointer is over the image
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect, screenPosition, uiCamera, out Vector2 localPoint);

        return targetRect.rect.Contains(localPoint);
    }

    private void StartDrawing(Vector2 screenPosition)
    {
        if (targetRect == null || lineTexture == null) return;

        // Convert screen position to local position in the image
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect, screenPosition, uiCamera, out startPoint);

        currentPoint = startPoint;
        controlPoints.Clear();

        // Try to snap to nearest edge
        Vector2 snappedPoint = startPoint;
        SnapToNearestEdge(ref snappedPoint);
        startPoint = snappedPoint;

        controlPoints.Add(startPoint);
        isDrawing = true;
    }

    private void UpdateDrawing(Vector2 screenPosition)
    {
        if (!isDrawing || targetRect == null) return;

        // Convert screen position to local position in the image
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect, screenPosition, uiCamera, out currentPoint);
    }

    private void EndDrawing(Vector2 screenPosition)
    {
        if (!isDrawing || targetRect == null || lineTexture == null) return;

        // Convert screen position to local position in the image
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect, screenPosition, uiCamera, out Vector2 endPoint);

        // Try to snap end point to nearest edge
        SnapToNearestEdge(ref endPoint);

        // Add final control point
        controlPoints.Add(endPoint);

        // Create the spline
        CreateSpline();

        isDrawing = false;
    }

    public void SetLineTexture(Texture2D texture)
    {
        lineTexture = texture;
    }

    public void SetSnapDistance(float snapDist)
    {
        snapDistance = snapDist;
    }

    private void SnapToNearestEdge(ref Vector2 point)
    {
        if (lineTexture == null) return;

        // Convert UI coordinate to texture coordinate
        Vector2 texturePoint = ConvertToTextureSpace(point);
        int texX = Mathf.RoundToInt(texturePoint.x);
        int texY = Mathf.RoundToInt(texturePoint.y);

        // Search in a square area around the point for the nearest line pixel
        int searchRadius = Mathf.CeilToInt(snapDistance);
        float minDistance = snapDistance * snapDistance; // Square the distance for comparison
        Vector2 closestPoint = point;
        bool foundLine = false;

        for (int y = -searchRadius; y <= searchRadius; y++)
        {
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                int sampleX = texX + x;
                int sampleY = texY + y;

                // Check if coordinates are within texture bounds
                if (sampleX >= 0 && sampleX < lineTexture.width &&
                    sampleY >= 0 && sampleY < lineTexture.height)
                {
                    Color pixelColor = lineTexture.GetPixel(sampleX, sampleY);

                    // If alpha > 0, this is a line pixel
                    if (pixelColor.a > 0)
                    {
                        float sqrDist = x * x + y * y;
                        if (sqrDist < minDistance)
                        {
                            minDistance = sqrDist;
                            closestPoint = ConvertToUISpace(new Vector2(sampleX, sampleY));
                            foundLine = true;
                        }
                    }
                }
            }
        }

        if (foundLine)
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

        // Get depth information
        float startDepth = GetEstimatedDepthAtPoint(controlPoints[0]);
        float endDepth = GetEstimatedDepthAtPoint(controlPoints[controlPoints.Count - 1]);

        // Initialize spline with all parameters
        spline.Initialize(controlPoints, splineMaterial, splineColor, splineWidth, startDepth, endDepth);

        // Create lights for this spline
        lightManager.CreateLightsForSpline(spline);

        // Add to list of splines
        splines.Add(spline);
    }

    private float GetEstimatedDepthAtPoint(Vector2 point)
    {
        if (lineTexture == null) return 0.5f;

        // Convert UI coordinate to texture coordinate
        Vector2 texturePoint = ConvertToTextureSpace(point);
        int texX = Mathf.RoundToInt(texturePoint.x);
        int texY = Mathf.RoundToInt(texturePoint.y);

        // Clamp to texture bounds
        texX = Mathf.Clamp(texX, 0, lineTexture.width - 1);
        texY = Mathf.Clamp(texY, 0, lineTexture.height - 1);

        // Sample the line texture for depth information
        Color pixelColor = lineTexture.GetPixel(texX, texY);

        // If this exact pixel is a line, use its value
        if (pixelColor.a > 0)
        {
            // Assuming depth is stored in the red channel from 0-1
            return pixelColor.r;
        }

        // If not, search neighboring pixels for the closest line
        int searchRadius = Mathf.CeilToInt(snapDistance);
        float minDistance = snapDistance * snapDistance;
        float closestDepth = 0.5f;
        bool foundLine = false;

        for (int y = -searchRadius; y <= searchRadius; y++)
        {
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                int sampleX = texX + x;
                int sampleY = texY + y;

                // Check if coordinates are within texture bounds
                if (sampleX >= 0 && sampleX < lineTexture.width &&
                    sampleY >= 0 && sampleY < lineTexture.height)
                {
                    Color sampleColor = lineTexture.GetPixel(sampleX, sampleY);

                    // If alpha > 0, this is a line pixel
                    if (sampleColor.a > 0)
                    {
                        float sqrDist = x * x + y * y;
                        if (sqrDist < minDistance)
                        {
                            minDistance = sqrDist;
                            closestDepth = sampleColor.r;
                            foundLine = true;
                        }
                    }
                }
            }
        }

        if (foundLine)
        {
            return closestDepth;
        }

        // Fallback: use vertical position as depth cue
        return point.y / targetRect.rect.height;
    }

    // Coordinate conversion helpers
    private Vector2 ConvertToUISpace(Vector2 texturePos)
    {
        if (targetRect == null || targetImage.texture == null) return texturePos;

        // Convert from texture space to UI space
        return new Vector2(
            texturePos.x / targetImage.texture.width * targetRect.rect.width,
            texturePos.y / targetImage.texture.height * targetRect.rect.height
        );
    }

    private Vector2 ConvertToTextureSpace(Vector2 uiPos)
    {
        if (targetRect == null || targetImage.texture == null) return uiPos;

        // Convert from UI space to texture space
        return new Vector2(
            uiPos.x / targetRect.rect.width * targetImage.texture.width,
            uiPos.y / targetRect.rect.height * targetImage.texture.height
        );
    }

    public void RemoveLastSpline()
    {
        if (splines.Count > 0)
        {
            int lastIndex = splines.Count - 1;
            ChristmasLightSpline lastSpline = splines[lastIndex];

            // Remove the lights associated with this spline
            lightManager.RemoveLightsForSpline(lastSpline.gameObject.name);

            // Destroy the GameObject
            UnityEngine.Object.Destroy(lastSpline.gameObject);

            // Remove from list
            splines.RemoveAt(lastIndex);
        }
    }

    public void ClearAllSplines()
    {
        foreach (ChristmasLightSpline spline in splines)
        {
            if (spline != null && spline.gameObject != null)
            {
                UnityEngine.Object.Destroy(spline.gameObject);
            }
        }

        splines.Clear();
    }
}