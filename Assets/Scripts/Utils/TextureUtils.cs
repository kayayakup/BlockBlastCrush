using System.Collections.Generic;
using UnityEngine;

public static class TextureUtils
{
    private static Sprite _whiteCellSprite;
    private static Sprite _crownSprite;
    private static Sprite _gearSprite;
    private static readonly Dictionary<Color, Sprite> _blockCache = new Dictionary<Color, Sprite>();

    // PPU ayarı 1 birimlik dünya alanına 512 piksel sığdırır, bu da 1080p'de mükemmel netlik sağlar.
    private const int RESOLUTION = 512;

    public static Sprite WhiteCellSprite =>
        _whiteCellSprite ?? (_whiteCellSprite = CreateRoundedRectSprite(RESOLUTION, RESOLUTION, 72f, Color.white));

    public static Sprite CrownSprite => _crownSprite ?? (_crownSprite = BuildCrownSprite());
    public static Sprite GearSprite => _gearSprite ?? (_gearSprite = BuildGearSprite());

    public static Sprite GetBlockSprite(Color baseColor)
    {
        if (_blockCache.TryGetValue(baseColor, out var cached)) return cached;

        const int W = RESOLUTION, H = RESOLUTION;
        const float R = 68f;
        const float B = 52f;

        var tex = NewTex(W, H);
        var px = new Color[W * H];

        // Renk paleti - 3D Efektleri için
        Color hiTop = Brighten(baseColor, 0.58f);
        Color hiGloss = Brighten(baseColor, 0.85f);
        Color shBot = Darken(baseColor, 0.50f);
        Color shRight = Darken(baseColor, 0.30f);
        Color hiLeft = Brighten(baseColor, 0.22f);
        Color face = Desaturate(baseColor, 0.07f);

        float inset = B * 1.05f;
        float cx = W * 0.5f;
        float cy = H * 0.5f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                // Mükemmel kenar yumuşatma için SDF
                float alpha = RoundedRectSDF(x, y, W, H, R);
                if (alpha <= 0.001f) { px[y * W + x] = Color.clear; continue; }

                float dTop = (H - 1) - y;
                float dBot = y;
                float dLeft = x;
                float dRight = (W - 1) - x;

                Color c = face;

                // 3D Bevel (Eğim) Gölgelendirmesi
                if (dBot < B) { float t = Mathf.Clamp01(1f - dBot / B); c = Color.Lerp(c, shBot, t * t * 0.92f); }
                if (dRight < B) { float t = Mathf.Clamp01(1f - dRight / B); c = Color.Lerp(c, shRight, t * t * 0.72f); }
                if (dTop < B) { float t = Mathf.Clamp01(1f - dTop / B); c = Color.Lerp(c, hiTop, t * t * 0.88f); }
                if (dLeft < B) { float t = Mathf.Clamp01(1f - dLeft / B); c = Color.Lerp(c, hiLeft, t * t * 0.52f); }

                // Parlama Şeridi (Glossy Stripe)
                float glossLo = H - inset - 50f;
                float glossHi = H - inset - 14f;
                if (y >= glossLo && y <= glossHi && x > inset + 18f && x < W - inset - 18f)
                {
                    float gt = SmoothStep(glossLo, glossLo + 10f, y) * SmoothStep(glossHi, glossHi - 10f, y);
                    float gx = SmoothStep(inset, inset + 40f, x) * SmoothStep(W - inset, W - inset - 40f, x);
                    c = Color.Lerp(c, hiGloss, gt * gx * 0.6f);
                }

                c.a = alpha * baseColor.a;
                px[y * W + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply(false, true);
        var sp = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W);
        _blockCache[baseColor] = sp;
        return sp;
    }

