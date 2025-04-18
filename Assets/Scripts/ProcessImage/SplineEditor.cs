using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SplineEditor : MonoBehaviour
{
    private RawImage targetImage;
    private RectTransform splineLayerRect;
    private ChristmasLightGenerator lightGenerator;
    private Material splineMaterial;
    private Color splineColor;
    private float splineWidth;
    private float snapDistance = 20f;

    private Texture2D lineTexture; // Texture containing line data with depth values
    private List<Vector2> controlPoints = new List<Vector2>();
    private List<ChristmasLightSpline> splines = new List<ChristmasLightSpline>();

    private Vector2 startPoint;
    private Vector2 currentPoint;
    private bool isDrawing = false;
    private RectTransform targetRect;
    private Camera uiCamera;

    private PlayerInput playerInput;
    private InputAction interactAction;

    public void Initialize(
        RawImage image,
        Texture2D lineImage,
        RectTransform splineLayer,
        ChristmasLightGenerator lightGen,
        Material material,
        Color color,
        float width,
        PlayerInput input)
    {
        targetImage = image;
        lineTexture = lineImage;
        splineLayerRect = splineLayer;
        lightGenerator = lightGen;
        splineMaterial = material;
        splineColor = color;
        splineWidth = width;
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
            uiCamera = FindFirstObjectByType<Camera>();
        }

        // Setup input actions
        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];

            if (interactAction != null)
            {
                interactAction.started += ctx => OnInteractStarted(ctx);
                interactAction.performed += ctx => OnInteractPerformed(ctx);
                interactAction.canceled += ctx => OnInteractCanceled(ctx);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (interactAction != null)
        {
            interactAction.started -= ctx => OnInteractStarted(ctx);
            interactAction.performed -= ctx => OnInteractPerformed(ctx);
            interactAction.canceled -= ctx => OnInteractCanceled(ctx);
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

    // Public API methods
    public void SetLineTexture(Texture2D texture)
    {
        lineTexture = texture;
    }

    public void SetSnapDistance(float snapDist)
    {
        snapDistance = snapDist;
    }

    // Spline creation methods
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
        if (lineTexture == null) return 0;

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
            // Assuming depth is stored in the red channel from 0-255
            return pixelColor.r;
        }

        // If not, search neighboring pixels for the closest line
        int searchRadius = Mathf.CeilToInt(snapDistance);
        float minDistance = snapDistance * snapDistance;
        float closestDepth = 0;
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
}