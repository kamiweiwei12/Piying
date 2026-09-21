using UnityEngine;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;

namespace YingYun.Rhythm.View
{
    /// <summary>以分片皮影、鉚釘關節與剛性竹製操縱桿呈現操演；不參與判定。</summary>
    public sealed class ShadowPuppetPresenter : MonoBehaviour
    {
        private static readonly Vector2[] RodGripPoints =
        {
            new Vector2(-3.45f, 1.35f),
            new Vector2(-2.75f, 2.75f),
            new Vector2(3.45f, 1.35f),
            new Vector2(-3.10f, -2.65f),
            new Vector2(0.55f, -3.05f),
            new Vector2(3.10f, -2.65f)
        };

        private readonly LineRenderer[] _rods = new LineRenderer[6];
        private readonly Transform[] _rodTargets = new Transform[6];
        private readonly float[] _rodDrive = new float[6];
        private readonly bool[] _heldRods = new bool[6];
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
        public int RodCount => _rods.Length;
        public int StringCount => RodCount;
        public float LeftUpperArmRotation => _leftUpperArmJoint == null ? 0f : _leftUpperArmJoint.localEulerAngles.z;
        public float LeftForearmRotation => _leftForearmJoint == null ? 0f : _leftForearmJoint.localEulerAngles.z;
        public float RightUpperArmRotation => _rightUpperArmJoint == null ? 0f : _rightUpperArmJoint.localEulerAngles.z;
        public float RightForearmRotation => _rightForearmJoint == null ? 0f : _rightForearmJoint.localEulerAngles.z;
        public float LeftThighRotation => _leftThighJoint == null ? 0f : _leftThighJoint.localEulerAngles.z;
        public float LeftShinRotation => _leftShinJoint == null ? 0f : _leftShinJoint.localEulerAngles.z;
        public float RightThighRotation => _rightThighJoint == null ? 0f : _rightThighJoint.localEulerAngles.z;
        public float RightShinRotation => _rightShinJoint == null ? 0f : _rightShinJoint.localEulerAngles.z;
        public float HeadRotation => _headJoint == null ? 0f : _headJoint.localEulerAngles.z;
        public float TorsoRotation => _torsoJoint == null ? 0f : _torsoJoint.localEulerAngles.z;
        public Vector3 PelvisPosition => _torsoJoint == null ? Vector3.zero : _torsoJoint.localPosition;
        public float GetRodDrive(int lane) => _rodDrive[lane];
        public Vector3 GetRodGripPosition(int lane) => _rods[lane] == null ? Vector3.zero : _rods[lane].GetPosition(0);
        public float GetStringTension(int lane) => GetRodDrive(lane);

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Begin()
        {
            EnsureInitialized();
            _evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            _songTime = double.NegativeInfinity;
            for (int lane = 0; lane < _heldRods.Length; lane++)
            {
                _heldRods[lane] = false;
            }
            ApplyPose(default);
        }

        public void Tick(double songTimeSec)
        {
            EnsureInitialized();
            _songTime = songTimeSec;
            PuppetPose pose = _evaluator.Evaluate(songTimeSec);
            ApplyPose(pose);
            UpdateRods();
        }

        /// <summary>每次原始按鍵都立即驅動對應竹桿與關節，與判定結果解耦。</summary>
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
                SetHeldRods(result.RequiredLanesMask, true);
                return;
            }

