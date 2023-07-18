using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SobelFilter : MonoBehaviour
{
    public Texture2D sourceImage;
    public Texture2D grayscaleImage;
    public Texture2D filteredImage;
    public Texture2D filteredGrayscaleImage;

    public void GenerateImages()
    {
        // Convert source image to grayscale
        Texture2D grayscaleImage = ConvertToGrayscale(sourceImage);
        this.grayscaleImage = grayscaleImage;

        // Apply Sobel filter
        Texture2D filteredImage = ApplySobelFilter(grayscaleImage);

        // Display the filtered image
        this.filteredImage = filteredImage;

        Texture2D filteredGrayscaleImage = ConvertToGrayscale(filteredImage);
        this.filteredGrayscaleImage = filteredGrayscaleImage;
    }

    private Texture2D ConvertToGrayscale(Texture2D source)
    {
        int width = source.width;
        int height = source.height;
        Texture2D grayscaleImage = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color[] pixels = source.GetPixels();
        Color[] grayscalePixels = new Color[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            float grayscaleValue = pixel.grayscale;
            grayscalePixels[i] = new Color(grayscaleValue, grayscaleValue, grayscaleValue, pixel.a);
        }

        grayscaleImage.SetPixels(grayscalePixels);
        grayscaleImage.Apply();

        return grayscaleImage;
    }

    private Texture2D ApplySobelFilter(Texture2D source)
    {
        int width = source.width;
        int height = source.height;
        Texture2D filteredImage = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color[] pixels = source.GetPixels();
        Color[] filteredPixels = new Color[pixels.Length];

        int[,] mask = new int[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                float sx = 0;
                float sy = 0;

                for (int mx = -1; mx <= 1; mx++)
                {
                    for (int my = -1; my <= 1; my++)
                    {
                        int neighborX = x + mx;
                        int neighborY = y + my;

                        Color neighborPixel = pixels[neighborY * width + neighborX];
                        float neighborGrayscale = neighborPixel.grayscale;

                        sx += neighborGrayscale * mask[mx + 1, my + 1];
                        sy += neighborGrayscale * mask[my + 1, mx + 1];
                    }
                }

                filteredPixels[y * width + x] = new Color(Mathf.Abs(sx), Mathf.Abs(sy), 0, 1);
            }
        }

        filteredImage.SetPixels(filteredPixels);
        filteredImage.Apply();

        return filteredImage;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SobelFilter))]
public class SobelFilterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SobelFilter sobelGenerator = (SobelFilter)target;

        if (GUILayout.Button("Generate Images"))
        {
            sobelGenerator.GenerateImages();
        }

        if (GUILayout.Button("Save Greyscale Image"))
        {
            string savePath = EditorUtility.SaveFilePanel("Save Greyscale Image", "", "GreyscaleImage", "png");

            if (!string.IsNullOrEmpty(savePath))
            {
                SaveTextureToPNG(sobelGenerator.grayscaleImage, savePath);
            }
        }

        if (GUILayout.Button("Save Filtered Image"))
        {
            string savePath = EditorUtility.SaveFilePanel("Save SobelFiltered Image", "", "SobelFilteredImage", "png");

            if (!string.IsNullOrEmpty(savePath))
            {
                SaveTextureToPNG(sobelGenerator.filteredImage, savePath);
            }
        }

        if (GUILayout.Button("Save FilteredGrayscale Image"))
        {
            string savePath = EditorUtility.SaveFilePanel("Save SobelFilteredGrayscale Image", "", "SobelFilteredGrayscaleImage", "png");

            if (!string.IsNullOrEmpty(savePath))
            {
                SaveTextureToPNG(sobelGenerator.filteredGrayscaleImage, savePath);
            }
        }
    }

    private void SaveTextureToPNG(Texture2D texture, string savePath)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);
        Debug.Log("Texture saved to: " + savePath);
    }
}
#endif