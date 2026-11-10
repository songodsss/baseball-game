using System.Collections.Generic;
using System.Linq;
using BaseballSim.Career;
using UnityEngine;

namespace BaseballGame
{
    /// <summary>
    /// My player's portrait, photographed from a 3D VRoid model put together from the parts of his look.
    /// The models are .vrm files exported from VRoid Studio into Assets/Resources/Avatar/ (UniVRM imports
    /// each as a prefab):
    ///   base_&lt;build&gt;   the whole player in uniform: slim, normal, big
    ///   hair_&lt;kind&gt;    the same player with another hairstyle; only the hair is taken
    ///   face_&lt;kind&gt;    the same player with other eyes and brows; only the face is taken
    /// Skin and hair are tinted in code, so the models are made with the lightest skin and light grey hair.
    /// Until a build's base is in, the look shows one of the drawn portraits with that build.
    /// </summary>
    public static class AvatarArt
    {
        const string Folder = "Avatar/";
        const int Width = 768, Height = 1024;
        /// <summary>A layer nothing else in the game draws on, so the photo has only the player in it.</summary>
        const int PhotoLayer = 30;

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        // The whole portraits drawn before looks existed, sorted by build.
        static readonly Dictionary<BodyBuild, string[]> Wholes = new Dictionary<BodyBuild, string[]>
        {
            { BodyBuild.Slim, new[] { "Portraits/player_02", "Portraits/pitcher_02", "Portraits/pitcher_03" } },
            { BodyBuild.Normal, new[] { "Portraits/player_01", "Portraits/player_04", "Portraits/pitcher_01" } },
            { BodyBuild.Big, new[] { "Portraits/player_03", "Portraits/pitcher_04" } },
        };

        /// <summary>True once the 3D base for this build is in.</summary>
        public static bool HasModels(BodyBuild build) => Prefab("base_" + Looks.BuildFile(build)) != null;

        /// <summary>Whether that hairstyle (index into Looks.Hairs) has its model.</summary>
        public static bool HasHair(int hair) => Part("hair_", Looks.Hairs[hair].File, out _);

        /// <summary>Whether that face (index into Looks.Eyes) has its model.</summary>
        public static bool HasFace(int eyes) => Part("face_", Looks.Eyes[eyes].File, out _);

        /// <summary>A part with no file is the base model's own, so it's always there and nothing is swapped.</summary>
        static bool Part(string prefix, string file, out GameObject prefab)
        {
            prefab = file == null ? null : Prefab(prefix + file);
            return file == null || prefab != null;
        }

        public static Sprite Portrait(PlayerLook look)
        {
            if (look == null) return null;
            if (Cache.TryGetValue(look.Key, out var cached) && cached != null) return cached;
            Sprite sprite = null;
            try { sprite = Photograph(look); }
            catch (System.Exception e) { Debug.LogWarning("Avatar photo failed: " + e.Message); }
            if (sprite != null) Remember(look.Key, sprite);
            sprite = sprite ?? Whole(look);
            Cache[look.Key] = sprite;
            return sprite;
        }

        // Flicking through looks in the picker takes a photo each time; only the latest few are kept.
        const int KeepPhotos = 12;
        static readonly List<string> Photos = new List<string>();

        static void Remember(string key, Sprite sprite)
        {
            Photos.Remove(key);
            Photos.Add(key);
            while (Photos.Count > KeepPhotos)
            {
                string old = Photos[0];
                Photos.RemoveAt(0);
                if (Cache.TryGetValue(old, out var s) && s != null)
                {
                    Object.Destroy(s.texture);
                    Object.Destroy(s);
                }
                Cache.Remove(old);
            }
        }

        static Sprite Whole(PlayerLook look)
        {
            var list = Wholes[look.Build];
            int hash = unchecked(look.Skin * 7 + look.Hair * 13 + look.HairColor * 5 + look.Eyes * 11 + look.Expression * 3);
            for (int i = 0; i < list.Length; i++)
            {
                var s = ArtLibrary.Get(list[(hash + i) % list.Length]);
                if (s != null) return s;
            }
            return null;
        }

