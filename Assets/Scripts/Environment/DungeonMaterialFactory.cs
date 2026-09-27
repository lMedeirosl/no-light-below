using UnityEngine;

namespace NoLightBelow.Environment
{
    public static class DungeonMaterialFactory
    {
        public static Shader GetURPLitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return shader;
        }

        public static Material CreateStoneWallMaterial()
        {
            Material mat = new Material(GetURPLitShader());
            mat.name = "Mat_DungeonStoneWall";
            mat.color = new Color(0.24f, 0.25f, 0.26f);

            Texture2D noiseTex = GeneratePerlinTexture(256, 256, 14f, 0.45f, new Color(0.26f, 0.27f, 0.28f), new Color(0.12f, 0.13f, 0.14f));
            Texture2D normalTex = GenerateNormalMap(noiseTex, 3.2f);

            mat.mainTexture = noiseTex;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalTex);
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1.8f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.04f);

            return mat;
        }

        public static Material CreateDungeonFloorMaterial()
        {
            Material mat = new Material(GetURPLitShader());
            mat.name = "Mat_DungeonFloorCobble";
            mat.color = new Color(0.24f, 0.23f, 0.22f);

            Texture2D tileTex = GenerateTileTexture(256, 256, 8, new Color(0.28f, 0.27f, 0.25f), new Color(0.08f, 0.07f, 0.06f));
            Texture2D normalTex = GenerateNormalMap(tileTex, 4.0f); // Deep bevel grooves

            mat.mainTexture = tileTex;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalTex);
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 2.0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.34f); // Damp wet stone sheen
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.02f);

            return mat;
        }

        public static Material CreateRustyIronMaterial()
        {
            Material mat = new Material(GetURPLitShader());
            mat.name = "Mat_RustyIron";
            mat.color = new Color(0.22f, 0.18f, 0.15f);

            Texture2D scratchTex = GeneratePerlinTexture(128, 128, 28f, 0.2f, new Color(0.28f, 0.24f, 0.20f), new Color(0.14f, 0.11f, 0.09f));
            Texture2D normalTex = GenerateNormalMap(scratchTex, 1.8f);

            mat.mainTexture = scratchTex;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalTex);
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1.2f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.42f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.85f);

            return mat;
        }

        public static Material CreateTorchFlameMaterial()
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) unlit = Shader.Find("Unlit/Color");
            Material mat = new Material(unlit != null ? unlit : GetURPLitShader());
            mat.name = "Mat_TorchFlame";
            Color flameAmber = new Color(1.0f, 0.55f, 0.12f) * 2.8f; // High HDR emission for Bloom
            mat.color = flameAmber;

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", flameAmber);
            }
            return mat;
        }

        public static Material CreatePlayerArmorMaterial()
        {
            Material mat = new Material(GetURPLitShader());
            mat.name = "Mat_PlayerTunic";
            mat.color = new Color(0.24f, 0.28f, 0.35f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.25f);
            return mat;
        }

        public static Material CreateDummyMaterial()
        {
            Material mat = new Material(GetURPLitShader());
            mat.name = "Mat_TrainingDummy";
            mat.color = new Color(0.65f, 0.45f, 0.28f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
            return mat;
        }

        public static Texture2D GenerateTileTexture(int width, int height, int tiles, Color stoneColor, Color mortarColor)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            int tileSizeX = width / tiles;
            int tileSizeY = height / tiles;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = (x % tileSizeX <= 2) || (y % tileSizeY <= 2);
                    float noise = Mathf.PerlinNoise(x * 0.06f, y * 0.06f) * 0.28f;

                    if (isBorder)
                    {
                        tex.SetPixel(x, y, mortarColor);
                    }
                    else
                    {
                        Color c = stoneColor + new Color(noise, noise, noise);
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        public static Texture2D GeneratePerlinTexture(int width, int height, float scale, float strength, Color baseCol, Color darkCol)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float p = Mathf.PerlinNoise(x / (float)width * scale, y / (float)height * scale);
                    Color c = Color.Lerp(darkCol, baseCol, p);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateNormalMap(Texture2D heightMap, float strength = 2.5f)
        {
            int w = heightMap.width;
            int h = heightMap.height;
            Texture2D normalMap = new Texture2D(w, h, TextureFormat.RGBA32, true);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float xLeft = heightMap.GetPixel((x - 1 + w) % w, y).grayscale;
                    float xRight = heightMap.GetPixel((x + 1) % w, y).grayscale;
                    float yDown = heightMap.GetPixel(x, (y - 1 + h) % h).grayscale;
                    float yUp = heightMap.GetPixel(x, (y + 1) % h).grayscale;

                    float xDiff = (xLeft - xRight) * strength;
                    float yDiff = (yDown - yUp) * strength;

                    Vector3 n = new Vector3(xDiff, yDiff, 1.0f).normalized;

                    Color nc = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1.0f);
                    normalMap.SetPixel(x, y, nc);
                }
            }

            normalMap.Apply();
            return normalMap;
        }
    }
}
