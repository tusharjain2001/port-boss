using System;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss
{
    // What a player's dock worker looks like: picked in the character editor, saved on this PC, synced to every peer
    // (Player.LookNet, host-checked). 16 bytes; every field is an index, so a bad value can only pick a wrong option.
    [Serializable]
    public struct Look : INetworkSerializeByMemcpy, IEquatable<Look>
    {
        public byte Set;                                      // 0 = never chosen (the slot's default is shown)
        public byte Face, Hair, Beard, Hat, Top, Gloves;
        public byte SkinTone, HairColor, TopColor, AccentColor, LegsColor, BootsColor, GloveColor, HatColor, Spare;

        public static readonly string[] Faces = { "Bruno", "Sunny", "Hawk", "Old Salt" };
        public static readonly string[] Hairs = { "Bald", "Buzz cut", "Quiff", "Curls" };
        public static readonly string[] Beards = { "Clean", "Moustache", "Full beard", "Goatee" };
        public static readonly string[] Hats = { "None", "Cap", "Beanie", "Hard hat" };
        public static readonly string[] Tops = { "T-shirt", "Hi-vis vest", "Overalls", "Work shirt" };
        public static readonly string[] GloveNames = { "Bare hands", "Work gloves" };
        public static readonly Color[] SkinTones = Hexes("#f2d0b4", "#e3b48f", "#d9a37e", "#c68a60", "#a86f4a", "#8a5636", "#6b4029", "#4e2e1e");
        public static readonly Color[] HairColors = Hexes("#1f1a17", "#3b2a1e", "#5e3c24", "#8a5a32", "#b8874a", "#d9b56a", "#8f3b1e", "#9a9a96");
        public static readonly Color[] Cloth = Hexes("#3f6d9e", "#2e3f5c", "#b5402f", "#d8892b", "#f2c230", "#e8752a", "#5aa34a", "#3d5a3a",
                                                      "#7a4a2a", "#3c4654", "#8c8c86", "#e8e2d4");

        static Color[] Hexes(params string[] h) => Array.ConvertAll(h, Art.Hex);

        // Player slot defaults: the slot colours the HUD uses (orange, blue, green, red) on the shirt.
        public static Look Default(int slot)
        {
            int s = Mathf.Abs(slot) % 4;
            return new Look
            {
                Face = (byte)s, Hair = (byte)(1 + s % 3), Beard = (byte)(s == 0 ? 2 : s == 3 ? 1 : 0), Hat = (byte)(s == 0 ? 3 : s == 2 ? 2 : 0),
                Top = (byte)(s == 1 ? 2 : s == 2 ? 3 : 1), Gloves = (byte)(s % 2 == 0 ? 1 : 0),
                SkinTone = (byte)new[] { 2, 4, 1, 6 }[s], HairColor = (byte)new[] { 2, 0, 6, 1 }[s], TopColor = (byte)new[] { 3, 0, 6, 2 }[s],
                AccentColor = (byte)(s == 1 ? 1 : 4), LegsColor = 9, BootsColor = 8, GloveColor = 3, HatColor = (byte)(s == 0 ? 4 : 2),
            };
        }

        public static Look Random(System.Random r)
        {
            int P(int n) => r.Next(n);
            return new Look
            {
                Set = 1, Face = (byte)P(4), Hair = (byte)P(4), Beard = (byte)P(4), Hat = (byte)P(4), Top = (byte)P(4), Gloves = (byte)P(2),
                SkinTone = (byte)P(SkinTones.Length), HairColor = (byte)P(HairColors.Length), TopColor = (byte)P(Cloth.Length),
                AccentColor = (byte)(r.NextDouble() < 0.6 ? new[] { 4, 5, 0, 1 }[P(4)] : P(Cloth.Length)), LegsColor = (byte)new[] { 9, 1, 8, 7, 10 }[P(5)],
                BootsColor = (byte)new[] { 8, 9, 1, 7 }[P(4)], GloveColor = (byte)new[] { 3, 8, 4, 10 }[P(4)], HatColor = (byte)P(Cloth.Length),
            };
        }

        // Host-side check of a look sent by a client: everything back in range.
        public Look Clamped()
        {
            var l = this;
            l.Set = 1;
            l.Face %= 4; l.Hair %= 4; l.Beard %= 4; l.Hat %= 4; l.Top %= 4; l.Gloves %= 2; l.Spare = 0;
            l.SkinTone %= (byte)SkinTones.Length; l.HairColor %= (byte)HairColors.Length;
            l.TopColor %= (byte)Cloth.Length; l.AccentColor %= (byte)Cloth.Length; l.LegsColor %= (byte)Cloth.Length;
            l.BootsColor %= (byte)Cloth.Length; l.GloveColor %= (byte)Cloth.Length; l.HatColor %= (byte)Cloth.Length;
            return l;
        }

        // Saved on this PC as 16 hex bytes.
        const string Key = "pb.look";
        public static bool Saved => PlayerPrefs.HasKey(Key);
        public static Look Load(int slot)
        {
            var s = PlayerPrefs.GetString(Key, "");
            if (s.Length != 32) return Default(slot);
            var b = new byte[16];
            for (int i = 0; i < 16; i++) if (!byte.TryParse(s.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out b[i])) return Default(slot);
            return FromBytes(b).Clamped();
        }
        public void Save() { var b = Bytes(); PlayerPrefs.SetString(Key, BitConverter.ToString(b).Replace("-", "")); PlayerPrefs.Save(); }

        byte[] Bytes() => new[] { Set, Face, Hair, Beard, Hat, Top, Gloves, SkinTone, HairColor, TopColor, AccentColor, LegsColor, BootsColor, GloveColor, HatColor, Spare };
        static Look FromBytes(byte[] b) => new Look
        {
            Set = b[0], Face = b[1], Hair = b[2], Beard = b[3], Hat = b[4], Top = b[5], Gloves = b[6], SkinTone = b[7], HairColor = b[8],
            TopColor = b[9], AccentColor = b[10], LegsColor = b[11], BootsColor = b[12], GloveColor = b[13], HatColor = b[14], Spare = b[15],
        };

        public bool Equals(Look o) => ((ReadOnlySpan<byte>)Bytes()).SequenceEqual(o.Bytes());
        public override bool Equals(object o) => o is Look l && Equals(l);
        public override int GetHashCode() => BitConverter.ToString(Bytes()).GetHashCode();
        public override string ToString() => BitConverter.ToString(Bytes()).Replace("-", "");
    }

    // Our own dock worker model (Assets/Art/Worker/Worker.fbx, built by art-src/worker/build_worker.py): one rig, every part a
    // skinned mesh; this shows the parts a Look picks and paints them through one 4x4 palette texture (each face's UVs sit
    // in one cell, so recolouring is 16 pixels).
    public class Worker : MonoBehaviour
    {
        // Palette cells, as in build_worker.py SLOTS.
        enum Cell { Skin, SkinDark, Hair, Eye, Top, TopTrim, Accent, Reflect, Legs, LegsTrim, Boots, Sole, Gloves, Hat, HatTrim, Metal }
        static readonly string[] HairParts = { null, "Hair_buzz", "Hair_quiff", "Hair_curly" };
        static readonly string[] BeardParts = { null, "Beard_moustache", "Beard_full", "Beard_goatee" };
        static readonly string[] HatParts = { null, "Hat_cap", "Hat_beanie", "Hat_hardhat" };

        Texture2D tex;
        Material mat;
        SkinnedMeshRenderer[] parts;
        public Look Current { get; private set; }

        public static Worker Make(GameObject model, Transform parent)
        {
            var g = Instantiate(model, parent, false);
            g.name = "worker";
            g.transform.localRotation = Quaternion.Euler(0, 180, 0); // exported facing -Z, like the pack's people
            var w = g.AddComponent<Worker>();
            w.parts = g.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            w.tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "WorkerPalette" };
            if (Art.Toon != null) { w.mat = new Material(Art.Toon); w.mat.SetTexture("_TextureSample", w.tex); }
            else { w.mat = new Material(Art.Lit); w.mat.SetTexture("_BaseMap", w.tex); w.mat.SetColor("_BaseColor", Color.white); w.mat.SetFloat("_Smoothness", 0.1f); }
            foreach (var r in w.parts) { r.sharedMaterial = w.mat; r.updateWhenOffscreen = true; }
            return w;
        }

        public void Apply(Look l)
        {
            Current = l;
            bool overalls = l.Top == 2, vest = l.Top == 1, shirt = l.Top == 3;
            bool Shown(string n)
            {
                switch (n)
                {
                    case "Skin_Neck": case "Skin_Arm_Lower": case "Boots": return true;
                    case "Skin_Arm_Upper": case "Top_Tee": return !shirt;
                    case "Top_Shirt": case "Top_Shirt_Roll": return shirt;
                    case "Vest": return vest;
                    case "Legs_Trousers": case "Legs_Trousers_Buckle": return !overalls;
                    case "Legs_Overalls": case "Legs_Overalls_Buckles": return overalls;
                    case "Skin_Hands": return l.Gloves == 0;
                    case "Gloves": return l.Gloves != 0;
                }
                if (n == "Head_" + l.Face || n == "Eyes_" + l.Face || n == "Brows_" + l.Face || n == "Mouth_" + l.Face) return true;
                if (BeardParts[l.Beard] != null && n == BeardParts[l.Beard] + "_" + l.Face) return true;
                if (n == HatParts[l.Hat]) return true;
                // Under a hat any hair shows as a crop round the rim (the hat sits up off the head).
                return n == (l.Hat == 0 || l.Hair == 0 ? HairParts[l.Hair] : HairParts[1]);
            }
            foreach (var r in parts) r.enabled = Shown(r.name);

            Color skin = Look.SkinTones[l.SkinTone], top = Look.Cloth[l.TopColor], legs = Look.Cloth[l.LegsColor], hat = Look.Cloth[l.HatColor];
            var px = new Color[16];
            px[(int)Cell.Skin] = skin; px[(int)Cell.SkinDark] = Color.Lerp(skin, new Color(0.35f, 0.12f, 0.1f), 0.35f);
            px[(int)Cell.Hair] = Look.HairColors[l.HairColor]; px[(int)Cell.Eye] = Art.Hex("#1d1712");
            px[(int)Cell.Top] = top; px[(int)Cell.TopTrim] = top * 0.78f;
            px[(int)Cell.Accent] = Look.Cloth[l.AccentColor]; px[(int)Cell.Reflect] = Art.Hex("#dfe3e6");
            px[(int)Cell.Legs] = legs; px[(int)Cell.LegsTrim] = legs * 0.72f;
            px[(int)Cell.Boots] = Look.Cloth[l.BootsColor]; px[(int)Cell.Sole] = Art.Hex("#2a2422");
            px[(int)Cell.Gloves] = Look.Cloth[l.GloveColor]; px[(int)Cell.Hat] = hat; px[(int)Cell.HatTrim] = hat * 0.8f;
            px[(int)Cell.Metal] = Art.Hex("#a8adb2");
            // The toon ramp lights flat colours brighter than URP Lit (same gain as Art.Solid).
            float gain = Art.Toon != null ? Art.ToonGain : 1f;
            for (int i = 0; i < 16; i++) px[i] = new Color(px[i].r * gain, px[i].g * gain, px[i].b * gain, 1);
            tex.SetPixels(px); tex.Apply(false, false);
        }

        void OnDestroy() { if (tex != null) Destroy(tex); if (mat != null) Destroy(mat); }
    }
}
