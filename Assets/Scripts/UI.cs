using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    // Minimap
    private static RawImage minimap;
    private static int mapSize;
    private static Texture2D texture;
    private static Vector2Int lastPlayerPos;
    private static float alpha = 0.5f;

    // Health Text
    private static Text health;

    void Start()
    {
        setUpMinimap();

        // setting up the health text
        float canvasX = GameObject.Find("Canvas").GetComponent<RectTransform>().rect.width;
        health = GetComponentInChildren<Text>();
        health.gameObject.transform.localScale = new Vector3(0.001f * canvasX, 0.001f * canvasX, 1.0f);
        health.color = Color.white;
        health.text = "Health: " + GameObject.Find("Player").GetComponent<Player>().health;
    }

    public void setUpMinimap()
    {
        // calculating the size of the minimap
        float canvasX = GameObject.Find("Canvas").GetComponent<RectTransform>().rect.width;
        GameObject.Find("Minimap").GetComponent<RectTransform>().localScale = new Vector3(0.25f * canvasX, 0.25f * canvasX, 1.0f);

        // getting the minimap data
        minimap = GameObject.Find("Minimap").GetComponent<RawImage>();
        mapSize = GameObject.Find("Map").GetComponent<Map>().size + 2;

        texture = new Texture2D(mapSize, mapSize, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;

        lastPlayerPos = new Vector2Int((int)GameObject.Find("Player").transform.position.x, (int)GameObject.Find("Player").transform.position.y);

        // setting pixels up so that transparency works
        Color fillColor = Color.clear;
        Color[] fillPixels = new Color[texture.width * texture.height];
        for (int i = 0; i < fillPixels.Length; i++)
            fillPixels[i] = fillColor;
        texture.SetPixels(fillPixels);

        // coloring blackground
        for (int i = 0; i < mapSize; i++)
            for (int j = 0; j < mapSize; j++)
                texture.SetPixel(i, j, new Color(0.1f, 0.1f, 0.1f, alpha));
        texture.Apply();
        minimap.texture = texture;
    }

    public static void paintMinimap(int x, int y, string tag)
    {
        // check to see if in screen bounds
        Vector3 p = Camera.main.WorldToViewportPoint(new Vector3(x, y, 0));
        if (p.x >= 0 && p.x <= 1 && p.y >=0 && p.y <= 1) {
            if (tag.Equals("Wall"))
                texture.SetPixel(x, y, new Color(0.9f, 0.9f, 0.9f, alpha));
            else if (tag.Equals("Door"))
                texture.SetPixel(x, y, new Color(0f, 1f, 1f, alpha));
            else if (tag.Equals("Player"))
            {
                texture.SetPixel(lastPlayerPos.x, lastPlayerPos.y, new Color(0f, 0.4f, 0f, alpha));
                texture.SetPixel(x, y, Color.green);
                lastPlayerPos = new Vector2Int(x, y);
            } else if (tag.Equals("Enemy"))
            {
                //texture.SetPixel(x, y, Color.red);
            }
            texture.Apply();
            minimap.texture = texture;
        }
    }

    public static void setHealthText(string text)
    {
        health.text = text;
    }
}