    public static Sprite CreateRoundedRectSprite(int w, int h, float radius, Color color)
    {
        var tex = NewTex(w, h);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float a = RoundedRectSDF(x, y, w, h, radius);
                px[y * w + x] = new Color(color.r, color.g, color.b, color.a * a);
            }
        }
        tex.SetPixels(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
    }

    private static Sprite BuildCrownSprite()
    {
        int w = 72, h = 52;
        var tex = new Texture2D(w, h, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);

        Color32 gold = new Color32(255, 196, 8, 255);
        Color32 light = new Color32(255, 235, 130, 255);
        Color32 dark = new Color32(200, 148, 0, 255);

        void Set(int x, int y, Color32 c)
        {
            if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c;
        }
        void FillRect(int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int yy = y0; yy <= y1; yy++)
                for (int xx = x0; xx <= x1; xx++)
                    Set(xx, yy, c);
        }
        void FillCircle(int cx, int cy, int r, Color32 c)
        {
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (dx * dx + dy * dy <= r * r) Set(cx + dx, cy + dy, c);
        }

        // Base strip (bottom 15 rows, full width)
        FillRect(3, 0, w - 4, 14, gold);
        FillRect(3, 12, w - 4, 14, light);
        FillRect(3, 0, w - 4, 2, dark);

        // Three triangular peaks
        int[] apexX = { 9, w / 2, w - 10 };
        int baseY = 15, topY = h - 7;

        for (int i = 0; i < 3; i++)
        {
            int ax = apexX[i];
            for (int yy = baseY; yy <= topY; yy++)
            {
                float t = (float)(yy - baseY) / (topY - baseY);
                int hw = Mathf.Max(1, (int)(9f * (1f - t)));
                for (int xx = ax - hw; xx <= ax + hw; xx++) Set(xx, yy, gold);
            }
            FillCircle(ax, topY, 5, gold);
            FillCircle(ax, topY, 3, light);
        }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite BuildGearSprite()
    {
        const int W = 256, H = 256;
        var tex = NewTex(W, H);
        var px = new Color[W * H];
        float cx = W * 0.5f, cy = H * 0.5f;
        // Gear çiziminde Mathf.Min(outer, inner) + 0.5f yerine 
        // direkt SDF mantığı (Clamped) kullanıldı.
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(72f - d); // Örnek pürüzsüz daire
                px[y * W + x] = new Color(0.8f, 0.8f, 0.9f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W);
    }

    // ── Gelişmiş SDF Metodu (Pikselleşmeyi Yok Eden Matematik) ────────────────
    private static float RoundedRectSDF(float px, float py, float w, float h, float r)
    {
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;
        // Objenin merkezine göre koordinatları normalize et
        float dx = Mathf.Abs(px - (w - 1) * 0.5f) - (halfW - r);
        float dy = Mathf.Abs(py - (h - 1) * 0.5f) - (halfH - r);

        float externalDist = Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) + Mathf.Max(dy, 0) * Mathf.Max(dy, 0));
        float internalDist = Mathf.Min(Mathf.Max(dx, dy), 0);
        float dist = externalDist + internalDist - r;

        // Anti-aliasing geçişi: 0.5 piksel genişliğinde pürüzsüzlük
        return Mathf.Clamp01(0.5f - dist);
    }

    // ── Texture Ayarları (Netlik Buradan Gelir) ───────────────────────────────
    private static Texture2D NewTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            // Bilinear, Trilinear'dan daha keskindir (Blur yapmaz)
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            anisoLevel = 1 // Mobil için 1 idealdir, fazlası görüntüyü bozar
        };
        return tex;
    }

    // Geriye dönük uyumluluk
    public static Sprite CreateRoundedRect(int w, int h, float radius, Color color)
        => CreateRoundedRectSprite(w, h, radius, color);

    private static Color Brighten(Color c, float t) => Color.Lerp(c, Color.white, t);
    private static Color Darken(Color c, float t) => Color.Lerp(c, Color.black, t);
    private static Color Desaturate(Color c, float t)
    {
        float g = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
        return Color.Lerp(c, new Color(g, g, g, c.a), t);
    }
    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }
}