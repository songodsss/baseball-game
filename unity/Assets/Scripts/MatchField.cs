using System;
using System.Collections;
using System.Collections.Generic;
using BaseballSim;
using UnityEngine;

namespace BaseballGame
{
    /// <summary>
    /// The 3D ballpark for the match screen, built from primitives: field, fielders, runners, ball
    /// and a broadcast camera. It only animates what a ReplayStep says happened.
    /// The art pass will swap the capsules for 3D anime-style character models.
    /// </summary>
    public class MatchField : MonoBehaviour
    {
        const float BaseDistance = 27.4f;
        const float FenceRadius = 110f;
        const float RunSecondsPerBase = 0.8f;

        static readonly Vector3 Mound = new Vector3(0, 0, 18.4f);
        static readonly Vector3 BattersBox = new Vector3(-1.2f, 0, 0.3f);
        static readonly Vector3 CameraPos = new Vector3(0, 14, -22);
        static readonly Vector3 CameraTarget = new Vector3(0, 0, 30);

        // P, C, 1B, 2B, SS, 3B, LF, CF, RF
        static readonly Vector3[] FielderSpots =
        {
            Mound, new Vector3(0, 0, -1.5f), new Vector3(22, 0, 25), new Vector3(10, 0, 36), new Vector3(-10, 0, 36),
            new Vector3(-22, 0, 25), new Vector3(-45, 0, 70), new Vector3(0, 0, 85), new Vector3(45, 0, 70),
        };

        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

        /// <summary>Playback speed multiplier.</summary>
        public float Speed = 1f;
        /// <summary>While true, animations end at once and the screen snaps to the result.</summary>
        public Func<bool> Skipping = () => false;
        public Player Mine;

        Camera _camera;
        Transform _ball;
        readonly List<Transform> _fielders = new List<Transform>();
        readonly Dictionary<Player, Transform> _runners = new Dictionary<Player, Transform>();
        Color _battingColor = Color.white;

        public static MatchField Build(Transform parent)
        {
            var go = new GameObject("MatchField");
            go.transform.SetParent(parent, false);
            var field = go.AddComponent<MatchField>();
            field.BuildWorld();
            return field;
        }

        public static Vector3 BasePosition(int b)
        {
            float h = BaseDistance / Mathf.Sqrt(2);
            switch (b)
            {
                case 0: return new Vector3(h, 0, h);
                case 1: return new Vector3(0, 0, 2 * h);
                case 2: return new Vector3(-h, 0, h);
                default: return Vector3.zero; // home
            }
        }

        // ---------- Building ----------

        void BuildWorld()
        {
            var camGo = new GameObject("MatchCamera", typeof(Camera));
            camGo.transform.SetParent(transform, false);
            _camera = camGo.GetComponent<Camera>();
            _camera.depth = 10;
            _camera.fieldOfView = 55;
            _camera.farClipPlane = 600;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
            ResetCamera();

            if (FindAnyObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Sun", typeof(Light));
                lightGo.transform.SetParent(transform, false);
                lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
                var light = lightGo.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.shadows = LightShadows.Soft;
            }

            var grass = Shape(PrimitiveType.Plane, "Grass", new Color(0.3f, 0.62f, 0.3f));
            grass.localScale = new Vector3(30, 1, 30);
            grass.localPosition = new Vector3(0, 0, 80);

            float side = BaseDistance + 6;
            var dirt = Shape(PrimitiveType.Cube, "Infield Dirt", new Color(0.72f, 0.52f, 0.34f));
            dirt.localScale = new Vector3(side, 0.02f, side);
            dirt.SetPositionAndRotation(BasePosition(1) * 0.5f + Vector3.up * 0.01f, Quaternion.Euler(0, 45, 0));
            var infieldGrass = Shape(PrimitiveType.Cube, "Infield Grass", new Color(0.33f, 0.66f, 0.33f));
            infieldGrass.localScale = new Vector3(BaseDistance - 6, 0.02f, BaseDistance - 6);
            infieldGrass.SetPositionAndRotation(BasePosition(1) * 0.5f + Vector3.up * 0.02f, Quaternion.Euler(0, 45, 0));

            var mound = Shape(PrimitiveType.Cylinder, "Mound", new Color(0.72f, 0.52f, 0.34f));
            mound.localScale = new Vector3(5.5f, 0.15f, 5.5f);
            mound.localPosition = Mound;

            for (int b = 0; b < 3; b++)
            {
                var bag = Shape(PrimitiveType.Cube, "Base " + (b + 1), Color.white);
                bag.localScale = new Vector3(0.6f, 0.12f, 0.6f);
                bag.SetPositionAndRotation(BasePosition(b) + Vector3.up * 0.06f, Quaternion.Euler(0, 45, 0));
            }
            var plate = Shape(PrimitiveType.Cube, "Home Plate", Color.white);
            plate.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            plate.SetPositionAndRotation(Vector3.up * 0.04f, Quaternion.Euler(0, 45, 0));

            // Foul lines and the outfield fence, -45° (left) to +45° (right).
            for (int sign = -1; sign <= 1; sign += 2)
            {
                var line = Shape(PrimitiveType.Cube, "Foul Line", Color.white);
                line.localScale = new Vector3(0.2f, 0.03f, FenceRadius);
                line.SetPositionAndRotation(Direction(sign * 45) * FenceRadius * 0.5f + Vector3.up * 0.03f, Quaternion.Euler(0, sign * 45, 0));
            }
            const int segments = 24;
            float segLength = FenceRadius * Mathf.PI / 2 / segments + 0.5f;
            for (int i = 0; i < segments; i++)
            {
                float angle = -45 + 90f * (i + 0.5f) / segments;
                var fence = Shape(PrimitiveType.Cube, "Fence", new Color(0.12f, 0.3f, 0.45f));
                fence.localScale = new Vector3(segLength, 3, 0.4f);
                fence.SetPositionAndRotation(Direction(angle) * FenceRadius + Vector3.up * 1.5f, Quaternion.Euler(0, angle, 0));
            }

            for (int i = 0; i < FielderSpots.Length; i++)
                _fielders.Add(Person("Fielder " + i, Color.gray, FielderSpots[i]));

            _ball = Shape(PrimitiveType.Sphere, "Ball", Color.white);
            _ball.localScale = Vector3.one * 0.45f;
            _ball.gameObject.SetActive(false);
        }