        static GameObject Prefab(string name)
        {
            if (Prefabs.TryGetValue(name, out var p)) return p;
            p = Resources.Load<GameObject>(Folder + name);
            Prefabs[name] = p;
            return p;
        }

        // ---------- Putting the model together ----------

        static Sprite Photograph(PlayerLook look)
        {
            var basePrefab = Prefab("base_" + Looks.BuildFile(look.Build));
            if (basePrefab == null) return null;

            var stage = new GameObject("AvatarStage");
            stage.transform.position = new Vector3(0, -500, 0); // out of sight of every game camera
            var temps = new List<Object> { stage };
            try
            {
                var body = Object.Instantiate(basePrefab, stage.transform, false);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                StopScripts(body);

                // Hair and face come off their own models onto this body.
                if (Part("hair_", Looks.Hairs[look.Hair].File, out var hairPrefab) && hairPrefab != null)
                    Swap(body, hairPrefab, stage, temps, IsHair);
                if (Part("face_", Looks.Eyes[look.Eyes].File, out var facePrefab) && facePrefab != null)
                    Swap(body, facePrefab, stage, temps, IsFace);

                Tint(body, look, temps);
                Express(body, Looks.Expressions[look.Expression]);
                ArmsDown(body);
                SetLayer(stage, PhotoLayer);
                return Shoot(body, temps);
            }
            finally
            {
                foreach (var o in temps)
                    if (o != null) Object.DestroyImmediate(o);
            }
        }

        /// <summary>Nothing on the model (spring bones, the VRM runtime) runs while it's on the stage.</summary>
        static void StopScripts(GameObject go)
        {
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
            var animator = go.GetComponentInChildren<Animator>();
            if (animator != null) animator.enabled = false;
        }

        // VRoid exports the parts as their own objects: "Face", "Body" (with the scalp's back hair merged
        // in, so it stays) and "Hair" (the hairstyle, missing on the base, which has only that scalp).
        static bool IsHair(SkinnedMeshRenderer r) => r.name.StartsWith("Hair");

        static bool IsFace(SkinnedMeshRenderer r) => r.name.StartsWith("Face");

        /// <summary>
        /// Hides the body's own part and moves the donor model's part onto the body's skeleton, bone by
        /// bone by name. Bones only the donor has (hair strands) move over with their pose, hung from the
        /// nearest bone the body does have, so they follow its head.
        /// </summary>
        static void Swap(GameObject body, GameObject donorPrefab, GameObject stage, List<Object> temps, System.Func<SkinnedMeshRenderer, bool> part)
        {
            var donor = Object.Instantiate(donorPrefab, stage.transform, false);
            temps.Add(donor);
            StopScripts(donor);

            foreach (var r in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (part(r)) r.gameObject.SetActive(false);

            var bones = body.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
            foreach (var r in donor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(part).ToList())
            {
                var mapped = r.bones.Select(b => b == null ? null : Map(b, bones)).ToArray();
                if (r.rootBone != null) r.rootBone = Map(r.rootBone, bones);
                r.bones = mapped;
                r.transform.SetParent(body.transform, false);
                r.gameObject.SetActive(true);
            }
        }

        static Transform Map(Transform bone, Dictionary<string, Transform> bones)
        {
            if (bones.TryGetValue(bone.name, out var mine)) return mine;
            // Find the nearest ancestor the body has and hang the donor's bone there with its local pose.
            var parent = bone.parent;
            while (parent != null && !bones.ContainsKey(parent.name)) parent = parent.parent;
            if (parent == null) return bone;
            var chainTop = bone;
            while (chainTop.parent != parent) chainTop = chainTop.parent;
            if (!bones.ContainsKey(chainTop.name))
            {
                chainTop.SetParent(bones[parent.name], false);
                foreach (var t in chainTop.GetComponentsInChildren<Transform>(true))
                    if (!bones.ContainsKey(t.name)) bones[t.name] = t;
            }
            return bones.TryGetValue(bone.name, out mine) ? mine : bone;
        }

        static void Tint(GameObject body, PlayerLook look, List<Object> temps)
        {
            var skin = Hex(Looks.Skins[look.Skin].Tint);
            var hair = Hex(Looks.HairColors[look.HairColor].Hex);
            foreach (var r in body.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.materials; // instances, destroyed after the photo
                temps.AddRange(mats);
                foreach (var m in mats)
                {
                    if (m == null) continue;
                    Color? tint = m.name.Contains("_SKIN") ? skin
                        : m.name.Contains("_HAIR") ? hair
                        : m.name.Contains("FaceBrow") ? hair * 0.75f
                        : (Color?)null;
                    if (tint == null) continue;
                    MultiplyColor(m, "_Color", tint.Value);
                    MultiplyColor(m, "_ShadeColor", tint.Value);
                }
            }
        }

        static void MultiplyColor(Material m, string property, Color tint)
        {
            if (!m.HasProperty(property)) return;
            var c = m.GetColor(property);
            m.SetColor(property, new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a));
        }

