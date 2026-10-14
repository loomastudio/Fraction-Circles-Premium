using UnityEngine;

namespace FractionCircles
{
    /// <summary>
    /// All textures and sprites of the tool. They are loaded from Resources/FractionCircles when the image files
    /// exist there, otherwise they are drawn in code.
    /// </summary>
    public static class ProceduralArt
    {
        public const string ResourceFolder = "FractionCircles";

        /// <summary>A generated image: its file name, pixels, 9-slice border and whether it is a tiling texture.</summary>
        public struct ArtFile
        {
            public string name;
            public Texture2D texture;
            public float border;
            public bool tiling;
        }

        /// <summary>Draws every image so it can be saved as a file.</summary>
        public static ArtFile[] GenerateAll() => new[]
        {
            new ArtFile { name = "circle", texture = CircleTex(128) },
            new ArtFile { name = "rounded", texture = RoundedTex(64, 16, 0, Color.white, Color.white), border = 20 },
            new ArtFile { name = "card", texture = RoundedTex(64, 12, 3, Color.white, new Color(0.45f, 0.45f, 0.47f)), border = 16 },
            new ArtFile { name = "glow", texture = GlowTex(128) },
            new ArtFile { name = "star", texture = StarTex(64) },
            new ArtFile { name = "pill", texture = RoundedTex(128, 64, 0, Color.white, Color.white), border = 62 },
            new ArtFile { name = "ring", texture = RingTex(128, 7) },
            new ArtFile { name = "speaker", texture = ShapeTex(96, SpeakerShape) },
            new ArtFile { name = "note", texture = ShapeTex(96, NoteShape) },
            new ArtFile { name = "house", texture = ShapeTex(96, HouseShape) },
            new ArtFile { name = "felt", texture = FeltTex(256), tiling = true },
            new ArtFile { name = "wood", texture = WoodTex(512), tiling = true },
        };

        static Sprite LoadSprite(string name, System.Func<Sprite> draw)
        {
            var s = Resources.Load<Sprite>(ResourceFolder + "/" + name);
            return s != null ? s : draw();
        }

        static Texture2D LoadTexture(string name, System.Func<Texture2D> draw)
        {
            var t = Resources.Load<Texture2D>(ResourceFolder + "/" + name);
            return t != null ? t : draw();
        }

        static Sprite circle, rounded, card, glow, star;
        static Texture2D felt, wood;

        public static Sprite Circle => circle ? circle : (circle = LoadSprite("circle", () => MakeSprite(CircleTex(128), 0)));
        public static Sprite Rounded => rounded ? rounded : (rounded = LoadSprite("rounded", () => MakeSprite(RoundedTex(64, 16, 0, Color.white, Color.white), 20)));
        public static Sprite Card => card ? card : (card = LoadSprite("card", () => MakeSprite(RoundedTex(64, 12, 3, Color.white, new Color(0.45f, 0.45f, 0.47f)), 16)));
        public static Sprite Glow => glow ? glow : (glow = LoadSprite("glow", () => MakeSprite(GlowTex(128), 0)));
        public static Sprite Star => star ? star : (star = LoadSprite("star", () => MakeSprite(StarTex(64), 0)));
        public static Texture2D FeltTexture => felt ? felt : (felt = LoadTexture("felt", () => FeltTex(256)));
        public static Texture2D WoodTexture => wood ? wood : (wood = LoadTexture("wood", () => WoodTex(512)));

        static Sprite pill, ring, speaker, note, house;

        public static Sprite Pill => pill ? pill : (pill = LoadSprite("pill", () => MakeSprite(RoundedTex(128, 64, 0, Color.white, Color.white), 62)));
        public static Sprite Ring => ring ? ring : (ring = LoadSprite("ring", () => MakeSprite(RingTex(128, 7), 0)));
        public static Sprite Speaker => speaker ? speaker : (speaker = LoadSprite("speaker", () => MakeSprite(ShapeTex(96, SpeakerShape), 0)));
        public static Sprite Note => note ? note : (note = LoadSprite("note", () => MakeSprite(ShapeTex(96, NoteShape), 0)));
        public static Sprite House => house ? house : (house = LoadSprite("house", () => MakeSprite(ShapeTex(96, HouseShape), 0)));

