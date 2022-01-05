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
    private static Text healthText;
    private static RectTransform healthBar;
    private static Text energyText;
    private static RectTransform energyBar;

    void Awake()
    {
        // setting up resource elements
        healthText = transform.Find("HealthText").gameObject.GetComponent<Text>();
        healthBar = transform.Find("HealthText").Find("HealthBar").gameObject.GetComponent<RectTransform>();
        energyText = transform.Find("EnergyText").gameObject.GetComponent<Text>();
        energyBar = transform.Find("EnergyText").Find("EnergyBar").gameObject.GetComponent<RectTransform>();
        // setting up minimap
        setUpMinimap();
    }

    public void setUpMinimap()
    {
        // calculating the size of the minimap
        float canvasX = GameObject.Find("Canvas").GetComponent<RectTransform>().rect.width;
        GameObject.Find("Minimap").GetComponent<RectTransform>().localScale = new Vector3(0.25f * canvasX, 0.25f * canvasX, 1.0f);

        // getting the minimap data
        minimap = GameObject.Find("Minimap").GetComponent<RawImage>();
        mapSize = GameObject.Find("Map").GetComponent<Map>().mapSize + 2;

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

    public static void setHealth(float currentHealth, float maxHealth)
    {
        healthText.text = Mathf.RoundToInt(currentHealth) + "/" + Mathf.RoundToInt(maxHealth);
        healthBar.localScale = new Vector3(currentHealth/ maxHealth, 1f, 1f);
    }

    public static void setEnergy(float currentEnergy, float maxEnergy) {
        energyText.text = Mathf.RoundToInt(currentEnergy) + "/" + Mathf.RoundToInt(maxEnergy);
        energyBar.localScale = new Vector3(currentEnergy / maxEnergy, 1f, 1f);
    }
}