        static void Express(GameObject body, (string Name, string Shape, float Weight) expression)
        {
            foreach (var r in body.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                var mesh = r.sharedMesh;
                if (mesh == null) continue;
                for (int i = 0; i < mesh.blendShapeCount; i++) r.SetBlendShapeWeight(i, 0);
                if (expression.Shape == null) continue;
                // VRoid names it "Fcl_ALL_Joy"; some exports add a prefix.
                for (int i = 0; i < mesh.blendShapeCount; i++)
                    if (mesh.GetBlendShapeName(i).EndsWith(expression.Shape))
                        r.SetBlendShapeWeight(i, expression.Weight * 100f);
            }
        }

        /// <summary>
        /// VRoid models come in a T-pose; let the arms hang at the sides with a slight bend at the elbow.
        /// Each bone is turned from where it points now to where it should point, so it doesn't matter
        /// which way the model faces or how its bones are rolled.
        /// </summary>
        static void ArmsDown(GameObject body)
        {
            var rig = Rig.Of(body);
            if (rig.Head == null) return;
            var fwd = rig.Forward;
            foreach (int side in new[] { -1, 1 })
            {
                var upper = side < 0 ? rig.LeftUpperArm : rig.RightUpperArm;
                var lower = side < 0 ? rig.LeftLowerArm : rig.RightLowerArm;
                var hand = side < 0 ? rig.LeftHand : rig.RightHand;
                if (upper == null || lower == null) continue;
                var outward = Vector3.ProjectOnPlane(lower.position - upper.position, Vector3.up).normalized;
                // Down, a little out from the body and a touch forward.
                Aim(upper, lower, (Vector3.down * 0.96f + outward * 0.22f + fwd * 0.05f).normalized);
                if (hand != null) Aim(lower, hand, (Vector3.down * 0.95f + outward * 0.08f + fwd * 0.28f).normalized);
            }
        }

        static void Aim(Transform bone, Transform child, Vector3 dir)
        {
            var now = child.position - bone.position;
            if (now.sqrMagnitude < 1e-8f) return;
            bone.rotation = Quaternion.FromToRotation(now, dir) * bone.rotation;
        }

        /// <summary>The bones the photo needs, from the humanoid avatar or else by VRoid's bone names.</summary>
        struct Rig
        {
            public Transform Head, Neck, Hips, LeftUpperArm, RightUpperArm, LeftLowerArm, RightLowerArm, LeftHand, RightHand;

            /// <summary>The way the model faces, worked out from its shoulders.</summary>
            public Vector3 Forward
            {
                get
                {
                    if (LeftUpperArm == null || RightUpperArm == null) return Vector3.forward;
                    var right = Vector3.ProjectOnPlane(RightUpperArm.position - LeftUpperArm.position, Vector3.up);
                    var f = Vector3.Cross(right, Vector3.up);
                    return f.sqrMagnitude < 1e-8f ? Vector3.forward : f.normalized;
                }
            }

