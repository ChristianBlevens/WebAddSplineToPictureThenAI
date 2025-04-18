using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EdgeDetectionUtility
{
    // Parameters for edge detection
    private float sobelThreshold = 0.1f;
    private int minLineLength = 20;
    private float maxLineGap = 5f;
    private float houghThreshold = 10f;

    // Sobel operator kernels
    private static readonly int[,] SobelX = new int[,] {
        { -1, 0, 1 },
        { -2, 0, 2 },
        { -1, 0, 1 }
    };

    private static readonly int[,] SobelY = new int[,] {
        { -1, -2, -1 },
        {  0,  0,  0 },
        {  1,  2,  1 }
    };

    public void SetThreshold(float threshold)
    {
        sobelThreshold = threshold;
    }

    // Main method to process an image and return a texture with lines
    public Texture2D ProcessImageToLineTexture(Texture2D sourceImage)
    {
        if (sourceImage == null) return null;

        // Step 1: Detect edges
        Texture2D edgeTexture = DetectEdges(sourceImage);

        // Step 2: Create transparent texture for output
        Texture2D lineTexture = new Texture2D(sourceImage.width, sourceImage.height, TextureFormat.RGBA32, false);
        Color[] transparentPixels = new Color[sourceImage.width * sourceImage.height];
        for (int i = 0; i < transparentPixels.Length; i++)
        {
            transparentPixels[i] = new Color(0, 0, 0, 0); // Fully transparent
        }
        lineTexture.SetPixels(transparentPixels);

        // Step 3: Detect and draw lines directly to texture
        DrawLinesDirectlyToTexture(edgeTexture, lineTexture);

        lineTexture.Apply();
        return lineTexture;
    }

    // Apply edge detection to a texture
    private Texture2D DetectEdges(Texture2D source)
    {
        if (source == null) return null;

        // Create output texture
        Texture2D edgeTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        Color[] pixelColors = new Color[source.width * source.height];

        // Apply Sobel operator for edge detection
        for (int y = 1; y < source.height - 1; y++)
        {
            for (int x = 1; x < source.width - 1; x++)
            {
                float gx = 0;
                float gy = 0;

                // Apply convolution kernel
                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        Color pixel = source.GetPixel(x + kx, y + ky);
                        float grayscale = pixel.grayscale;

                        gx += grayscale * SobelX[ky + 1, kx + 1];
                        gy += grayscale * SobelY[ky + 1, kx + 1];
                    }
                }

                // Calculate gradient magnitude
                float magnitude = Mathf.Sqrt(gx * gx + gy * gy);

                // Apply threshold
                if (magnitude > sobelThreshold)
                {
                    pixelColors[y * source.width + x] = Color.white;
                }
                else
                {
                    pixelColors[y * source.width + x] = Color.black;
                }
            }
        }

        edgeTexture.SetPixels(pixelColors);
        edgeTexture.Apply();
        return edgeTexture;
    }

    // Detect and draw lines directly to output texture without storing ImageLine objects
    private void DrawLinesDirectlyToTexture(Texture2D edgeTexture, Texture2D outputTexture)
    {
        if (edgeTexture == null) return;

        // Collect edge points
        List<Vector2> edgePoints = new List<Vector2>();
        for (int y = 0; y < edgeTexture.height; y++)
        {
            for (int x = 0; x < edgeTexture.width; x++)
            {
                if (edgeTexture.GetPixel(x, y).grayscale > 0.5f)
                {
                    edgePoints.Add(new Vector2(x, y));
                }
            }
        }

        // Define parameter space for Hough transform
        int thetaResolution = 180; // 1-degree resolution
        float rhoResolution = 1.0f;
        float maxRho = Mathf.Sqrt(edgeTexture.width * edgeTexture.width + edgeTexture.height * edgeTexture.height);
        int rhoSteps = Mathf.CeilToInt(2 * maxRho / rhoResolution);

        // Create accumulator
        int[,] accumulator = new int[thetaResolution, rhoSteps];

        // Fill accumulator
        foreach (Vector2 point in edgePoints)
        {
            for (int thetaIndex = 0; thetaIndex < thetaResolution; thetaIndex++)
            {
                float theta = thetaIndex * Mathf.PI / thetaResolution;
                float rho = point.x * Mathf.Cos(theta) + point.y * Mathf.Sin(theta);

                // Convert rho to index
                int rhoIndex = Mathf.RoundToInt((rho + maxRho) / rhoResolution);

                // Ensure within bounds
                if (rhoIndex >= 0 && rhoIndex < rhoSteps)
                {
                    accumulator[thetaIndex, rhoIndex]++;
                }
            }
        }

        // List to store temporary line data for merging
        List<LineData> lineDataList = new List<LineData>();

        // Find peaks in the accumulator
        for (int thetaIndex = 0; thetaIndex < thetaResolution; thetaIndex++)
        {
            for (int rhoIndex = 0; rhoIndex < rhoSteps; rhoIndex++)
            {
                if (accumulator[thetaIndex, rhoIndex] > houghThreshold)
                {
                    // Convert back to theta and rho
                    float theta = thetaIndex * Mathf.PI / thetaResolution;
                    float rho = (rhoIndex * rhoResolution) - maxRho;

                    // Convert polar to cartesian form to get line segments
                    Vector2 start, end;

                    // Choose two points on the line to create a line segment
                    if (Mathf.Abs(Mathf.Sin(theta)) < 0.001f)
                    {
                        // Vertical line
                        start = new Vector2(rho / Mathf.Cos(theta), 0);
                        end = new Vector2(rho / Mathf.Cos(theta), edgeTexture.height - 1);
                    }
                    else
                    {
                        // Non-vertical line
                        start = new Vector2(0, rho / Mathf.Sin(theta));
                        end = new Vector2(edgeTexture.width - 1, (rho - (edgeTexture.width - 1) * Mathf.Cos(theta)) / Mathf.Sin(theta));
                    }

                    // Clip line to image boundaries
                    start.x = Mathf.Clamp(start.x, 0, edgeTexture.width - 1);
                    start.y = Mathf.Clamp(start.y, 0, edgeTexture.height - 1);
                    end.x = Mathf.Clamp(end.x, 0, edgeTexture.width - 1);
                    end.y = Mathf.Clamp(end.y, 0, edgeTexture.height - 1);

                    // Only add lines that are long enough
                    float lineLength = Vector2.Distance(start, end);
                    if (lineLength >= minLineLength)
                    {
                        // Calculate angle
                        Vector2 direction = end - start;
                        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                        // Store temporary line data
                        lineDataList.Add(new LineData(start, end, angle, lineLength));
                    }

                    // Reset this peak to avoid detecting the same line multiple times
                    accumulator[thetaIndex, rhoIndex] = 0;
                }
            }
        }

        // Merge similar lines
        List<LineData> mergedLines = MergeLines(lineDataList);

        // Calculate depth for each line
        EstimateDepthForLines(mergedLines, edgeTexture.height);

        // Draw lines directly to output texture with depth as black shade
        foreach (LineData line in mergedLines)
        {
            // Convert depth (0-1) to color intensity (0-255)
            // 0 depth = black (0), 1 depth = lighter gray (100)
            byte colorValue = (byte)Mathf.Clamp(line.depth * 100, 0, 255);
            Color lineColor = new Color32(colorValue, colorValue, colorValue, 255); // Full alpha

            // Draw the line
            DrawLine(outputTexture, (int)line.start.x, (int)line.start.y,
                    (int)line.end.x, (int)line.end.y, lineColor);
        }
    }

    // Temporary structure to hold line data for merging
    private struct LineData
    {
        public Vector2 start;
        public Vector2 end;
        public float angle;
        public float length;
        public float depth;

        public LineData(Vector2 start, Vector2 end, float angle, float length)
        {
            this.start = start;
            this.end = end;
            this.angle = angle;
            this.length = length;
            this.depth = 0.5f; // Default depth
        }

        // Calculate distance from line to point
        public float DistanceToPoint(Vector2 point)
        {
            // Line equation: ax + by + c = 0
            float a = end.y - start.y;
            float b = start.x - end.x;
            float c = end.x * start.y - start.x * end.y;

            // Distance from point to line
            return Mathf.Abs(a * point.x + b * point.y + c) / Mathf.Sqrt(a * a + b * b);
        }
    }

    // Fix for CS1612: Cannot modify the return value of 'List<EdgeDetectionUtility.LineData>.this[int]' because it is not a variable

    // Update the EstimateDepthForLines method to use a temporary variable for the LineData struct
    private void EstimateDepthForLines(List<LineData> lines, int imageHeight)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            // Copy the LineData struct to a temporary variable
            LineData line = lines[i];

            // Get average Y position (normalized 0-1)
            float avgY = (line.start.y + line.end.y) / (2f * imageHeight);

            // Convert to depth (higher Y means further away in a typical photo of a house)
            float positionDepth = avgY;

            // Line orientation factor (horizontal = 1, vertical = 0)
            float horizontalness = Mathf.Abs(Mathf.Cos(line.angle * Mathf.Deg2Rad));

            // Length factor (longer lines might be closer)
            float normalizedLength = Mathf.Clamp01(line.length / 100f);
            float lengthFactor = 1f - normalizedLength * 0.3f;

            // Combine factors
            float depth = positionDepth * 0.6f +  // Position factor (60%)
                          horizontalness * 0.3f +  // Orientation factor (30%)
                          lengthFactor * 0.1f;    // Length factor (10%)

            // Clamp result
            line.depth = Mathf.Clamp01(depth);

            // Write the modified struct back to the list
            lines[i] = line;
        }
    }

    // Merge similar lines
    private List<LineData> MergeLines(List<LineData> lines)
    {
        List<LineData> mergedLines = new List<LineData>();

        if (lines.Count == 0) return mergedLines;

        // Sort lines by angle
        lines = lines.OrderBy(l => l.angle).ToList();

        // Threshold for considering lines as similar
        float angleTolerance = 5.0f; // degrees
        float distanceTolerance = maxLineGap;

        LineData currentGroup = lines[0];
        List<LineData> currentCluster = new List<LineData> { currentGroup };

        for (int i = 1; i < lines.Count; i++)
        {
            LineData line = lines[i];

            // Check if this line belongs to the current group
            bool similarAngle = Mathf.Abs(line.angle - currentGroup.angle) < angleTolerance ||
                               Mathf.Abs(line.angle - currentGroup.angle - 180) < angleTolerance ||
                               Mathf.Abs(line.angle - currentGroup.angle + 180) < angleTolerance;

            bool closeEnough = Vector2.Distance(line.start, currentGroup.start) < distanceTolerance ||
                              Vector2.Distance(line.start, currentGroup.end) < distanceTolerance ||
                              Vector2.Distance(line.end, currentGroup.start) < distanceTolerance ||
                              Vector2.Distance(line.end, currentGroup.end) < distanceTolerance;

            if (similarAngle && closeEnough)
            {
                // Add to current cluster
                currentCluster.Add(line);
            }
            else
            {
                // Merge current cluster and start a new one
                mergedLines.Add(MergeCluster(currentCluster));
                currentGroup = line;
                currentCluster.Clear();
                currentCluster.Add(line);
            }
        }

        // Add the last cluster
        if (currentCluster.Count > 0)
        {
            mergedLines.Add(MergeCluster(currentCluster));
        }

        return mergedLines;
    }

    // Merge a cluster of similar lines
    private LineData MergeCluster(List<LineData> cluster)
    {
        if (cluster.Count == 1) return cluster[0];

        // Find the extreme points of all lines in the cluster
        Vector2 minPoint = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxPoint = new Vector2(float.MinValue, float.MinValue);

        foreach (LineData line in cluster)
        {
            // Check start point
            if (line.start.x < minPoint.x || (line.start.x == minPoint.x && line.start.y < minPoint.y))
            {
                minPoint = line.start;
            }
            if (line.start.x > maxPoint.x || (line.start.x == maxPoint.x && line.start.y > maxPoint.y))
            {
                maxPoint = line.start;
            }

            // Check end point
            if (line.end.x < minPoint.x || (line.end.x == minPoint.x && line.end.y < minPoint.y))
            {
                minPoint = line.end;
            }
            if (line.end.x > maxPoint.x || (line.end.x == maxPoint.x && line.end.y > maxPoint.y))
            {
                maxPoint = line.end;
            }
        }

        // Calculate properties for the merged line
        Vector2 direction = maxPoint - minPoint;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Create a new merged line
        return new LineData(minPoint, maxPoint, angle, length);
    }

    // Draw a line on a texture using Bresenham's algorithm
    private void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // Check bounds
            if (x0 >= 0 && x0 < texture.width && y0 >= 0 && y0 < texture.height)
            {
                texture.SetPixel(x0, y0, color);
            }

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}