            if (result.EventKind == JudgmentEventKind.NoteJudged)
            {
                _evaluator.ReleaseHold(result.RequiredLanesMask, _songTime);
                SetHeldRods(result.RequiredLanesMask, false);
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

            CreateSprite("Warm Backlight", _visualRoot, Vector3.zero, new Vector2(5.4f, 6.15f),
                new Color(1f, 0.73f, 0.32f, 0.22f), -7, _circleSprite);
            CreateSprite("Translucent Paper Screen", _visualRoot, Vector3.zero, new Vector2(4.65f, 5.55f),
                new Color(1f, 0.88f, 0.60f, 0.88f), -6, _circleSprite);
            BuildStageFrame();

            var puppetRoot = new GameObject("Joint Pelvis");
            puppetRoot.transform.SetParent(_visualRoot, false);
            puppetRoot.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            _torsoJoint = puppetRoot.transform;

            CreateSprite("Torso", _torsoJoint, new Vector3(0f, 0.68f, 0f), new Vector2(0.92f, 1.48f),
                ShadowColor(), 3, _circleSprite);
            CreateSprite("Waist Ornament", _torsoJoint, new Vector3(0f, 0.12f, -0.01f), new Vector2(1.02f, 0.20f),
                AccentColor(), 4, _squareSprite);
            CreateSprite("Robe Skirt", _torsoJoint, new Vector3(0f, -0.28f, 0f), new Vector2(1.28f, 0.72f),
                ShadowColor(), 3, _circleSprite);
            CreateSprite("Robe Hem", _torsoJoint, new Vector3(0f, -0.58f, -0.01f), new Vector2(1.36f, 0.14f),
                AccentColor(), 4, _squareSprite);
            CreateSprite("Chest Cutout", _torsoJoint, new Vector3(0f, 0.78f, -0.01f), new Vector2(0.48f, 0.18f),
                new Color(0.78f, 0.28f, 0.055f, 0.72f), 4, _circleSprite);

            _headJoint = CreateJoint("Joint Neck", _torsoJoint, new Vector3(0f, 1.48f, 0f));
            Transform head = CreateSprite("Head", _headJoint, new Vector3(0f, 0.28f, 0f), new Vector2(0.62f, 0.72f),
                ShadowColor(), 5, _circleSprite);
            CreateSprite("Head Crown", _headJoint, new Vector3(0f, 0.70f, 0f), new Vector2(0.78f, 0.18f),
                AccentColor(), 6, _squareSprite);
            Transform crownWingLeft = CreateSprite("Crown Wing Left", _headJoint, new Vector3(-0.43f, 0.78f, 0f), new Vector2(0.68f, 0.10f),
                AccentColor(), 6, _squareSprite);
            crownWingLeft.localRotation = Quaternion.Euler(0f, 0f, 12f);
            Transform crownWingRight = CreateSprite("Crown Wing Right", _headJoint, new Vector3(0.43f, 0.78f, 0f), new Vector2(0.68f, 0.10f),
                AccentColor(), 6, _squareSprite);
            crownWingRight.localRotation = Quaternion.Euler(0f, 0f, -12f);
            CreateSprite("Crown Jewel", _headJoint, new Vector3(0f, 0.86f, 0f), new Vector2(0.18f, 0.18f),
                new Color(0.92f, 0.48f, 0.08f, 0.98f), 7, _circleSprite);

            _leftUpperArmJoint = CreateJoint("Joint Left Shoulder", _torsoJoint, new Vector3(-0.48f, 1.12f, 0f));
            CreateLimb("Left Upper Arm", _leftUpperArmJoint, 0.82f, 0.20f, 5);
            CreateSprite("Left Flowing Sleeve", _leftUpperArmJoint, new Vector3(-0.12f, -0.48f, 0f), new Vector2(0.48f, 0.82f),
                ShadowColor(), 4, _circleSprite);
            _leftForearmJoint = CreateJoint("Joint Left Elbow", _leftUpperArmJoint, new Vector3(0f, -0.82f, 0f));
            Transform leftHand = CreateLimb("Left Forearm", _leftForearmJoint, 0.72f, 0.17f, 6);

            _rightUpperArmJoint = CreateJoint("Joint Right Shoulder", _torsoJoint, new Vector3(0.48f, 1.12f, 0f));
            CreateLimb("Right Upper Arm", _rightUpperArmJoint, 0.82f, 0.20f, 5);
            CreateSprite("Right Flowing Sleeve", _rightUpperArmJoint, new Vector3(0.12f, -0.48f, 0f), new Vector2(0.48f, 0.82f),
                ShadowColor(), 4, _circleSprite);
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

            _rodTargets[0] = leftHand;
            _rodTargets[1] = head;
            _rodTargets[2] = rightHand;
            _rodTargets[3] = leftFoot;
            _rodTargets[4] = _torsoJoint;
            _rodTargets[5] = rightFoot;
            BuildRods();
        }

        private Transform CreateLimb(string name, Transform joint, float length, float width, int sortingOrder)
        {
            CreateSprite(name, joint, new Vector3(0f, -length * 0.5f, 0f), new Vector2(width, length),
                ShadowColor(), sortingOrder, _circleSprite);
            return CreateSprite(name + " End", joint, new Vector3(0f, -length, 0f), new Vector2(width * 1.45f, width * 1.45f),
                AccentColor(), sortingOrder + 1, _circleSprite);
        }

        private void BuildStageFrame()
        {
            Color wood = new Color(0.28f, 0.055f, 0.025f, 0.98f);
            CreateSprite("Stage Header", _visualRoot, new Vector3(0f, 3.02f, 0f), new Vector2(5.65f, 0.22f), wood, -4, _squareSprite);
            CreateSprite("Stage Left Post", _visualRoot, new Vector3(-2.72f, 0f, 0f), new Vector2(0.20f, 6.15f), wood, -4, _squareSprite);
            CreateSprite("Stage Right Post", _visualRoot, new Vector3(2.72f, 0f, 0f), new Vector2(0.20f, 6.15f), wood, -4, _squareSprite);
            CreateSprite("Stage Foot", _visualRoot, new Vector3(0f, -3.02f, 0f), new Vector2(5.65f, 0.24f), wood, -4, _squareSprite);

            Color scenery = new Color(0.30f, 0.075f, 0.035f, 0.22f);
            CreateSprite("Scenery Left Mountain", _visualRoot, new Vector3(-1.72f, -2.25f, 0f), new Vector2(1.75f, 0.52f), scenery, -3, _circleSprite);
            CreateSprite("Scenery Right Mountain", _visualRoot, new Vector3(1.55f, -2.32f, 0f), new Vector2(2.25f, 0.44f), scenery, -3, _circleSprite);
        }

