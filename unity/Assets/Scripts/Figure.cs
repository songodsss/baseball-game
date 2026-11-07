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
            if (_graph.IsValid()) _graph.Destroy();
            _playing = AnimationPlayableUtilities.PlayClip(_animator, clip, out _graph);
            _playing.SetSpeed(speed);
            _current = clipName;
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

        void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }
    }
}