        static Texture2D RingTex(int s, float width)
        {
            var t = NewTex(s, s, false);
            var px = new Color32[s * s];
            float r = s * 0.5f - width - 1;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Mathf.Abs(new Vector2(x + 0.5f - s * 0.5f, y + 0.5f - s * 0.5f).magnitude - r);
                px[y * s + x] = new Color(1, 1, 1, Mathf.Clamp01(width - d + 0.5f));
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        /// <summary>Rasterises a shape given as an inside test over [-0.5, 0.5]², with 4x4 super-sampling.</summary>
        static Texture2D ShapeTex(int s, System.Func<Vector2, bool> inside)
        {
            var t = NewTex(s, s, false);
            var px = new Color32[s * s];
            const int ss = 4;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ss; sy++)
                for (int sx = 0; sx < ss; sx++)
                {
                    var p = new Vector2((x + (sx + 0.5f) / ss) / s - 0.5f, (y + (sy + 0.5f) / ss) / s - 0.5f);
                    if (inside(p)) hits++;
                }
                px[y * s + x] = new Color(1, 1, 1, hits / (float)(ss * ss));
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        static readonly Vector2[] SpeakerBody =
        {
            new Vector2(-0.38f, -0.12f), new Vector2(-0.2f, -0.12f), new Vector2(0.0f, -0.32f),
            new Vector2(0.0f, 0.32f), new Vector2(-0.2f, 0.12f), new Vector2(-0.38f, 0.12f),
        };

        static bool SpeakerShape(Vector2 p)
        {
            if (InPolygon(p, SpeakerBody)) return true;
            var q = p - new Vector2(0.02f, 0);
            if (q.x <= 0.04f || Mathf.Abs(Mathf.Atan2(q.y, q.x)) > 0.85f) return false;
            float len = q.magnitude;
            return Mathf.Abs(len - 0.17f) < 0.035f || Mathf.Abs(len - 0.31f) < 0.035f;
        }

        static readonly Vector2[] NoteFlag =
        {
            new Vector2(0.05f, 0.38f), new Vector2(0.05f, 0.24f), new Vector2(0.3f, 0.1f), new Vector2(0.27f, 0.24f),
        };

        static bool NoteShape(Vector2 p)
        {
            var h = new Vector2((p.x + 0.09f) / 0.15f, (p.y + 0.24f) / 0.11f);
            if (h.sqrMagnitude < 1f) return true;
            if (p.x > 0.0f && p.x < 0.06f && p.y > -0.24f && p.y < 0.38f) return true;
            return InPolygon(p, NoteFlag);
        }

        static readonly Vector2[] Roof = { new Vector2(-0.42f, 0.0f), new Vector2(0f, 0.4f), new Vector2(0.42f, 0.0f) };

        static bool HouseShape(Vector2 p)
        {
            bool door = p.x > -0.07f && p.x < 0.07f && p.y < -0.12f;
            if (door) return false;
            if (InPolygon(p, Roof)) return true;
            return p.x > -0.28f && p.x < 0.28f && p.y > -0.38f && p.y < 0.02f;
        }

        static Sprite MakeSprite(Texture2D t, float border) =>
            Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));

