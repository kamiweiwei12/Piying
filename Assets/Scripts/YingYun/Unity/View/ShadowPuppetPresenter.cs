using UnityEngine;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;

namespace YingYun.Rhythm.View
{
    /// <summary>以關節旋轉與操偶線張力呈現判定回饋；不參與判定。</summary>
    public sealed class ShadowPuppetPresenter : MonoBehaviour
    {
        private static readonly Vector2[] ControlPoints =
        {
            new Vector2(-1.85f, 3.25f),
            new Vector2(-0.35f, 3.45f),
            new Vector2(1.85f, 3.25f),
            new Vector2(-1.25f, 3.15f),
            new Vector2(0.35f, 3.35f),
            new Vector2(1.25f, 3.15f)
        };

        private readonly LineRenderer[] _strings = new LineRenderer[6];
        private readonly Transform[] _stringTargets = new Transform[6];
        private readonly float[] _tensions = new float[6];
        private PuppetPoseEvaluator _evaluator;
        private Transform _visualRoot;
        private Transform _torsoJoint;
        private Transform _headJoint;
        private Transform _leftUpperArmJoint;
        private Transform _leftForearmJoint;
        private Transform _rightUpperArmJoint;
        private Transform _rightForearmJoint;
        private Transform _leftThighJoint;
        private Transform _leftShinJoint;
        private Transform _rightThighJoint;
        private Transform _rightShinJoint;
        private Sprite _squareSprite;
        private Sprite _circleSprite;
        private Texture2D _squareTexture;
        private Texture2D _circleTexture;
        private Material _lineMaterial;
        private double _songTime;

        public int JointCount => 10;
        public int StringCount => _strings.Length;
        public float LeftUpperArmRotation => _leftUpperArmJoint == null ? 0f : _leftUpperArmJoint.localEulerAngles.z;
        public float RightUpperArmRotation => _rightUpperArmJoint == null ? 0f : _rightUpperArmJoint.localEulerAngles.z;
        public float RightThighRotation => _rightThighJoint == null ? 0f : _rightThighJoint.localEulerAngles.z;
        public float GetStringTension(int lane) => _tensions[lane];

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Begin()
        {
            EnsureInitialized();
            _evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            _songTime = double.NegativeInfinity;
            ApplyPose(default);
        }

        public void Tick(double songTimeSec)
        {
            EnsureInitialized();
            _songTime = songTimeSec;
            PuppetPose pose = _evaluator.Evaluate(songTimeSec);
            ApplyPose(pose);
            UpdateStrings();
        }

        /// <summary>每次原始按鍵都立即拉動對應操偶線，與判定結果解耦。</summary>
        public void OnInput(HitInput input)
        {
            EnsureInitialized();
            int laneMask = 1 << input.Lane;
            if (input.Kind == InputKind.Press)
            {
                _evaluator.Trigger(laneMask, input.InputTimeSec);
            }
            else
            {
                _evaluator.ReleaseHold(laneMask, input.InputTimeSec);
            }
        }

        public void OnJudged(JudgmentResult result)
        {
            EnsureInitialized();
            if (result.EventKind == JudgmentEventKind.HoldStarted)
            {
                _evaluator.BeginHold(result.RequiredLanesMask, _songTime);
                return;
            }

            if (result.EventKind == JudgmentEventKind.NoteJudged)
            {
                _evaluator.ReleaseHold(result.RequiredLanesMask, _songTime);
                if (result.Grade == JudgmentGrade.Miss)
                {
                    _evaluator.Fail(_songTime);
                }
            }
        }

        private void EnsureInitialized()
        {
            if (_visualRoot != null)
            {
                return;
            }

            _evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            BuildPuppetStage();
        }