        private void BuildRods()
        {
            for (int lane = 0; lane < _rods.Length; lane++)
            {
                var lineObject = new GameObject($"Bamboo Control Rod {lane}");
                lineObject.transform.SetParent(_visualRoot, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 2;
                line.startWidth = 0.075f;
                line.endWidth = 0.055f;
                line.material = _lineMaterial;
                line.sortingOrder = 9;
                _rods[lane] = line;

                CreateSprite($"Bamboo Grip {lane}", _visualRoot, RodGripPoints[lane], new Vector2(0.46f, 0.12f),
                    new Color(0.38f, 0.16f, 0.055f, 0.98f), 10, _circleSprite);
            }

            UpdateRods();
        }

        private void ApplyPose(PuppetPose pose)
        {
            double leftLead = HoldOscillation(0);
            double lift = HoldOscillation(1);
            double rightLead = HoldOscillation(2);
            double leftStep = HoldOscillation(3);
            double sink = HoldOscillation(4);
            double rightStep = HoldOscillation(5);
            SetRotation(_headJoint, pose.Head + (lift * 1.6d));
            SetRotation(_torsoJoint, pose.Torso + (sink * 1.4d));
            SetRotation(_leftUpperArmJoint, pose.LeftUpperArm + (leftLead * 2.0d));
            SetRotation(_leftForearmJoint, pose.LeftForearm + (leftLead * 3.2d));
            SetRotation(_rightUpperArmJoint, pose.RightUpperArm - (rightLead * 2.0d));
            SetRotation(_rightForearmJoint, pose.RightForearm - (rightLead * 3.2d));
            SetRotation(_leftThighJoint, pose.LeftThigh + (leftStep * 1.8d));
            SetRotation(_leftShinJoint, pose.LeftShin + (leftStep * 2.8d));
            SetRotation(_rightThighJoint, pose.RightThigh - (rightStep * 1.8d));
            SetRotation(_rightShinJoint, pose.RightShin - (rightStep * 2.8d));
            _rodDrive[0] = (float)pose.LeftHandTension;
            _rodDrive[1] = (float)pose.HeadTension;
            _rodDrive[2] = (float)pose.RightHandTension;
            _rodDrive[3] = (float)pose.LeftFootTension;
            _rodDrive[4] = (float)pose.TorsoTension;
            _rodDrive[5] = (float)pose.RightFootTension;
        }

        private double HoldOscillation(int lane)
        {
            return _heldRods[lane]
                ? System.Math.Sin((_songTime * System.Math.PI * 4d) + (lane * 0.65d))
                : 0d;
        }

        private void UpdateRods()
        {
            if (_visualRoot == null)
            {
                return;
            }

            for (int lane = 0; lane < _rods.Length; lane++)
            {
                Vector3 restGrip = RodGripPoints[lane];
                Vector3 target = _visualRoot.InverseTransformPoint(_rodTargets[lane].position);
                Vector3 direction = (target - restGrip).normalized;
                Vector3 normal = new Vector3(-direction.y, direction.x, 0f);
                float holdPulse = _heldRods[lane]
                    ? (0.10f * Mathf.Sin((float)(_songTime * Mathf.PI * 5.0d) + lane))
                    : 0f;
                Vector3 grip = restGrip + (direction * ((_rodDrive[lane] * 0.18f) + holdPulse));
                grip += normal * (holdPulse * 0.35f);
                _rods[lane].SetPosition(0, grip);
                _rods[lane].SetPosition(1, target);
                Color color = Color.Lerp(new Color(0.30f, 0.13f, 0.045f, 0.48f), new Color(0.78f, 0.34f, 0.07f, 1f), _rodDrive[lane]);
                _rods[lane].startColor = color;
                _rods[lane].endColor = color;
                _rods[lane].startWidth = Mathf.Lerp(0.065f, 0.095f, _rodDrive[lane]) + (_heldRods[lane] ? 0.012f : 0f);
            }
        }

        private void SetHeldRods(int lanesMask, bool held)
        {
            for (int lane = 0; lane < _heldRods.Length; lane++)
            {
                if ((lanesMask & (1 << lane)) != 0)
                {
                    _heldRods[lane] = held;
                }
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
