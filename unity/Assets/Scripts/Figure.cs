using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BaseballGame
{
    /// <summary>
    /// A ballplayer on the field made from a character model: scaled to a fixed height, shirt in the
    /// team colour, and playing the model's own clips (Idle, Walk, Victory...) through a playable,
    /// so no Animator Controller asset is needed.
    /// The object sits one metre up, where the old capsule's centre was, with the feet on the grass.
    /// </summary>
    public class Figure : MonoBehaviour
    {
        const float Height = 1.85f;
        /// <summary>Turns the model if it was exported facing somewhere other than +Z.</summary>
        const float ModelYaw = 0f;
        static readonly Color Pants = new Color(0.93f, 0.93f, 0.95f);

        Animator _animator;
        AnimationClip[] _clips;
        PlayableGraph _graph;
        AnimationClipPlayable _playing;
        string _current;
        readonly List<Material> _shirts = new List<Material>();

        /// <summary>Top of the head, measured from this object's origin.</summary>
        public static float HeadTop => Height - 1f;

        public static Figure Create(Transform parent, string name, GameObject model, AnimationClip[] clips, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position + Vector3.up;
            var figure = go.AddComponent<Figure>();
            figure.Build(model, clips);
            return figure;
        }

        void Build(GameObject model, AnimationClip[] clips)
        {
            var body = Instantiate(model, transform, false);
            body.name = "Model";
            body.transform.localRotation = Quaternion.Euler(0, ModelYaw, 0);

            // Models come in at whatever size they were made; stand every one at the same height.
            var renderers = body.GetComponentsInChildren<Renderer>();
            var bounds = Measure(renderers);
            if (bounds.size.y > 0.01f) body.transform.localScale *= Height / bounds.size.y;
            bounds = Measure(renderers);
            body.transform.position += Vector3.up * (transform.position.y - 1f - bounds.min.y);

            foreach (var r in renderers)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                foreach (var m in r.materials)
                {
                    if (m.name.StartsWith("Shirt")) _shirts.Add(m);
                    else if (m.name.StartsWith("Pants")) m.color = Pants;
                }
            }

            _clips = clips;
            _animator = body.GetComponentInChildren<Animator>();
            if (_animator == null) _animator = body.AddComponent<Animator>();
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Play("Idle");
        }

        static Bounds Measure(Renderer[] renderers)
        {
            var bounds = new Bounds();
            for (int i = 0; i < renderers.Length; i++)
                if (i == 0) bounds = renderers[i].bounds;
                else bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        /// <summary>Plays the clip whose name ends with clipName ("Idle", "Walk"...); speed 1 is normal.</summary>
        public void Play(string clipName, float speed = 1f)
        {
            if (_clips == null) return;
            if (_current == clipName && _graph.IsValid())
            {
                _playing.SetSpeed(speed);
                return;
            }
            AnimationClip clip = null;
            foreach (var c in _clips)
                if (c.name.EndsWith(clipName)) { clip = c; break; }
            if (clip == null) return;
            PlayClip(clip, clipName, speed);
        }

        void PlayClip(AnimationClip clip, string key, float speed)
        {
            if (_graph.IsValid()) _graph.Destroy();
            _playing = AnimationPlayableUtilities.PlayClip(_animator, clip, out _graph);
            _playing.SetSpeed(speed);
            _current = key;
        }

        /// <summary>
        /// Plays a Mixamo baseball move (WorldArt.Move) when it's there and the model is humanoid;
        /// otherwise the model's own clip named fallback. True when the real move played.
        /// </summary>
        public bool PlayMove(string move, string fallback, float speed = 1f, float fallbackSpeed = -1f)
        {
            var clip = WorldArt.Move(move);
            if (clip != null && _animator != null && _animator.avatar != null && _animator.avatar.isHuman)
            {
                string key = "move:" + move;
                if (_current == key && _graph.IsValid()) { _playing.SetSpeed(speed); return true; }
                PlayClip(clip, key, speed);
                return true;
            }
            Play(fallback, fallbackSpeed < 0 ? speed : fallbackSpeed);
            return false;
        }

        public void SetUniform(Color color)
        {
            foreach (var m in _shirts) m.color = color;
        }

        /// <summary>Turns to look along a direction on the ground (in the field's space).</summary>
        public void Face(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > 0.0001f) transform.localRotation = Quaternion.LookRotation(direction);
        }

        // ---------- Baseball gear and moves ----------
        // The models only know generic clips (Idle, Walk, Punch, PickUp...). A bat in the right fist,
        // a glove on the left, and a swing made by turning the body around the hips are enough to read
        // as baseball from the broadcast camera.

        Transform _bat, _glove, _model;
        Transform _rightFist, _leftFist;
        float _swing = -1f;      // 0..1 while swinging, -1 otherwise
        float _swingSeconds = 0.35f;

        Transform Bone(string name)
        {
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t.name == name || t.name.EndsWith(name)) return t;
            return null;
        }

        static Transform Prop(PrimitiveType type, string name, Color color, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            r.material.color = color;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            go.transform.localScale = scale;
            return go.transform;
        }

        /// <summary>Gives the figure a bat (a hitter at the plate) or takes it away.</summary>
        public void HoldBat(bool on)
        {
            if (on && _bat == null)
            {
                _rightFist = _rightFist ?? Bone("Fist.R");
                _bat = Prop(PrimitiveType.Cylinder, "Bat", new Color(0.78f, 0.6f, 0.38f), new Vector3(0.07f, 0.42f, 0.07f));
                _bat.SetParent(transform, false);
            }
            if (_bat != null) _bat.gameObject.SetActive(on);
        }

        /// <summary>A glove on the left hand, for fielders.</summary>
        public void WearGlove(bool on)
        {
            if (on && _glove == null)
            {
                _leftFist = _leftFist ?? Bone("Fist.L");
                if (_leftFist == null) return;
                _glove = Prop(PrimitiveType.Sphere, "Glove", new Color(0.45f, 0.26f, 0.12f), new Vector3(0.2f, 0.24f, 0.12f));
                _glove.SetParent(_leftFist, false);
                _glove.localPosition = Vector3.zero;
                // Bones can be scaled by the import: keep the glove its real size.
                var s = _leftFist.lossyScale;
                _glove.localScale = new Vector3(0.2f / Mathf.Max(0.001f, s.x), 0.24f / Mathf.Max(0.001f, s.y), 0.12f / Mathf.Max(0.001f, s.z));
            }
            if (_glove != null) _glove.gameObject.SetActive(on);
        }

        /// <summary>Swings the bat: load back, turn through the ball, follow through. Seconds is the whole swing.</summary>
        public void Swing(float seconds = 0.35f)
        {
            _swingSeconds = Mathf.Max(0.1f, seconds);
            _swing = 0f;
            // A real swing (Mixamo) moves the body and hands itself; the made-up one turns the hips.
            _realSwing = PlayMove(WorldArt.Variant("swing", ++_swings), "Punch", 1.2f / Mathf.Max(0.3f, seconds / 0.35f), 1.6f);
        }

        bool _realSwing;
        static int _swings;
        Vector3 _batDir = Vector3.up;

        bool IsMovePlaying => _current != null && _current.StartsWith("move:");

        void LateUpdate()
        {
            if (_model == null) _model = transform.Find("Model");
            float yaw = 0f, phase = 0f;
            if (_swing >= 0f && _realSwing)
            {
                _swing += Time.deltaTime / _swingSeconds;
                phase = Mathf.Clamp01(_swing);
                if (_swing >= 2.2f) { _swing = -1f; _realSwing = false; PlayMove(WorldArt.Variant("stance", _swings), "Idle"); }
            }
            else if (_swing >= 0f)
            {
                _swing += Time.deltaTime / _swingSeconds;
                // Load (turn back a little), then whip through to a long follow-through.
                phase = Mathf.Clamp01(_swing);
                yaw = phase < 0.25f ? Mathf.Lerp(0, -25, phase / 0.25f) : Mathf.Lerp(-25, 110, Mathf.SmoothStep(0, 1, (phase - 0.25f) / 0.75f));
                if (_swing >= 1.6f) { _swing = -1f; Play("Idle"); }
                else if (_swing >= 1f) yaw = 110;
            }
            if (_model != null) _model.localRotation = Quaternion.Euler(0, ModelYaw + yaw, 0);

            if (_bat != null && _bat.gameObject.activeSelf)
            {
                var hand = _rightFist != null ? _rightFist.position : transform.TransformPoint(new Vector3(0.25f, 0.3f, 0.15f));
                // In the stance the bat stands up behind the head; through the swing it lies flat and whips round.
                var stance = new Vector3(-0.25f, 0.9f, -0.35f).normalized;
                var through = new Vector3(0f, 0.15f, 1f).normalized;
                float k = _swing < 0f ? 0f : phase < 0.25f ? 0f : Mathf.SmoothStep(0, 1, (phase - 0.25f) / 0.5f);
                var local = Vector3.Slerp(stance, through, k);
                var dir = (_model != null ? _model : transform).TransformDirection(local);
                if (_realSwing || IsMovePlaying)
                {
                    // With real moves the hands lead: the bat runs on from the bottom hand through the top one.
                    _leftFist = _leftFist ?? Bone("Fist.L");
                    if (_leftFist != null && _rightFist != null)
                    {
                        var grip = _rightFist.position - _leftFist.position;
                        if (grip.sqrMagnitude > 0.0004f) dir = Vector3.Slerp(_batDir, grip.normalized, 0.5f);
                    }
                }
                _batDir = dir;
                _bat.rotation = Quaternion.FromToRotation(Vector3.up, dir);
                _bat.position = hand + dir * 0.4f;
            }
        }

        void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }
    }
}