        private void BuildPuppetStage()
        {
            _squareTexture = BuildSolidTexture("M6 Square");
            _circleTexture = BuildCircleTexture(64);
            _squareSprite = Sprite.Create(_squareTexture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
            _circleSprite = Sprite.Create(_circleTexture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));

            var root = new GameObject("M6 Shadow Puppet Stage");
            root.transform.SetParent(transform, false);
            _visualRoot = root.transform;

            CreateSprite("Warm Backlit Screen", _visualRoot, Vector3.zero, new Vector2(4.7f, 5.65f),
                new Color(1f, 0.73f, 0.32f, 0.22f), -7, _circleSprite);
            CreateSprite("Paper Screen", _visualRoot, Vector3.zero, new Vector2(4.05f, 5.15f),
                new Color(1f, 0.88f, 0.60f, 0.88f), -6, _circleSprite);

            var puppetRoot = new GameObject("Joint Pelvis");
            puppetRoot.transform.SetParent(_visualRoot, false);
            puppetRoot.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            _torsoJoint = puppetRoot.transform;

            CreateSprite("Torso", _torsoJoint, new Vector3(0f, 0.68f, 0f), new Vector2(0.92f, 1.48f),
                ShadowColor(), 3, _circleSprite);
            CreateSprite("Waist Ornament", _torsoJoint, new Vector3(0f, 0.12f, -0.01f), new Vector2(1.02f, 0.20f),
                AccentColor(), 4, _squareSprite);

            _headJoint = CreateJoint("Joint Neck", _torsoJoint, new Vector3(0f, 1.48f, 0f));
            Transform head = CreateSprite("Head", _headJoint, new Vector3(0f, 0.28f, 0f), new Vector2(0.62f, 0.72f),
                ShadowColor(), 5, _circleSprite);
            CreateSprite("Head Crown", _headJoint, new Vector3(0f, 0.70f, 0f), new Vector2(0.78f, 0.18f),
                AccentColor(), 6, _squareSprite);

            _leftUpperArmJoint = CreateJoint("Joint Left Shoulder", _torsoJoint, new Vector3(-0.48f, 1.12f, 0f));
            CreateLimb("Left Upper Arm", _leftUpperArmJoint, 0.82f, 0.20f, 5);
            _leftForearmJoint = CreateJoint("Joint Left Elbow", _leftUpperArmJoint, new Vector3(0f, -0.82f, 0f));
            Transform leftHand = CreateLimb("Left Forearm", _leftForearmJoint, 0.72f, 0.17f, 6);

            _rightUpperArmJoint = CreateJoint("Joint Right Shoulder", _torsoJoint, new Vector3(0.48f, 1.12f, 0f));
            CreateLimb("Right Upper Arm", _rightUpperArmJoint, 0.82f, 0.20f, 5);
            _rightForearmJoint = CreateJoint("Joint Right Elbow", _rightUpperArmJoint, new Vector3(0f, -0.82f, 0f));
            Transform rightHand = CreateLimb("Right Forearm", _rightForearmJoint, 0.72f, 0.17f, 6);

            _leftThighJoint = CreateJoint("Joint Left Hip", _torsoJoint, new Vector3(-0.27f, 0.08f, 0f));
            CreateLimb("Left Thigh", _leftThighJoint, 0.88f, 0.24f, 3);
            _leftShinJoint = CreateJoint("Joint Left Knee", _leftThighJoint, new Vector3(0f, -0.88f, 0f));
            Transform leftFoot = CreateLimb("Left Shin", _leftShinJoint, 0.82f, 0.19f, 4);

            _rightThighJoint = CreateJoint("Joint Right Hip", _torsoJoint, new Vector3(0.27f, 0.08f, 0f));
            CreateLimb("Right Thigh", _rightThighJoint, 0.88f, 0.24f, 3);
            _rightShinJoint = CreateJoint("Joint Right Knee", _rightThighJoint, new Vector3(0f, -0.88f, 0f));
            Transform rightFoot = CreateLimb("Right Shin", _rightShinJoint, 0.82f, 0.19f, 4);

            CreateJointPin(_leftUpperArmJoint, 7);
            CreateJointPin(_leftForearmJoint, 7);
            CreateJointPin(_rightUpperArmJoint, 7);
            CreateJointPin(_rightForearmJoint, 7);
            CreateJointPin(_leftThighJoint, 7);
            CreateJointPin(_leftShinJoint, 7);
            CreateJointPin(_rightThighJoint, 7);
            CreateJointPin(_rightShinJoint, 7);

            _stringTargets[0] = leftHand;
            _stringTargets[1] = head;
            _stringTargets[2] = rightHand;
            _stringTargets[3] = leftFoot;
            _stringTargets[4] = _torsoJoint;
            _stringTargets[5] = rightFoot;
            BuildStrings();
        }

        private Transform CreateLimb(string name, Transform joint, float length, float width, int sortingOrder)
        {
            CreateSprite(name, joint, new Vector3(0f, -length * 0.5f, 0f), new Vector2(width, length),
                ShadowColor(), sortingOrder, _circleSprite);
            return CreateSprite(name + " End", joint, new Vector3(0f, -length, 0f), new Vector2(width * 1.45f, width * 1.45f),
                AccentColor(), sortingOrder + 1, _circleSprite);
        }