        Transform Shape(PrimitiveType type, string name, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            // Tints the built-in default material, so builds need no extra shaders or assets.
            go.GetComponent<Renderer>().material.color = color;
            return go.transform;
        }

        Transform Person(string name, Color color, Vector3 position)
        {
            var body = Shape(PrimitiveType.Capsule, name, color);
            body.localScale = new Vector3(0.9f, 1f, 0.9f);
            body.localPosition = position + Vector3.up;
            return body;
        }

        static Vector3 Direction(float degrees) =>
            new Vector3(Mathf.Sin(degrees * Mathf.Deg2Rad), 0, Mathf.Cos(degrees * Mathf.Deg2Rad));

        static void Tint(Transform t, Color color) => t.GetComponent<Renderer>().material.color = color;

        // ---------- State ----------

        public void SetTeamColors(Color batting, Color fielding)
        {
            _battingColor = batting;
            foreach (var f in _fielders) Tint(f, fielding);
        }

        /// <summary>Puts everyone where they stand before (or after) a play, with no animation.</summary>
        public void Snap(Player[] bases, Player batter)
        {
            for (int i = 0; i < _fielders.Count; i++) _fielders[i].localPosition = FielderSpots[i] + Vector3.up;
            _ball.gameObject.SetActive(false);
            ResetCamera();

            var keep = new HashSet<Player>();
            for (int b = 0; b < 3; b++)
                if (bases[b] != null)
                {
                    keep.Add(bases[b]);
                    Runner(bases[b]).localPosition = BasePosition(b) + Vector3.up;
                }
            if (batter != null)
            {
                keep.Add(batter);
                Runner(batter).localPosition = BattersBox + Vector3.up;
            }

            var gone = new List<Player>();
            foreach (var pair in _runners)
                if (!keep.Contains(pair.Key)) gone.Add(pair.Key);
            foreach (var p in gone)
            {
                Destroy(_runners[p].gameObject);
                _runners.Remove(p);
            }
        }

        Transform Runner(Player p)
        {
            if (_runners.TryGetValue(p, out var t))
            {
                t.localScale = new Vector3(0.9f, 1f, 0.9f);
                Tint(t, _battingColor);
                return t;
            }
            t = Person(p.Name, _battingColor, BattersBox);
            if (p == Mine)
            {
                // My player wears the team uniform like everyone else; a gold marker overhead picks them out.
                var marker = Shape(PrimitiveType.Sphere, "Mine Marker", Gold);
                marker.SetParent(t, false);
                marker.localPosition = new Vector3(0, 1.6f, 0);
                marker.localScale = new Vector3(0.45f, 0.4f, 0.45f);
            }
            _runners[p] = t;
            return t;
        }

        void ResetCamera()
        {
            _camera.transform.localPosition = CameraPos;
            _camera.transform.LookAt(transform.TransformPoint(CameraTarget));
        }

        // ---------- Animation ----------

        float Step => Time.deltaTime * Speed;

        public IEnumerator Pitch()
        {
            _ball.gameObject.SetActive(true);
            var from = Mound + new Vector3(0, 1.8f, 0);
            var to = new Vector3(0, 1f, 0.6f);
            for (float t = 0; t < 1 && !Skipping(); t += Step / 0.45f)
            {
                _ball.localPosition = Vector3.Lerp(from, to, t);
                yield return null;
            }
        }