        static Texture2D NewTex(int w, int h, bool repeat)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
        }

        static Texture2D CircleTex(int s)
        {
            var t = NewTex(s, s, false);
            var px = new Color32[s * s];
            float r = s * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude;
                float a = Mathf.Clamp01(r - 1f - d + 0.5f);
                px[y * s + x] = new Color(1, 1, 1, a);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        static Texture2D RoundedTex(int s, float radius, float border, Color fill, Color edge)
        {
            var t = NewTex(s, s, false);
            var px = new Color32[s * s];
            float h = s * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                var p = new Vector2(Mathf.Abs(x + 0.5f - h), Mathf.Abs(y + 0.5f - h));
                var q = p - new Vector2(h - radius, h - radius);
                float d = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - (radius - 0.5f);
                float a = Mathf.Clamp01(0.5f - d);
                Color c = fill;
                if (border > 0) c = Color.Lerp(fill, edge, Mathf.Clamp01(d + border + 0.5f));
                c.a = a;
                px[y * s + x] = c;
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        static Texture2D GlowTex(int s)
        {
            var t = NewTex(s, s, false);
            var px = new Color32[s * s];
            float r = s * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude / r;
                float a = Mathf.Clamp01(1f - d);
                px[y * s + x] = new Color(1, 1, 1, a * a);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        static Texture2D StarTex(int s)
        {
            var t = NewTex(s, s, false);
            var px = new Color32[s * s];
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.Deg2Rad * (90 + i * 36);
                float rr = (i % 2 == 0 ? 0.48f : 0.2f) * s;
                pts[i] = new Vector2(s * 0.5f + Mathf.Cos(ang) * rr, s * 0.5f + Mathf.Sin(ang) * rr);
            }
            const int ss = 4; // super-sampling for smooth edges
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ss; sy++)
                for (int sx = 0; sx < ss; sx++)
                    if (InPolygon(new Vector2(x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss), pts)) hits++;
                px[y * s + x] = new Color(1, 1, 1, hits / (float)(ss * ss));
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        static bool InPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        static Texture2D FeltTex(int s)
        {
            var t = NewTex(s, s, true);
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = x / (float)s, v = y / (float)s;
                float n = Fbm(u * 16, v * 16, 16, 16, 3, 3);
                float fiber = Hash(x, y, 5) - 0.5f;
                float k = 0.86f + 0.18f * n + 0.08f * fiber;
                var c = Theme.Felt * k;
                c.a = 1;
                px[y * s + x] = c;
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        static Texture2D WoodTex(int s)
        {
            var t = NewTex(s, s, true);
            var px = new Color32[s * s];
            var baseCol = new Color(0.80f, 0.62f, 0.42f);
            const int planks = 4;
            int pw = s / planks, half = s / 2;
            for (int x = 0; x < s; x++)
            {
                int pi = x / pw, lx = x % pw;
                int jointOffset = (int)(Hash(pi, 7, 3) * s);
                for (int y = 0; y < s; y++)
                {
                    int yy = (y + jointOffset) % s;
                    int board = pi * 2 + yy / half;
                    float tone = 0.86f + 0.22f * Hash(board, 1, 9);
                    float u = x / (float)s, v = y / (float)s;
                    float n = Fbm(u * 8, v * 2, 8, 2, 11 + board, 4);
                    float g = Mathf.Sin(lx * 0.22f + n * 16f + Hash(board, 2, 2) * 10f) * 0.5f + 0.5f;
                    float dark = Mathf.Pow(g, 6) * 0.16f;
                    float knotty = Mathf.Pow(Fbm(u * 4, v * 4, 4, 4, 77 + board, 2), 3) * 0.12f;
                    float fine = (Hash(x, y, 21) - 0.5f) * 0.05f;
                    float k = tone * (1f - dark - knotty + fine);
                    if (lx < 2) k *= 0.72f;              // gap between planks
                    if (yy % half < 2) k *= 0.78f;       // butt joint
                    var c = baseCol * k;
                    c.a = 1;
                    px[y * s + x] = c;
                }
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        // ---------- tileable value noise ----------

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        static int Wrap(int v, int p) => ((v % p) + p) % p;

        static float ValueNoise(float x, float y, int px, int py, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            int x0 = Wrap(xi, px), x1 = Wrap(xi + 1, px), y0 = Wrap(yi, py), y1 = Wrap(yi + 1, py);
            float a = Mathf.Lerp(Hash(x0, y0, seed), Hash(x1, y0, seed), fx);
            float b = Mathf.Lerp(Hash(x0, y1, seed), Hash(x1, y1, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        static float Fbm(float x, float y, int px, int py, int seed, int octaves)
        {
            float sum = 0, amp = 0.5f, norm = 0;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise(x, y, px, py, seed + i * 31) * amp;
                norm += amp;
                x *= 2; y *= 2; px *= 2; py *= 2; amp *= 0.5f;
            }
            return sum / norm;
        }
    }
}