            public static Rig Of(GameObject body)
            {
                var animator = body.GetComponentInChildren<Animator>(true);
                var all = body.GetComponentsInChildren<Transform>(true);
                Transform Bone(HumanBodyBones human, string vroid)
                {
                    Transform t = null;
                    if (animator != null && animator.isHuman) t = animator.GetBoneTransform(human);
                    return t != null ? t : all.FirstOrDefault(x => x.name == vroid);
                }
                return new Rig
                {
                    Head = Bone(HumanBodyBones.Head, "J_Bip_C_Head"),
                    Neck = Bone(HumanBodyBones.Neck, "J_Bip_C_Neck"),
                    Hips = Bone(HumanBodyBones.Hips, "J_Bip_C_Hips"),
                    LeftUpperArm = Bone(HumanBodyBones.LeftUpperArm, "J_Bip_L_UpperArm"),
                    RightUpperArm = Bone(HumanBodyBones.RightUpperArm, "J_Bip_R_UpperArm"),
                    LeftLowerArm = Bone(HumanBodyBones.LeftLowerArm, "J_Bip_L_LowerArm"),
                    RightLowerArm = Bone(HumanBodyBones.RightLowerArm, "J_Bip_R_LowerArm"),
                    LeftHand = Bone(HumanBodyBones.LeftHand, "J_Bip_L_Hand"),
                    RightHand = Bone(HumanBodyBones.RightHand, "J_Bip_R_Hand"),
                };
            }
        }

        static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        // ---------- The photo ----------

        /// <summary>
        /// Head and shoulders from the front, the face in the upper part of the picture. Shot once on black
        /// and once on white: where the two differ the model is see-through, which gives clean soft edges
        /// whatever the toon shader writes to alpha.
        /// </summary>
        static Sprite Shoot(GameObject body, List<Object> temps)
        {
            var rig = Rig.Of(body);
            var fwd = rig.Forward;
            // The top of the hair: the highest point of the meshes on the head (the body's own bounds
            // still hold the T-pose arms, which only widen, never raise, it).
            float top = float.MinValue;
            foreach (var r in body.GetComponentsInChildren<Renderer>(false)) top = Mathf.Max(top, r.bounds.max.y);
            Vector3 head = rig.Head != null ? rig.Head.position : body.transform.position + Vector3.up * 1.4f;
            if (top == float.MinValue || top < head.y) top = head.y + 0.2f;
            float neckY = rig.Neck != null ? rig.Neck.position.y : head.y - 0.08f;
            float headH = Mathf.Max(0.12f, top - neckY);
            top += headH * 0.10f;
            float bottom = neckY - headH * 1.45f; // down to the chest
            float frameH = top - bottom;

            var camGo = new GameObject("AvatarCamera");
            temps.Add(camGo);
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.cullingMask = 1 << PhotoLayer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 20;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50;
            cam.aspect = Width / (float)Height;
            float distance = frameH * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var target = new Vector3(head.x, (top + bottom) * 0.5f, head.z);
            camGo.transform.position = target + fwd * distance;
            camGo.transform.rotation = Quaternion.LookRotation(-fwd, Vector3.up);
            var right = Vector3.Cross(Vector3.up, fwd);

            var lightGo = new GameObject("AvatarLight");
            temps.Add(lightGo);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.cullingMask = 1 << PhotoLayer;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.97f, 0.93f);
            light.shadows = LightShadows.None;
            lightGo.transform.rotation = Quaternion.LookRotation(-fwd + Vector3.down * 0.4f + right * 0.3f);

            var rt = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
            cam.targetTexture = rt;
            var onBlack = Capture(cam, rt, Color.black);
            var onWhite = Capture(cam, rt, Color.white);
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);

            var pixels = new Color32[onBlack.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                var b = onBlack[i];
                var w = onWhite[i];
                // white - black = (1 - alpha) per channel; colour = black / alpha.
                float alpha = 1f - ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / (3f * 255f);
                alpha = Mathf.Clamp01(alpha);
                if (alpha < 0.004f) { pixels[i] = new Color32(0, 0, 0, 0); continue; }
                pixels[i] = new Color32(
                    (byte)Mathf.Clamp(b.r / alpha, 0, 255),
                    (byte)Mathf.Clamp(b.g / alpha, 0, 255),
                    (byte)Mathf.Clamp(b.b / alpha, 0, 255),
                    (byte)(alpha * 255));
            }
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), 100);
        }

        static Color32[] Capture(Camera cam, RenderTexture rt, Color background)
        {
            cam.backgroundColor = background;
            cam.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var read = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            read.Apply(false);
            RenderTexture.active = previous;
            var pixels = read.GetPixels32();
            Object.Destroy(read);
            return pixels;
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }
}
