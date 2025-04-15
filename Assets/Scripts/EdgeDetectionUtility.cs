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

    // Class to represent a line in the image
    public class ImageLine
    {
        public Vector2 Start;
        public Vector2 End;
        public float Angle;
        public float Length;
        public float EstimatedDepth;

        public ImageLine(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
            Vector2 direction = end - start;
            Length = direction.magnitude;
            Angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            EstimatedDepth = 0.5f; // Default depth
        }

        public float DistanceToPoint(Vector2 point)
        {
            // Line equation: ax + by + c = 0
            float a = End.y - Start.y;
            float b = Start.x - End.x;
            float c = End.x * Start.y - Start.x * End.y;

            // Distance from point to line
            return Mathf.Abs(a * point.x + b * point.y + c) / Mathf.Sqrt(a * a + b * b);
        }

        public Vector2 ProjectPointOntoLine(Vector2 point)
        {
            Vector2 lineDir = (End - Start).normalized;
            float projection = Vector2.Dot(point - Start, lineDir);

            // Constrain to line segment
            projection = Mathf.Clamp(projection, 0, Length);

            return Start + lineDir * projection;
        }
    }

    public void SetThreshold(float threshold)
    {
        sobelThreshold = threshold;
    }

    // Apply edge detection to a texture
    public Texture2D DetectEdges(Texture2D source)
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

    // Extract lines from edge data using Hough transform
    public List<ImageLine> DetectLines(Texture2D edgeTexture)
    {
        if (edgeTexture == null) return new List<ImageLine>();

        List<ImageLine> detectedLines = new List<ImageLine>();

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

        // Simplified Hough transform implementation
        // In a real implementation, you would use a proper Hough space accumulator

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
                    // Line equation: x*cos(theta) + y*sin(theta) = rho
                    // We need to find the endpoints of the line segment within the image

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
                        detectedLines.Add(new ImageLine(start, end));
                    }

                    // Reset this peak to avoid detecting the same line multiple times
                    accumulator[thetaIndex, rhoIndex] = 0;
                }
            }
        }

        // Merge similar lines and remove duplicates
        return MergeLines(detectedLines);
    }

    // Estimate depth of lines based on image analysis
    public void EstimateDepth(List<ImageLine> lines, int imageHeight)
    {
        // Depth estimation heuristics:
        // 1. Vertical position (Y) - lower in image often means closer
        // 2. Line orientation - horizontal lines often represent surfaces at depth
        // 3. Line length - longer lines can imply closer surfaces

        foreach (ImageLine line in lines)
        {
            // Get average Y position (normalized 0-1)
            float avgY = (line.Start.y + line.End.y) / (2f * imageHeight);

            // Convert to depth (higher Y means further away in a typical photo of a house)
            float positionDepth = avgY;

            // Line orientation factor (horizontal = 1, vertical = 0)
            float horizontalness = Mathf.Abs(Mathf.Cos(line.Angle * Mathf.Deg2Rad));

            // Length factor (longer lines might be closer)
            float normalizedLength = Mathf.Clamp01(line.Length / 100f);
            float lengthFactor = 1f - normalizedLength * 0.3f;

            // Combine factors
            line.EstimatedDepth = positionDepth * 0.6f +  // Position factor (60%)
                                  horizontalness * 0.3f +  // Orientation factor (30%)
                                  lengthFactor * 0.1f;    // Length factor (10%)

            // Clamp result
            line.EstimatedDepth = Mathf.Clamp01(line.EstimatedDepth);
        }
    }

    // Merge similar lines to avoid duplicates
    private List<ImageLine> MergeLines(List<ImageLine> lines)
    {
        List<ImageLine> mergedLines = new List<ImageLine>();

        if (lines.Count == 0) return mergedLines;

        // Sort lines by angle
        lines = lines.OrderBy(l => l.Angle).ToList();

        // Threshold for considering lines as similar
        float angleTolerance = 5.0f; // degrees
        float distanceTolerance = maxLineGap;

        ImageLine currentGroup = lines[0];
        List<ImageLine> currentCluster = new List<ImageLine> { currentGroup };

        for (int i = 1; i < lines.Count; i++)
        {
            ImageLine line = lines[i];

            // Check if this line belongs to the current group
            bool similarAngle = Mathf.Abs(line.Angle - currentGroup.Angle) < angleTolerance ||
                               Mathf.Abs(line.Angle - currentGroup.Angle - 180) < angleTolerance ||
                               Mathf.Abs(line.Angle - currentGroup.Angle + 180) < angleTolerance;

            bool closeEnough = Vector2.Distance(line.Start, currentGroup.Start) < distanceTolerance ||
                              Vector2.Distance(line.Start, currentGroup.End) < distanceTolerance ||
                              Vector2.Distance(line.End, currentGroup.Start) < distanceTolerance ||
                              Vector2.Distance(line.End, currentGroup.End) < distanceTolerance;

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

    // Merge a cluster of similar lines into a single representative line
    private ImageLine MergeCluster(List<ImageLine> cluster)
    {
        if (cluster.Count == 1) return cluster[0];

        // Find the extreme points of all lines in the cluster
        Vector2 minPoint = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxPoint = new Vector2(float.MinValue, float.MinValue);

        foreach (ImageLine line in cluster)
        {
            // Check start point
            if (line.Start.x < minPoint.x || (line.Start.x == minPoint.x && line.Start.y < minPoint.y))
            {
                minPoint = line.Start;
            }
            if (line.Start.x > maxPoint.x || (line.Start.x == maxPoint.x && line.Start.y > maxPoint.y))
            {
                maxPoint = line.Start;
            }

            // Check end point
            if (line.End.x < minPoint.x || (line.End.x == minPoint.x && line.End.y < minPoint.y))
            {
                minPoint = line.End;
            }
            if (line.End.x > maxPoint.x || (line.End.x == maxPoint.x && line.End.y > maxPoint.y))
            {
                maxPoint = line.End;
            }
        }

        // Create a new merged line using the extreme points
        return new ImageLine(minPoint, maxPoint);
    }

    // Visualize detected lines on a texture
    public Texture2D VisualizeLines(Texture2D sourceTexture, List<ImageLine> lines)
    {
        Texture2D visualTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);

        // Copy source texture
        Graphics.CopyTexture(sourceTexture, visualTexture);

        // Draw each line
        foreach (ImageLine line in lines)
        {
            // Color based on depth
            Color lineColor = Color.Lerp(Color.red, Color.green, line.EstimatedDepth);

            // Draw the line
            DrawLine(visualTexture, (int)line.Start.x, (int)line.Start.y,
                    (int)line.End.x, (int)line.End.y, lineColor);
        }

        visualTexture.Apply();
        return visualTexture;
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

    // Find the closest line to a point
    public ImageLine FindClosestLine(List<ImageLine> lines, Vector2 point, float maxDistance)
    {
        ImageLine closestLine = null;
        float minDistance = maxDistance;

        foreach (ImageLine line in lines)
        {
            float distance = line.DistanceToPoint(point);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestLine = line;
            }
        }

        return closestLine;
    }
}