        private void BuildStrings()
        {
            for (int lane = 0; lane < _strings.Length; lane++)
            {
                var lineObject = new GameObject($"Control String {lane}");
                lineObject.transform.SetParent(_visualRoot, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 3;
                line.startWidth = 0.025f;
                line.endWidth = 0.018f;
                line.material = _lineMaterial;
                line.sortingOrder = 12;
                _strings[lane] = line;

                CreateSprite($"Control Handle {lane}", _visualRoot, ControlPoints[lane], new Vector2(0.40f, 0.07f),
                    new Color(0.35f, 0.10f, 0.05f, 0.95f), 13, _squareSprite);
            }

            UpdateStrings();
        }

        private void ApplyPose(PuppetPose pose)
        {
            SetRotation(_headJoint, pose.Head);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_leftUpperArmJoint, pose.LeftUpperArm);
            SetRotation(_leftForearmJoint, pose.LeftForearm);
            SetRotation(_rightUpperArmJoint, pose.RightUpperArm);
            SetRotation(_rightForearmJoint, pose.RightForearm);
            SetRotation(_leftThighJoint, pose.LeftThigh);
            SetRotation(_leftShinJoint, pose.LeftShin);
            SetRotation(_rightThighJoint, pose.RightThigh);
            SetRotation(_rightShinJoint, pose.RightShin);
            _tensions[0] = (float)pose.LeftHandTension;
            _tensions[1] = (float)pose.HeadTension;
            _tensions[2] = (float)pose.RightHandTension;
            _tensions[3] = (float)pose.LeftFootTension;
            _tensions[4] = (float)pose.TorsoTension;
            _tensions[5] = (float)pose.RightFootTension;
        }

        private void UpdateStrings()
        {
            if (_visualRoot == null)
            {
                return;
            }

            for (int lane = 0; lane < _strings.Length; lane++)
            {
                Vector3 start = ControlPoints[lane];
                Vector3 end = _visualRoot.InverseTransformPoint(_stringTargets[lane].position);
                float slack = (1f - _tensions[lane]) * 0.30f;
                Vector3 middle = Vector3.Lerp(start, end, 0.5f) + (Vector3.down * slack);
                _strings[lane].SetPosition(0, start);
                _strings[lane].SetPosition(1, middle);
                _strings[lane].SetPosition(2, end);
                Color color = Color.Lerp(new Color(0.28f, 0.12f, 0.06f, 0.28f), new Color(0.55f, 0.08f, 0.03f, 0.95f), _tensions[lane]);
                _strings[lane].startColor = color;
                _strings[lane].endColor = color;
                _strings[lane].startWidth = Mathf.Lerp(0.018f, 0.045f, _tensions[lane]);
            }
        }

        private Transform CreateJoint(string name, Transform parent, Vector3 localPosition)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = localPosition;
            return joint;
        }

        private void CreateJointPin(Transform joint, int sortingOrder)
        {
            CreateSprite("Joint Pin", joint, Vector3.zero, new Vector2(0.14f, 0.14f), AccentColor(), sortingOrder, _circleSprite);
        }

        private static Transform CreateSprite(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector2 scale,
            Color color,
            int sortingOrder,
            Sprite sprite)
        {
            var visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return visual.transform;
        }

        private static void SetRotation(Transform target, double degrees)
        {
            if (target != null)
            {
                target.localRotation = Quaternion.Euler(0f, 0f, (float)degrees);
            }
        }

        private static Color ShadowColor() => new Color(0.12f, 0.025f, 0.018f, 0.96f);
        private static Color AccentColor() => new Color(0.70f, 0.12f, 0.045f, 0.98f);

        private static Texture2D BuildSolidTexture(string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = name };
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D BuildCircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "M6 Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float radius = (size - 1) * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    pixels[(y * size) + x] = new Color32(255, 255, 255, distance <= radius ? (byte)255 : (byte)0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null) Destroy(_lineMaterial);
            if (_squareSprite != null) Destroy(_squareSprite);
            if (_circleSprite != null) Destroy(_circleSprite);
            if (_squareTexture != null) Destroy(_squareTexture);
            if (_circleTexture != null) Destroy(_circleTexture);
        }
    }
}