        public IEnumerator Result(ReplayStep step)
        {
            var ev = step.Event;
            int running = 0;
            IEnumerator Track(IEnumerator routine)
            {
                running++;
                yield return routine;
                running--;
            }

            bool inPlay = ev.Outcome != Outcome.Strikeout && ev.Outcome != Outcome.Walk && ev.Outcome != Outcome.HitByPitch;
            if (inPlay) StartCoroutine(Track(BallInPlay(step)));
            else _ball.gameObject.SetActive(false);

            foreach (var move in step.Moves)
                if (move.To != move.From) StartCoroutine(Track(Run(move, ev.Outcome)));

            yield return null;
            while (running > 0 && !Skipping()) yield return null;
        }

        IEnumerator BallInPlay(ReplayStep step)
        {
            var rng = new System.Random(step.Index * 7919 + step.Event.AwayScore * 31 + step.Event.HomeScore);
            float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);
            float Gap(float min, float max) => Range(min, max) * (rng.NextDouble() < 0.5 ? -1 : 1);

            float angle, distance, height, seconds;
            switch (step.Event.Outcome)
            {
                case Outcome.GroundOut: angle = Range(-40, 40); distance = Range(22, 34); height = 0.4f; seconds = 0.7f; break;
                case Outcome.Single: angle = Range(-40, 40); distance = Range(45, 62); height = 4; seconds = 0.9f; break;
                case Outcome.LineOut: angle = Range(-35, 35); distance = Range(30, 55); height = 2.5f; seconds = 0.6f; break;
                case Outcome.FlyOut: angle = Range(-42, 42); distance = Range(60, 88); height = 28; seconds = 2f; break;
                case Outcome.Double: angle = Gap(20, 42); distance = Range(85, 100); height = 12; seconds = 1.4f; break;
                case Outcome.Triple: angle = Gap(36, 43); distance = 102; height = 14; seconds = 1.6f; break;
                default: angle = Range(-40, 40); distance = Range(118, 138); height = 38; seconds = 2.4f; break; // home run
            }

            var from = new Vector3(0, 1, 0.8f);
            var landing = Direction(angle) * distance;
            bool follow = height > 10;

            // The nearest fielder chases the ball (no chase over the fence).
            Transform chaser = null;
            if (distance < FenceRadius)
            {
                float best = float.MaxValue;
                foreach (var f in _fielders)
                {
                    float d = Vector3.Distance(f.localPosition, landing);
                    if (d < best) { best = d; chaser = f; }
                }
            }
            var chaserStart = chaser != null ? chaser.localPosition : Vector3.zero;
            var chaserEnd = landing + Vector3.up;

            for (float t = 0; t < 1 && !Skipping(); t += Step / seconds)
            {
                _ball.localPosition = Vector3.Lerp(from, landing, t) + Vector3.up * (4 * height * t * (1 - t));
                if (chaser != null) chaser.localPosition = Vector3.Lerp(chaserStart, chaserEnd, Mathf.SmoothStep(0, 1, t));
                if (follow)
                    _camera.transform.LookAt(transform.TransformPoint(Vector3.Lerp(CameraTarget, _ball.localPosition, 0.6f)));
                yield return null;
            }

            // Grounders go to first; anything else still in the park comes back to the infield.
            if (distance < FenceRadius)
            {
                var start = _ball.localPosition;
                var target = step.Event.Outcome == Outcome.GroundOut ? BasePosition(0) + Vector3.up : BasePosition(1) * 0.6f + Vector3.up;
                for (float t = 0; t < 1 && !Skipping(); t += Step / 0.5f)
                {
                    _ball.localPosition = Vector3.Lerp(start, target, t) + Vector3.up * (2 * t * (1 - t));
                    yield return null;
                }
            }
            _ball.gameObject.SetActive(false);
        }

        IEnumerator Run(RunnerMove move, Outcome outcome)
        {
            var runner = Runner(move.Runner);
            var path = new List<Vector3> { runner.localPosition };

            if (move.To == RunnerMove.Out)
            {
                // A strikeout ends at the plate; anyone else is put out partway to the next base.
                if (!(move.From == RunnerMove.BattersBox && outcome == Outcome.Strikeout))
                    path.Add(Vector3.Lerp(BasePosition(move.From), BasePosition(move.From + 1), 0.6f) + Vector3.up);
            }
            else
            {
                for (int b = move.From + 1; b <= move.To; b++) path.Add(BasePosition(b) + Vector3.up);
            }

            for (int i = 1; i < path.Count; i++)
                for (float t = 0; t < 1 && !Skipping(); t += Step / RunSecondsPerBase)
                {
                    runner.localPosition = Vector3.Lerp(path[i - 1], path[i], t);
                    yield return null;
                }

            if (move.To == RunnerMove.Out || move.To == RunnerMove.Scored)
                for (float t = 0; t < 1 && !Skipping(); t += Step / 0.4f)
                {
                    runner.localScale = new Vector3(0.9f, 1f, 0.9f) * (1 - t);
                    yield return null;
                }
        }
    }
}
