// Utility class to handle texture operations
using UnityEngine;

public static class TextureUtility
{
    public static Texture2D GetReadableTexture(Texture2D source)
    {
        // If the source texture is null, return null
        if (source == null) return null;

        // Create a temporary RenderTexture with the same size as the texture
        RenderTexture renderTex = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);

        // Backup the currently active render texture
        RenderTexture previousRT = RenderTexture.active;

        // Set the current render texture to our temporary one
        RenderTexture.active = renderTex;

        // Copy the texture data to the render texture
        Graphics.Blit(source, renderTex);

        // Create a new readable texture
        Texture2D readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);

        // Read the render texture data into the readable texture
        readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableTexture.Apply();

        // Restore the previous render texture
        RenderTexture.active = previousRT;

        // Release the temporary render texture
        RenderTexture.ReleaseTemporary(renderTex);

        return readableTexture;
    }
}