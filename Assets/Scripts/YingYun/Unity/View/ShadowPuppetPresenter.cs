using System;
using UnityEngine;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Puppet.V2;

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
        private readonly SpriteRenderer[] _rodGrips = new SpriteRenderer[6];
        private readonly Transform[] _rodTargets = new Transform[6];
        private readonly float[] _rodDrive = new float[6];
        private readonly bool[] _heldRods = new bool[6];
        private PuppetPoseEvaluator _evaluator;
        private DancePlayback _dancePlayback;
        private Transform _visualRoot;
        private Transform _pelvisJoint;
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
        private Transform _leftSleeve;
        private Transform _rightSleeve;
        private Transform _leftSleeveTail;
        private Transform _rightSleeveTail;
        private Transform _leftWristJoint;
        private Transform _rightWristJoint;
        private Transform _leftFingerJoint;
        private Transform _rightFingerJoint;
        private Transform _leftPointFinger;
        private Transform _rightPointFinger;
        private Transform _leftAnkleJoint;
        private Transform _rightAnkleJoint;
        private Transform _robeSkirt;
        private Transform _robeHem;
        private Transform _leftFootPlate;
        private Transform _rightFootPlate;
        private Transform _pelvisArt;
        private Transform _leftSleeveArt;
        private Transform _rightSleeveArt;
        private Transform _leftHandArt;
        private Transform _rightHandArt;
        private Transform _leftThighArt;
        private Transform _leftShinArt;
        private Transform _leftShoeArt;
        private Transform _rightThighArt;
        private Transform _rightShinArt;
        private Transform _rightShoeArt;
        private Vector3 _pelvisArtBaseScale;
        private Vector3 _leftSleeveArtBaseScale;
        private Vector3 _rightSleeveArtBaseScale;
        private Vector3 _leftHandArtBaseScale;
        private Vector3 _rightHandArtBaseScale;
        private Vector3 _leftThighArtBaseScale;
        private Vector3 _leftShinArtBaseScale;
        private Vector3 _leftShoeArtBaseScale;
        private Vector3 _rightThighArtBaseScale;
        private Vector3 _rightShinArtBaseScale;
        private Vector3 _rightShoeArtBaseScale;
        private Quaternion _leftHandArtBaseRotation;
        private Quaternion _rightHandArtBaseRotation;
        private float _leftPointFingerAmount;
        private Sprite[] _puppetArtSprites = Array.Empty<Sprite>();
        private Sprite[] _v2HandSprites = Array.Empty<Sprite>();
        private Sprite _leftDefaultHandSprite;
        private Sprite _rightDefaultHandSprite;
        private bool _usesSegmentedPuppetArt;
        private Sprite _squareSprite;
        private Sprite _circleSprite;
        private Sprite _backgroundSprite;
        private Texture2D _squareTexture;
        private Texture2D _circleTexture;
        private Material _lineMaterial;
        private double _songTime;
        private float _facingScale = 1f;
        private bool _v2TurnActive;
        private float _v2TurnProgress;

        public int JointCount => 21;
        public bool UsesSegmentedPuppetArt => _usesSegmentedPuppetArt;
        public int PuppetArtRendererCount { get; private set; }
        public event Action<DancePerformanceStatus> DanceStatusChanged;
        public int RodCount => _rods.Length;
        public int StringCount => RodCount;
        public bool HasBackgroundPicture => _backgroundSprite != null;
        public float LeftUpperArmRotation => _leftUpperArmJoint == null ? 0f : _leftUpperArmJoint.localEulerAngles.z;
        public float LeftForearmRotation => _leftForearmJoint == null ? 0f : _leftForearmJoint.localEulerAngles.z;
        public float RightUpperArmRotation => _rightUpperArmJoint == null ? 0f : _rightUpperArmJoint.localEulerAngles.z;
        public float RightForearmRotation => _rightForearmJoint == null ? 0f : _rightForearmJoint.localEulerAngles.z;
        public float LeftThighRotation => _leftThighJoint == null ? 0f : _leftThighJoint.localEulerAngles.z;
        public float LeftShinRotation => _leftShinJoint == null ? 0f : _leftShinJoint.localEulerAngles.z;
        public float RightThighRotation => _rightThighJoint == null ? 0f : _rightThighJoint.localEulerAngles.z;
        public float RightShinRotation => _rightShinJoint == null ? 0f : _rightShinJoint.localEulerAngles.z;
        public float LeftAnkleRotation => _leftAnkleJoint == null ? 0f : _leftAnkleJoint.localEulerAngles.z;
        public float RightAnkleRotation => _rightAnkleJoint == null ? 0f : _rightAnkleJoint.localEulerAngles.z;
        public float HeadRotation => _headJoint == null ? 0f : _headJoint.localEulerAngles.z;
        public float TorsoRotation => _torsoJoint == null ? 0f : _torsoJoint.localEulerAngles.z;
        public Vector3 PelvisPosition => _pelvisJoint == null ? Vector3.zero : _pelvisJoint.localPosition;
        public float GetRodDrive(int lane) => _rodDrive[lane];
        public Vector3 GetRodGripPosition(int lane) => _rods[lane] == null ? Vector3.zero : _rods[lane].GetPosition(0);
        public float GetStringTension(int lane) => GetRodDrive(lane);
        public int GetRodSortingOrder(int lane) => _rods[lane] == null ? 0 : _rods[lane].sortingOrder;
        public float FacingScale => _facingScale;
        public float LowerBodyFacingScale => _facingScale;
        public float LeftWristRotation => _leftWristJoint == null ? 0f : _leftWristJoint.localEulerAngles.z;
        public float LeftFingerRotation => _leftFingerJoint == null ? 0f : _leftFingerJoint.localEulerAngles.z;
        public Vector3 LeftWristPosition => _leftWristJoint == null ? Vector3.zero :
            _visualRoot.InverseTransformPoint(_leftWristJoint.position);
        public Vector3 RightWristPosition => _rightWristJoint == null ? Vector3.zero :
            _visualRoot.InverseTransformPoint(_rightWristJoint.position);
        public Vector3 LeftElbowPosition => _leftForearmJoint == null ? Vector3.zero :
            _visualRoot.InverseTransformPoint(_leftForearmJoint.position);
        public Vector3 RightElbowPosition => _rightForearmJoint == null ? Vector3.zero :
            _visualRoot.InverseTransformPoint(_rightForearmJoint.position);
        public Vector3 LeftShoulderPosition => _leftUpperArmJoint == null ? Vector3.zero :
            _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
        public Vector3 RightShoulderPosition => _rightUpperArmJoint == null ? Vector3.zero :
            _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
        public Vector3 RightFingerScale => _rightFingerJoint == null ? Vector3.one : _rightFingerJoint.localScale;
        public Vector3 LeftFingerScale => _leftFingerJoint == null ? Vector3.one : _leftFingerJoint.localScale;
        public bool LeftPointFingerVisible => _leftPointFinger != null && _leftPointFinger.gameObject.activeSelf;
        public Vector3 LeftAnklePosition => _leftAnkleJoint == null ? Vector3.zero : _visualRoot.InverseTransformPoint(_leftAnkleJoint.position);
        public Vector3 RightAnklePosition => _rightAnkleJoint == null ? Vector3.zero : _visualRoot.InverseTransformPoint(_rightAnkleJoint.position);
        public Vector3 V2LeftHandTarget { get; private set; }
        public Vector3 V2RightHandTarget { get; private set; }
        public float V2VisibleFacing { get; private set; } = 1f;
        public bool HasV2HandShapes => _v2HandSprites.Length == 4;
        public string LeftHandSpriteName => _leftHandArt == null ? string.Empty :
            _leftHandArt.GetComponent<SpriteRenderer>().sprite.name;
        public string RightHandSpriteName => _rightHandArt == null ? string.Empty :
            _rightHandArt.GetComponent<SpriteRenderer>().sprite.name;

        /// <summary>读取当前 15 分片的可见边界和末端接触点，供 V2 标定与回归测试使用。</summary>
        public PuppetRigCalibrationSnapshot CaptureV2Calibration()
        {
            EnsureInitialized();
            Bounds visible = default;
            bool hasBounds = false;
            foreach (PuppetArtCalibration art in PuppetRigV2Calibration.Art)
            {
                Transform part = _visualRoot.Find(art.Path);
                SpriteRenderer renderer = part == null ? null : part.GetComponent<SpriteRenderer>();
                if (renderer == null || !renderer.enabled) continue;
                if (!hasBounds)
                {
                    visible = renderer.bounds;
                    hasBounds = true;
                }
                else visible.Encapsulate(renderer.bounds);
            }

            Vector2 leftTip = LowestPoint(_leftHandArt.GetComponent<SpriteRenderer>().bounds);
            Vector2 rightTip = LowestPoint(_rightHandArt.GetComponent<SpriteRenderer>().bounds);
            Vector2 leftSole = LowestPoint(_leftAnkleJoint.Find("Art Left Shoe").GetComponent<SpriteRenderer>().bounds);
            Vector2 rightSole = LowestPoint(_rightAnkleJoint.Find("Art Right Shoe").GetComponent<SpriteRenderer>().bounds);
            return new PuppetRigCalibrationSnapshot(ToLocalBounds(visible), ToLocal(leftTip), ToLocal(rightTip),
                ToLocal(leftSole), ToLocal(rightSole));
        }

        private Vector2 ToLocal(Vector2 world) => _visualRoot.InverseTransformPoint(world);

        private Bounds ToLocalBounds(Bounds world)
        {
            Vector3 min = _visualRoot.InverseTransformPoint(world.min);
            Vector3 max = _visualRoot.InverseTransformPoint(world.max);
            var result = new Bounds();
            result.SetMinMax(Vector3.Min(min, max), Vector3.Max(min, max));
            return result;
        }

        private static Vector2 LowestPoint(Bounds bounds) => new Vector2(bounds.center.x, bounds.min.y);

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Begin()
        {
            EnsureInitialized();
            if (_dancePlayback != null)
                _dancePlayback.StatusChanged -= ForwardDanceStatus;
            _dancePlayback = null;
            _v2TurnActive = false;
            _v2TurnProgress = 0f;
            DanceStatusChanged?.Invoke(new DancePerformanceStatus(DancePerformanceKind.Waiting, null));
            _evaluator = new PuppetPoseEvaluator(PrototypeActionBindings.All);
            _songTime = double.NegativeInfinity;
            for (int lane = 0; lane < _heldRods.Length; lane++)
            {
                _heldRods[lane] = false;
            }
            ApplyPose(default);
            _facingScale = 1f;
            _torsoJoint.localScale = Vector3.one;
            _robeSkirt.localScale = new Vector3(1.28f, 0.72f, 1f);
            _robeHem.localScale = new Vector3(1.36f, 0.14f, 1f);
            _leftThighJoint.localPosition = new Vector3(-0.19f, 0f, 0f);
            _rightThighJoint.localPosition = new Vector3(0.19f, 0f, 0f);
            _leftFootPlate.localPosition = new Vector3(-0.19f, -0.04f, 0f);
            _rightFootPlate.localPosition = new Vector3(0.19f, -0.04f, 0f);
            _leftSleeve.localScale = new Vector3(0.48f, 0.82f, 1f);
            _rightSleeve.localScale = new Vector3(0.48f, 0.82f, 1f);
            if (_usesSegmentedPuppetArt)
            {
                _pelvisArt.localScale = _pelvisArtBaseScale;
                _leftSleeveArt.localScale = _leftSleeveArtBaseScale;
                _rightSleeveArt.localScale = _rightSleeveArtBaseScale;
                RestoreV2LowerArtScale();
            }
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            _leftPointFingerAmount = 0f;
            RestoreDefaultHandSprites();
            SyncSegmentedHandArt();
            _pelvisJoint.localPosition = new Vector3(0f, -0.55f, 0f);
            ApplyGroundedLegs(new Vector2(-0.34f, -2.08f), new Vector2(0.34f, -2.08f));
        }

        public void Begin(DancePhrase[] phrases)
        {
            Begin();
            _dancePlayback = new DancePlayback(phrases);
            _dancePlayback.StatusChanged += ForwardDanceStatus;
        }

        private void ForwardDanceStatus(DancePerformanceStatus status) => DanceStatusChanged?.Invoke(status);

        public void Tick(double songTimeSec)
        {
            EnsureInitialized();
            double elapsed = double.IsNegativeInfinity(_songTime)
                ? 0d : System.Math.Max(0d, songTimeSec - _songTime);
            _songTime = songTimeSec;
            if (_dancePlayback != null)
            {
                _dancePlayback.Evaluate(songTimeSec);
                ApplyDancePose();
                UpdateArticulatedDetails(elapsed);
                UpdateHoldSleeves(elapsed);
                UpdateRods();
                return;
            }

            PuppetPose pose = _evaluator.Evaluate(songTimeSec);
            ApplyPose(pose);
            UpdateArticulatedDetails(elapsed);
            UpdateRods();
        }

        /// <summary>僅供結構驗收：單步移重心、起腳及落腳，不代表戲曲套路。</summary>
        public void PreviewStructure(float progress)
        {
            EnsureInitialized();
            float t = Mathf.Clamp01(progress);
            float weight = Smooth(Mathf.Clamp01(t * 2f));
            float step = Smooth(Mathf.Clamp01((t - 0.15f) / 0.7f));
            float lift = Mathf.Sin(Mathf.PI * step) * 0.27f;
            _pelvisJoint.localPosition = new Vector3(-0.18f * weight, -0.55f - (0.08f * lift), 0f);
            _torsoJoint.localScale = Vector3.one;
            SetRotation(_torsoJoint, 0d);
            SetRotation(_leftUpperArmJoint, -25f - (25f * step));
            SetRotation(_leftForearmJoint, -15f - (10f * step));
            SetRotation(_rightUpperArmJoint, 20f + (15f * step));
            SetRotation(_rightForearmJoint, 14f);
            SetRotation(_leftWristJoint, -18f * step);
            SetRotation(_rightWristJoint, 14f * step);
            SetRotation(_leftFingerJoint, 12f * step);
            SetRotation(_rightFingerJoint, -12f * step);
            SetRotation(_leftSleeveTail, -16f * step);
            SetRotation(_rightSleeveTail, 12f * step);
            ApplyGroundedLegs(new Vector2(-0.34f, -2.08f),
                new Vector2(0.34f + (0.55f * step), -2.08f + lift));
            UpdateRods();
        }

        /// <summary>M8.1 實驗預覽；只驗證手勢剪影，不接入正式舞句或判定。</summary>
        public void PreviewHandGesture(HandGesture gesture, float progress)
        {
            EnsureInitialized();
            RestoreDefaultHandSprites();
            HandGesturePhrase phrase = HandGestureChoreography.Get(gesture);
            float t = Mathf.Clamp01(progress);
            SetRotation(_leftUpperArmJoint, phrase.Shoulder(t));
            SetRotation(_leftForearmJoint, phrase.Elbow(t));
            SetRotation(_leftWristJoint, phrase.Wrist(t));
            SetRotation(_leftFingerJoint, phrase.Finger(t));
            SetRotation(_rightUpperArmJoint, 0d);
            SetRotation(_rightForearmJoint, 0d);
            SetRotation(_rightWristJoint, 0d);
            SetRotation(_rightFingerJoint, 0d);
            _leftFingerJoint.localScale = new Vector3((float)phrase.FingerWidth(t),
                (float)phrase.FingerLength(t), 1f);
            _rightFingerJoint.localScale = Vector3.one;
            ApplyLeftPointFinger((float)phrase.PointFinger(t));
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            SetRotation(_torsoJoint, 0d);
            _torsoJoint.localScale = Vector3.one;
            _pelvisJoint.localPosition = new Vector3(0f, -0.55f, 0f);
            ApplyGroundedLegs(new Vector2(-0.34f, -2.08f), new Vector2(0.34f, -2.08f));
            _rodDrive[0] = t;
            for (int lane = 1; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            UpdateRods();
        }

        /// <summary>雙手拳掌禮能力預覽；只驗證兩支手簽會合與右拳剪影，不定案抱拳／拱手名稱。</summary>
        public void PreviewFistPalmSalute(float progress)
        {
            EnsureInitialized();
            RestoreDefaultHandSprites();
            FistPalmSalutePhrase phrase = FistPalmSaluteChoreography.Get();
            float t = Mathf.Clamp01(progress);
            SetRotation(_leftUpperArmJoint, phrase.LeftShoulder(t));
            SetRotation(_leftForearmJoint, phrase.LeftElbow(t));
            SetRotation(_leftWristJoint, phrase.LeftWrist(t));
            SetRotation(_leftFingerJoint, phrase.LeftFinger(t));
            SetRotation(_rightUpperArmJoint, phrase.RightShoulder(t));
            SetRotation(_rightForearmJoint, phrase.RightElbow(t));
            SetRotation(_rightWristJoint, phrase.RightWrist(t));
            SetRotation(_rightFingerJoint, phrase.RightFinger(t));
            _leftFingerJoint.localScale = Vector3.one;
            float closure = (float)phrase.RightClosure(t);
            _rightFingerJoint.localScale = new Vector3(1f - (0.48f * closure),
                1f - (0.30f * closure), 1f);
            SyncSegmentedHandArt();
            SetRotation(_torsoJoint, 0d);
            _torsoJoint.localScale = Vector3.one;
            _pelvisJoint.localPosition = new Vector3(0f, -0.55f, 0f);
            ApplyGroundedLegs(new Vector2(-0.34f, -2.08f), new Vector2(0.34f, -2.08f));
            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            _rodDrive[0] = t;
            _rodDrive[2] = t;
            UpdateRods();
        }

        /// <summary>V2-P1 双展山膀预览；与正式旧舞句隔离。</summary>
        public void PreviewV2DoubleMountainArm(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = DoubleMountainArmChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, 1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);
            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);
            _rodDrive[0] = Mathf.Clamp01(progress);
            _rodDrive[2] = Mathf.Clamp01(progress);
            for (int lane = 1; lane < _rodDrive.Length; lane++)
                if (lane != 2) _rodDrive[lane] = 0f;
            UpdateRods();
        }

        /// <summary>V2-P2 提膝挂脚预览；左足锁定，右足落稳后才回收重心。</summary>
        public void PreviewV2RaisedKneeHookedFoot(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = RaisedKneeHookedFootChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, 1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);
            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            _rodDrive[0] = Mathf.Clamp01(progress);
            _rodDrive[3] = Mathf.Clamp01(Mathf.Abs((float)pose.RootX) / 0.18f);
            _rodDrive[5] = Mathf.Clamp01(((float)pose.RightFootY + 2.08f) / 0.43f);
            UpdateRods();
        }

        /// <summary>V2-P3 回身定相预览；短暂侧身并统一交换分片前后层级。</summary>
        public void PreviewV2TurnBackSetPose(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = true;
            _v2TurnProgress = Mathf.Clamp01(progress);
            PuppetV2Pose pose = TurnBackSetPoseChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            _torsoJoint.localScale = Vector3.one;
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _facingScale = (float)pose.Facing;

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, 1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, 0d);
            SetRotation(_rightAnkleJoint, 0d);

            float facing = (float)pose.Facing;
            float sign = facing < 0f ? -1f : 1f;
            V2VisibleFacing = sign * Mathf.Max(0.10f, Mathf.Abs(facing));
            _torsoJoint.localScale = new Vector3(V2VisibleFacing, 1f, 1f);
            _pelvisArt.localScale = MirrorX(_pelvisArtBaseScale, V2VisibleFacing);
            _leftThighArt.localScale = MirrorX(_leftThighArtBaseScale, V2VisibleFacing);
            _leftShinArt.localScale = MirrorX(_leftShinArtBaseScale, V2VisibleFacing);
            _leftShoeArt.localScale = MirrorX(_leftShoeArtBaseScale, V2VisibleFacing);
            _rightThighArt.localScale = MirrorX(_rightThighArtBaseScale, V2VisibleFacing);
            _rightShinArt.localScale = MirrorX(_rightShinArtBaseScale, V2VisibleFacing);
            _rightShoeArt.localScale = MirrorX(_rightShoeArtBaseScale, V2VisibleFacing);
            ApplyV2FacingSorting(facing < 0f);
            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            _rodDrive[1] = Mathf.Clamp01(1f - Mathf.Abs(facing));
            _rodDrive[4] = _rodDrive[1];
            UpdateRods();
        }

        /// <summary>V2-P4 四手型生产预览；只切换获批手图，不改变旧舞句。</summary>
        public void PreviewV2HandShape(PuppetHandShape shape)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            _pelvisJoint.localPosition = new Vector3(0f, -0.40f, 0f);
            _torsoJoint.localScale = Vector3.one;
            SetRotation(_torsoJoint, 0d);
            SetRotation(_headJoint, 0d);
            SetRotation(_leftUpperArmJoint, -92d);
            SetRotation(_leftForearmJoint, 18d);
            SetRotation(_rightUpperArmJoint, 92d);
            SetRotation(_rightForearmJoint, -18d);
            SetRotation(_leftWristJoint, 0d);
            SetRotation(_rightWristJoint, 0d);
            SetRotation(_leftFingerJoint, 0d);
            SetRotation(_rightFingerJoint, 0d);
            ApplyV2HandShapes(shape, shape);
            ApplyGroundedLegs(new Vector2(-0.34f, -2.08f), new Vector2(0.34f, -2.08f));
            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            UpdateRods();
        }

        /// <summary>V2-P5-01 缓步入场预览；八次小步交替落地，末两拍停稳。</summary>
        public void PreviewV2SlowEntrance(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = SlowEntranceChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, 1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            // 求解器输出幕面绝对角；肩关节挂在旋转躯干下，需抵消父级角度。
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float walk = Mathf.Clamp01((progress - (2f / SlowEntranceChoreography.Beats)) /
                (8f / SlowEntranceChoreography.Beats));
            _rodDrive[0] = Mathf.Abs(Mathf.Sin(walk * Mathf.PI * 8f));
            _rodDrive[2] = _rodDrive[0];
            _rodDrive[3] = pose.LeftFootPlanted ? 0f : 1f;
            _rodDrive[4] = 4f * walk * (1f - walk);
            _rodDrive[5] = pose.RightFootPlanted ? 0f : 1f;
            UpdateRods();
        }

        /// <summary>V2-P5-02 整冠肃立预览；双手在冠侧连续整理，双足和骨盆保持锁定。</summary>
        public void PreviewV2StraightenCrown(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = StraightenCrownChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, 1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float handDrive = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            _rodDrive[0] = handDrive;
            _rodDrive[1] = Mathf.Sin(Mathf.Clamp01(progress * 2f) * Mathf.PI);
            _rodDrive[2] = handDrive;
            UpdateRods();
        }

        /// <summary>V2-P5-03 拱手致礼预览；胸前合手保持礼位，以腰、头和双膝共同躬身。</summary>
        public void PreviewV2BowingSalute(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = BowingSaluteChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, 1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float phraseDrive = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            _rodDrive[0] = phraseDrive;
            _rodDrive[2] = phraseDrive;
            _rodDrive[4] = Mathf.Sin(Mathf.Clamp01((progress - 0.48f) / 0.52f) * Mathf.PI);
            UpdateRods();
        }

        /// <summary>V2-P5-04 云手展圆预览；双手错拍走上、下圆弧，翻腕融合在连续路径内。</summary>
        public void PreviewV2CloudHandsCircle(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = CloudHandsCircleChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            // 聚手阶段保持袖片分离；展圆和回收都只在双臂接近伸直时切换折肘分支。
            bool openBranch = progress >= 0.70f && progress < 0.82f;
            float leftBend = openBranch ? 1f : -1f;
            float rightBend = openBranch ? -1f : 1f;
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, leftBend,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, rightBend,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            _rodDrive[0] = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            float delayed = Mathf.Clamp01((progress - 0.125f) / 0.875f);
            _rodDrive[2] = Mathf.Sin(delayed * Mathf.PI);
            _rodDrive[4] = Mathf.Abs(Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI * 2f)) * 0.15f;
            UpdateRods();
        }

        /// <summary>V2-P5-05 托月仰望预览；面向侧右手斜上托月，定掌后抬头凝望，左手守于腰前。</summary>
        public void PreviewV2MoonOfferingGaze(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = MoonOfferingGazeChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, 1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float lift = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            _rodDrive[0] = lift * 0.30f;
            _rodDrive[2] = lift;
            _rodDrive[4] = Mathf.Sin(Mathf.Clamp01((progress - 0.50f) / 0.50f) * Mathf.PI) * 0.18f;
            UpdateRods();
        }

        /// <summary>V2-P5-07 斜展顺风旗预览；左臂横展、右臂斜上，双足固定形成强对角线。</summary>
        public void PreviewV2WindFlagDiagonal(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = WindFlagDiagonalChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, 1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float spread = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            _rodDrive[0] = spread * 0.70f;
            _rodDrive[2] = spread;
            _rodDrive[4] = spread * 0.12f;
            UpdateRods();
        }

        /// <summary>V2-P5-08 穿掌送势预览；前脚先落、身体后随，右掌再由胸前沿直线穿送。</summary>
        public void PreviewV2PiercingPalmDrive(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = PiercingPalmDriveChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float phrase = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            _rodDrive[2] = phrase;
            _rodDrive[4] = phrase * 0.18f;
            _rodDrive[5] = Mathf.Sin(Mathf.Clamp01(progress / 0.24f) * Mathf.PI) * 0.65f;
            UpdateRods();
        }

        /// <summary>V2-P5-09 指路远眺预览；头先提示方向，右臂后伸并在停顿段切为指向掌。</summary>
        public void PreviewV2PointingFarGaze(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = PointingFarGazeChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float phrase = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            _rodDrive[1] = Mathf.Sin(Mathf.Clamp01(progress / 0.25f) * Mathf.PI) * 0.45f;
            _rodDrive[2] = phrase;
            _rodDrive[5] = Mathf.Sin(Mathf.Clamp01(progress / 0.50f) * Mathf.PI) * 0.20f;
            UpdateRods();
        }

        /// <summary>V2-P5-10 遮额探看预览；右手先搭眉上，身体后前探，看定后回身落手。</summary>
        public void PreviewV2ShadingBrowSearch(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = ShadingBrowSearchChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float handLift = Mathf.Sin(Mathf.Clamp01(progress / 0.50f) * Mathf.PI);
            float search = Mathf.Sin(Mathf.Clamp01((progress - 0.375f) / 0.625f) * Mathf.PI);
            _rodDrive[1] = search * 0.45f;
            _rodDrive[2] = Mathf.Max(handLift, search * 0.75f);
            _rodDrive[4] = search * 0.22f;
            UpdateRods();
        }

        /// <summary>V2-P5-11 掩面低诉预览；双手近下脸、低头收胸，一次沉肩后落至胸前。</summary>
        public void PreviewV2VeiledFaceLament(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = VeiledFaceLamentChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            // 两肘从身体两侧自然张开，腕在面侧上方、掌指向下覆面。
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, 1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float gather = Mathf.Sin(Mathf.Clamp01(progress / 0.70f) * Mathf.PI);
            float sigh = Mathf.Sin(Mathf.Clamp01((progress - 0.66f) / 0.24f) * Mathf.PI);
            _rodDrive[0] = gather;
            _rodDrive[1] = sigh * 0.35f;
            _rodDrive[2] = gather;
            _rodDrive[4] = Mathf.Max(gather * 0.25f, sigh * 0.45f);
            UpdateRods();
        }

        /// <summary>V2-P5-12 展臂仰笑预览；胸前聚势后双臂斜上打开，以抬头开胸表达笑意。</summary>
        public void PreviewV2OpenArmsUpwardLaugh(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = OpenArmsUpwardLaughChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, 1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float opening = Mathf.Sin(Mathf.Clamp01(progress / 0.85f) * Mathf.PI);
            float lift = Mathf.Sin(Mathf.Clamp01((progress - 0.35f) / 0.65f) * Mathf.PI);
            _rodDrive[0] = opening;
            _rodDrive[2] = opening;
            _rodDrive[4] = lift * 0.55f;
            UpdateRods();
        }

        /// <summary>V2-P5-13 躬身请势预览；右掌向前下方相请，左手守胸，腰带头下俯。</summary>
        public void PreviewV2BowingInvitation(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = BowingInvitationChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float invitation = Mathf.Sin(Mathf.Clamp01(progress / 0.88f) * Mathf.PI);
            float bow = Mathf.Sin(Mathf.Clamp01((progress - 0.25f) / 0.75f) * Mathf.PI);
            _rodDrive[1] = invitation;
            _rodDrive[2] = invitation * 0.28f;
            _rodDrive[4] = bow * 0.55f;
            UpdateRods();
        }

        /// <summary>V2-P5-14 弓步推势预览；前脚落稳、重心前移后双掌错层推出。</summary>
        public void PreviewV2LungeDoublePalmPush(float progress)
        {
            EnsureInitialized();
            _v2TurnActive = false;
            PuppetV2Pose pose = LungeDoublePalmPushChoreography.Evaluate(progress);
            _pelvisJoint.localPosition = new Vector3((float)pose.RootX, (float)pose.RootY, 0f);
            SetRotation(_torsoJoint, pose.Torso);
            SetRotation(_headJoint, pose.Head);
            _torsoJoint.localScale = Vector3.one;
            _facingScale = 1f;
            _pelvisArt.localScale = _pelvisArtBaseScale;
            RestoreV2LowerArtScale();

            Vector2 leftShoulder = _visualRoot.InverseTransformPoint(_leftUpperArmJoint.position);
            Vector2 rightShoulder = _visualRoot.InverseTransformPoint(_rightUpperArmJoint.position);
            V2LeftHandTarget = new Vector3((float)pose.LeftHandX, (float)pose.LeftHandY, 0f);
            V2RightHandTarget = new Vector3((float)pose.RightHandX, (float)pose.RightHandY, 0f);
            PlanarTwoBoneArmSolver.Solve(leftShoulder, V2LeftHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float leftShoulderAngle, out float leftElbowAngle);
            PlanarTwoBoneArmSolver.Solve(rightShoulder, V2RightHandTarget,
                PuppetRigV2Calibration.UpperArmLength, PuppetRigV2Calibration.ForearmLength, -1f,
                out float rightShoulderAngle, out float rightElbowAngle);
            SetRotation(_leftUpperArmJoint, leftShoulderAngle - (float)pose.Torso);
            SetRotation(_leftForearmJoint, leftElbowAngle);
            SetRotation(_rightUpperArmJoint, rightShoulderAngle - (float)pose.Torso);
            SetRotation(_rightForearmJoint, rightElbowAngle);
            SetRotation(_leftWristJoint, pose.LeftWrist);
            SetRotation(_rightWristJoint, pose.RightWrist);
            _leftFingerJoint.localScale = Vector3.one;
            _rightFingerJoint.localScale = Vector3.one;
            _leftPointFinger.gameObject.SetActive(false);
            _rightPointFinger.gameObject.SetActive(false);
            SyncSegmentedHandArt();
            ApplyV2HandShapes(pose.LeftHandShape, pose.RightHandShape);
            // 双掌同向向角色前方推出；通用左右手型默认互为镜像，
            // 本动作需将左掌翻到与右掌相同朝向，不能靠扭腕反折手臂。
            _leftHandArt.localScale = new Vector3(-Mathf.Abs(_leftHandArt.localScale.x),
                _leftHandArt.localScale.y, _leftHandArt.localScale.z);

            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint,
                new Vector2((float)pose.LeftFootX, (float)pose.LeftFootY), -1f);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint,
                new Vector2((float)pose.RightFootX, (float)pose.RightFootY), 1f);
            SetRotation(_leftAnkleJoint, pose.LeftShoe);
            SetRotation(_rightAnkleJoint, pose.RightShoe);

            for (int lane = 0; lane < _rodDrive.Length; lane++) _rodDrive[lane] = 0f;
            float step = Mathf.Sin(Mathf.Clamp01(progress / 0.34f) * Mathf.PI);
            float push = Mathf.Sin(Mathf.Clamp01((progress - 0.28f) / 0.72f) * Mathf.PI);
            _rodDrive[0] = push;
            _rodDrive[2] = push;
            _rodDrive[3] = step;
            _rodDrive[4] = Mathf.Max(step * 0.30f, push * 0.45f);
            UpdateRods();
        }

        /// <summary>每次原始按鍵都立即驅动對應竹桿與關節，與判定結果解耦。</summary>
        public void OnInput(HitInput input)
        {
            EnsureInitialized();
            if (_dancePlayback != null) return;
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
            if (_dancePlayback != null)
            {
                if (result.EventKind == JudgmentEventKind.HoldStarted)
                    SetHeldRods(result.RequiredLanesMask, true);
                else if (result.EventKind == JudgmentEventKind.NoteJudged)
                    SetHeldRods(result.RequiredLanesMask, false);
                _dancePlayback.OnJudged(result, _songTime);
                return;
            }

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

            Texture2D backgroundTexture = Resources.Load<Texture2D>("YingYun/backgroundpicture");
            if (backgroundTexture != null)
            {
                float pixelsPerUnit = backgroundTexture.height / 10f;
                _backgroundSprite = Sprite.Create(
                    backgroundTexture,
                    new Rect(0f, 0f, backgroundTexture.width, backgroundTexture.height),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit);
                CreateSprite("Traditional Shadow Play Background", _visualRoot, Vector3.zero, Vector2.one,
                    new Color(0.86f, 0.86f, 0.86f, 1f), -20, _backgroundSprite);
            }

            CreateSprite("Warm Backlight", _visualRoot, Vector3.zero, new Vector2(5.4f, 6.15f),
                new Color(1f, 0.73f, 0.32f, 0.22f), -7, _circleSprite);
            CreateSprite("Translucent Paper Screen", _visualRoot, Vector3.zero, new Vector2(4.65f, 5.55f),
                new Color(1f, 0.88f, 0.60f, 0.88f), -6, _circleSprite);
            BuildStageFrame();

            var puppetRoot = new GameObject("Joint Pelvis");
            puppetRoot.transform.SetParent(_visualRoot, false);
            puppetRoot.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            _pelvisJoint = puppetRoot.transform;
            _torsoJoint = CreateJoint("Joint Waist", _pelvisJoint, new Vector3(0f, 0.15f, 0f));
            CreateJointPin(_torsoJoint, 7);

            CreateSprite("Torso", _torsoJoint, new Vector3(0f, 0.53f, 0f), new Vector2(0.92f, 1.48f),
                ShadowColor(), 3, _circleSprite);
            CreateSprite("Waist Ornament", _torsoJoint, new Vector3(0f, -0.03f, -0.01f), new Vector2(1.02f, 0.20f),
                AccentColor(), 4, _squareSprite);
            _robeSkirt = CreateSprite("Robe Skirt", _pelvisJoint, new Vector3(0f, -0.28f, 0f), new Vector2(1.28f, 0.72f),
                ShadowColor(), 3, _circleSprite);
            CreateSprite("Robe Front Trim", _robeSkirt, new Vector3(0.28f, -0.03f, -0.01f),
                new Vector2(0.12f, 0.52f), AccentColor(), 4, _squareSprite);
            _robeHem = CreateSprite("Robe Hem", _pelvisJoint, new Vector3(0f, -0.58f, -0.01f), new Vector2(1.36f, 0.14f),
                AccentColor(), 4, _squareSprite);
            CreateSprite("Chest Cutout", _torsoJoint, new Vector3(0f, 0.63f, -0.01f), new Vector2(0.48f, 0.18f),
                new Color(0.78f, 0.28f, 0.055f, 0.72f), 4, _circleSprite);
            Transform bodyRodSocket = CreateJoint("Chest Rod Socket", _torsoJoint, new Vector3(0f, 0.85f, 0f));
            CreateJointPin(bodyRodSocket, 5);

            _headJoint = CreateJoint("Joint Neck", _torsoJoint, new Vector3(0f, 1.26f, 0f));
            Transform head = CreateSprite("Head", _headJoint, new Vector3(0f, 0.28f, 0f), new Vector2(0.62f, 0.72f),
                ShadowColor(), 5, _circleSprite);
            CreateSprite("Profile Nose", _headJoint, new Vector3(0.33f, 0.30f, 0f), new Vector2(0.22f, 0.16f),
                ShadowColor(), 6, _circleSprite);
            CreateSprite("Profile Eye", _headJoint, new Vector3(0.19f, 0.38f, 0f), new Vector2(0.10f, 0.06f),
                AccentColor(), 7, _circleSprite);
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

            _leftUpperArmJoint = CreateJoint("Joint Left Shoulder", _torsoJoint, new Vector3(-0.34f, 0.93f, 0f));
            CreateLimb("Left Upper Arm", _leftUpperArmJoint, 0.82f, 0.20f, 5);
            _leftSleeve = CreateSprite("Left Flowing Sleeve", _leftUpperArmJoint, new Vector3(-0.12f, -0.48f, 0f), new Vector2(0.48f, 0.82f),
                ShadowColor(), 4, _circleSprite);
            _leftForearmJoint = CreateJoint("Joint Left Elbow", _leftUpperArmJoint, new Vector3(0f, -0.82f, 0f));
            Transform leftHand = CreateLimb("Left Forearm", _leftForearmJoint, 0.72f, 0.17f, 6);
            _leftWristJoint = CreateJoint("Joint Left Wrist", _leftForearmJoint, new Vector3(0f, -0.72f, 0f));
            BuildHand(_leftWristJoint, true);
            _leftSleeveTail = BuildSleeveTail(_leftForearmJoint, true);

            _rightUpperArmJoint = CreateJoint("Joint Right Shoulder", _torsoJoint, new Vector3(0.30f, 0.93f, 0f));
            CreateLimb("Right Upper Arm", _rightUpperArmJoint, 0.82f, 0.20f, 5);
            _rightSleeve = CreateSprite("Right Flowing Sleeve", _rightUpperArmJoint, new Vector3(0.12f, -0.48f, 0f), new Vector2(0.48f, 0.82f),
                ShadowColor(), 4, _circleSprite);
            _rightForearmJoint = CreateJoint("Joint Right Elbow", _rightUpperArmJoint, new Vector3(0f, -0.82f, 0f));
            Transform rightHand = CreateLimb("Right Forearm", _rightForearmJoint, 0.72f, 0.17f, 6);
            _rightWristJoint = CreateJoint("Joint Right Wrist", _rightForearmJoint, new Vector3(0f, -0.72f, 0f));
            BuildHand(_rightWristJoint, false);
            _rightSleeveTail = BuildSleeveTail(_rightForearmJoint, false);

            _leftThighJoint = CreateJoint("Joint Left Hip", _pelvisJoint, new Vector3(-0.19f, 0f, 0f));
            CreateLimb("Left Thigh", _leftThighJoint, 0.88f, 0.24f, 3);
            _leftShinJoint = CreateJoint("Joint Left Knee", _leftThighJoint, new Vector3(0f, -0.88f, 0f));
            Transform leftFoot = CreateLimb("Left Shin", _leftShinJoint, 0.82f, 0.19f, 4);
            _leftAnkleJoint = CreateJoint("Joint Left Ankle", _leftShinJoint, new Vector3(0f, -0.82f, 0f));
            _leftFootPlate = BuildFoot(_leftAnkleJoint, true);

            _rightThighJoint = CreateJoint("Joint Right Hip", _pelvisJoint, new Vector3(0.19f, 0f, 0f));
            CreateLimb("Right Thigh", _rightThighJoint, 0.88f, 0.24f, 3);
            _rightShinJoint = CreateJoint("Joint Right Knee", _rightThighJoint, new Vector3(0f, -0.88f, 0f));
            Transform rightFoot = CreateLimb("Right Shin", _rightShinJoint, 0.82f, 0.19f, 4);
            _rightAnkleJoint = CreateJoint("Joint Right Ankle", _rightShinJoint, new Vector3(0f, -0.82f, 0f));
            _rightFootPlate = BuildFoot(_rightAnkleJoint, false);

            CreateJointPin(_leftUpperArmJoint, 7);
            CreateJointPin(_leftForearmJoint, 7);
            CreateJointPin(_rightUpperArmJoint, 7);
            CreateJointPin(_rightForearmJoint, 7);
            CreateJointPin(_leftThighJoint, 7);
            CreateJointPin(_leftShinJoint, 7);
            CreateJointPin(_rightThighJoint, 7);
            CreateJointPin(_rightShinJoint, 7);
            CreateJointPin(_leftWristJoint, 8);
            CreateJointPin(_rightWristJoint, 8);
            CreateJointPin(_leftAnkleJoint, 6);
            CreateJointPin(_rightAnkleJoint, 6);

            LoadAndBuildSegmentedPuppetArt();

            _rodTargets[0] = _leftWristJoint;
            _rodTargets[1] = head;
            _rodTargets[2] = _rightWristJoint;
            _rodTargets[3] = _leftAnkleJoint;
            _rodTargets[4] = bodyRodSocket;
            _rodTargets[5] = _rightAnkleJoint;
            BuildRods();
        }

        private void BuildHand(Transform wrist, bool left)
        {
            float side = left ? -1f : 1f;
            CreateSprite(left ? "Left Palm Plate" : "Right Palm Plate", wrist,
                new Vector3(side * 0.08f, -0.12f, 0f), new Vector2(0.23f, 0.27f), ShadowColor(), 8, _circleSprite);
            Transform finger = CreateJoint(left ? "Joint Left Finger Fan" : "Joint Right Finger Fan",
                wrist, new Vector3(side * 0.12f, -0.22f, 0f));
            CreateSprite("Finger Silhouette", finger, new Vector3(side * 0.08f, -0.12f, 0f),
                new Vector2(0.12f, 0.30f), ShadowColor(), 8, _circleSprite);
            Transform pointFinger = CreateSprite(left ? "Left Single Finger Silhouette" : "Right Single Finger Silhouette",
                wrist, new Vector3(side * 0.22f, -0.30f, 0f), new Vector2(0.075f, 0.52f),
                ShadowColor(), 9, _circleSprite);
            pointFinger.gameObject.SetActive(false);
            if (left)
            {
                _leftFingerJoint = finger;
                _leftPointFinger = pointFinger;
            }
            else
            {
                _rightFingerJoint = finger;
                _rightPointFinger = pointFinger;
            }
        }

        private Transform BuildSleeveTail(Transform elbow, bool left)
        {
            float side = left ? -1f : 1f;
            Transform cuff = CreateJoint(left ? "Joint Left Sleeve Cuff" : "Joint Right Sleeve Cuff",
                elbow, new Vector3(side * 0.12f, -0.30f, 0f));
            CreateSprite("Sleeve Cuff Plate", cuff, new Vector3(0f, -0.16f, 0f),
                new Vector2(0.52f, 0.38f), ShadowColor(), 7, _circleSprite);
            Transform tail = CreateJoint(left ? "Joint Left Sleeve Tail" : "Joint Right Sleeve Tail",
                cuff, new Vector3(0f, -0.29f, 0f));
            CreateSprite("Trailing Water Sleeve", tail, new Vector3(side * 0.04f, -0.23f, 0f),
                new Vector2(0.31f, 0.70f), new Color(0.32f, 0.045f, 0.025f, 0.82f), 8, _circleSprite);
            CreateSprite("Sleeve Tail Cutwork", tail, new Vector3(side * 0.04f, -0.47f, 0f),
                new Vector2(0.19f, 0.09f), AccentColor(), 9, _circleSprite);
            CreateJointPin(cuff, 8);
            return tail;
        }

        private Transform BuildFoot(Transform ankle, bool left)
        {
            float side = left ? -1f : 1f;
            return CreateSprite(left ? "Left Foot Plate" : "Right Foot Plate", ankle,
                new Vector3(side * 0.19f, -0.04f, 0f), new Vector2(0.48f, 0.15f), ShadowColor(), 6, _circleSprite);
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
            CreateSprite("Foot Contact Line", _visualRoot, new Vector3(0f, -2.15f, 0f), new Vector2(2.75f, 0.055f),
                new Color(0.25f, 0.075f, 0.025f, 0.58f), 2, _squareSprite);
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
                bool principalRod = lane == 0 || lane == 2 || lane == 4;
                line.startWidth = principalRod ? 0.075f : 0.043f;
                line.endWidth = principalRod ? 0.055f : 0.032f;
                line.material = _lineMaterial;
                line.sortingOrder = 9;
                _rods[lane] = line;

                Transform grip = CreateSprite($"Bamboo Grip {lane}", _visualRoot, RodGripPoints[lane], new Vector2(0.46f, 0.12f),
                    new Color(0.38f, 0.16f, 0.055f, principalRod ? 0.98f : 0.58f), 10, _circleSprite);
                _rodGrips[lane] = grip.GetComponent<SpriteRenderer>();
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

        private void ApplyDancePose()
        {
            SetRotation(_headJoint, _dancePlayback.Angle(DanceJoint.Head));
            // 結構階段先限住腰片：舊舞句的任意軀幹偏轉不應破壞腳下承重。
            SetRotation(_torsoJoint, Mathf.Clamp((float)_dancePlayback.Angle(DanceJoint.Torso), -6f, 6f));
            SetRotation(_leftUpperArmJoint, _dancePlayback.Angle(DanceJoint.LeftShoulder));
            SetRotation(_leftForearmJoint, _dancePlayback.Angle(DanceJoint.LeftElbow));
            SetRotation(_rightUpperArmJoint, _dancePlayback.Angle(DanceJoint.RightShoulder));
            SetRotation(_rightForearmJoint, _dancePlayback.Angle(DanceJoint.RightElbow));
            float facing = (float)_dancePlayback.FacingScale;
            _facingScale = facing;
            // 舊幾何剪影可以靠連續縮放穿過側身；完整分片貼圖照原值會被壓成細線，
            // 上一版改成整寬換面又失去了側身深度。保留原 FacingScale 時序，在美術層把
            // 最窄側面限制為 56%，使正面－側面－背面仍有連續透視且不破壞分片細節。
            float visibleFacing = facing;
            if (_usesSegmentedPuppetArt)
            {
                float turnDepth = Mathf.Lerp(0.56f, 1f, Mathf.Abs(facing));
                visibleFacing = (facing < 0f ? -1f : 1f) * turnDepth;
            }
            _torsoJoint.localScale = new Vector3(visibleFacing, 1f, 1f);
            // 分片的下身各自改向，不縮放骨盆根節點，避免足點反解在側身時失穩。
            _robeSkirt.localScale = new Vector3(1.28f * visibleFacing, 0.72f, 1f);
            _robeHem.localScale = new Vector3(1.36f * visibleFacing, 0.14f, 1f);
            if (_usesSegmentedPuppetArt)
            {
                _pelvisArt.localScale = new Vector3(_pelvisArtBaseScale.x * visibleFacing,
                    _pelvisArtBaseScale.y, _pelvisArtBaseScale.z);
            }
            // 側身時讓兩個鉚接髖位收攏並交換前後，但保持足點在地平線上。
            float hipOffset = (0.15f + (0.04f * facing)) * facing;
            _leftThighJoint.localPosition = new Vector3(-hipOffset, 0f, 0f);
            _rightThighJoint.localPosition = new Vector3(hipOffset, 0f, 0f);
            _leftFootPlate.localPosition = new Vector3(-0.19f * facing, -0.04f, 0f);
            _rightFootPlate.localPosition = new Vector3(0.19f * facing, -0.04f, 0f);
            _pelvisJoint.localPosition = new Vector3((float)_dancePlayback.PelvisX, -0.55f, 0f);
            ApplyGroundedLegs(
                new Vector2((float)_dancePlayback.LeftFootX, (float)_dancePlayback.LeftFootY),
                new Vector2((float)_dancePlayback.RightFootX, (float)_dancePlayback.RightFootY));
            _rodDrive[0] = Mathf.Clamp01(Mathf.Abs((float)_dancePlayback.Angle(DanceJoint.LeftShoulder)) / 80f);
            _rodDrive[1] = Mathf.Clamp01(Mathf.Abs((float)_dancePlayback.Angle(DanceJoint.Head)) / 30f);
            _rodDrive[2] = Mathf.Clamp01(Mathf.Abs((float)_dancePlayback.Angle(DanceJoint.RightShoulder)) / 150f);
            _rodDrive[3] = Mathf.Clamp01(((float)_dancePlayback.LeftFootY + 2.08f) / 0.34f);
            _rodDrive[4] = Mathf.Clamp01(Mathf.Abs((float)_dancePlayback.Angle(DanceJoint.Torso)) / 25f);
            _rodDrive[5] = Mathf.Clamp01(((float)_dancePlayback.RightFootY + 2.08f) / 0.34f);
            for (int lane = 0; lane < _heldRods.Length; lane++)
            {
                if (_heldRods[lane]) _rodDrive[lane] = Mathf.Max(_rodDrive[lane], 0.75f);
            }
        }

        private void UpdateHoldSleeves(double elapsed)
        {
            float step = 1f - Mathf.Exp(-8f * (float)System.Math.Min(elapsed, 0.5d));
            float left = Mathf.Lerp(_leftSleeve.localScale.y, _heldRods[0] ? 1.07f : 0.82f, step);
            float right = Mathf.Lerp(_rightSleeve.localScale.y, _heldRods[2] ? 1.07f : 0.82f, step);
            _leftSleeve.localScale = new Vector3(0.48f, left, 1f);
            _rightSleeve.localScale = new Vector3(0.48f, right, 1f);
            if (_usesSegmentedPuppetArt)
            {
                float leftArtY = Mathf.Lerp(_leftSleeveArt.localScale.y,
                    _leftSleeveArtBaseScale.y * (_heldRods[0] ? 1.12f : 1f), step);
                float rightArtY = Mathf.Lerp(_rightSleeveArt.localScale.y,
                    _rightSleeveArtBaseScale.y * (_heldRods[2] ? 1.12f : 1f), step);
                _leftSleeveArt.localScale = new Vector3(_leftSleeveArtBaseScale.x, leftArtY, 1f);
                _rightSleeveArt.localScale = new Vector3(_rightSleeveArtBaseScale.x, rightArtY, 1f);
            }
        }

        private void LoadAndBuildSegmentedPuppetArt()
        {
            _puppetArtSprites = Resources.LoadAll<Sprite>("YingYun/Art/Puppet/puppet_parts_v1");
            _v2HandSprites = Resources.LoadAll<Sprite>("YingYun/Art/Puppet/V2/puppet_hand_shapes_v2");
            string[] requiredNames =
            {
                "puppet_head", "puppet_torso", "puppet_pelvis",
                "puppet_left_upper_arm", "puppet_left_forearm", "puppet_left_hand",
                "puppet_right_upper_arm", "puppet_right_forearm", "puppet_right_hand",
                "puppet_left_thigh", "puppet_left_shin", "puppet_left_shoe",
                "puppet_right_thigh", "puppet_right_shin", "puppet_right_shoe"
            };
            for (int i = 0; i < requiredNames.Length; i++)
            {
                if (FindPuppetArt(requiredNames[i]) == null)
                {
                    Debug.LogWarning($"[M11-Art] Missing segmented puppet sprite: {requiredNames[i]}. Legacy puppet art remains active.");
                    return;
                }
            }

            SpriteRenderer[] legacy = _pelvisJoint.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < legacy.Length; i++) legacy[i].enabled = false;

            CreateRigArt("Art Head", _headJoint, FindPuppetArt("puppet_head"), 1.42f, 12);
            CreateRigArt("Art Torso", _torsoJoint, FindPuppetArt("puppet_torso"), 1.45f, 8);
            _pelvisArt = CreateRigArt("Art Pelvis", _pelvisJoint, FindPuppetArt("puppet_pelvis"), 0.86f, 7);
            _pelvisArt.localPosition = new Vector3(0f, 0.16f, 0f);

            _leftSleeveArt = CreateRigArt("Art Left Upper Arm", _leftUpperArmJoint,
                FindPuppetArt("puppet_left_upper_arm"), 1.03f, 9, -7.2f);
            CreateRigArt("Art Left Forearm", _leftForearmJoint,
                FindPuppetArt("puppet_left_forearm"), 0.89f, 10, 5.8f);
            _leftHandArt = CreateRigArt("Art Left Hand", _leftWristJoint,
                FindPuppetArt("puppet_left_hand"), 0.42f, 12);

            _rightSleeveArt = CreateRigArt("Art Right Upper Arm", _rightUpperArmJoint,
                FindPuppetArt("puppet_right_upper_arm"), 1.02f, 9, -1.4f);
            CreateRigArt("Art Right Forearm", _rightForearmJoint,
                FindPuppetArt("puppet_right_forearm"), 0.90f, 10, -1.4f);
            _rightHandArt = CreateRigArt("Art Right Hand", _rightWristJoint,
                FindPuppetArt("puppet_right_hand"), 0.42f, 12);
            _leftDefaultHandSprite = _leftHandArt.GetComponent<SpriteRenderer>().sprite;
            _rightDefaultHandSprite = _rightHandArt.GetComponent<SpriteRenderer>().sprite;

            _leftThighArt = CreateRigArt("Art Left Thigh", _leftThighJoint,
                FindPuppetArt("puppet_left_thigh"), 1.08f, 6, 1.5f);
            _leftShinArt = CreateRigArt("Art Left Shin", _leftShinJoint,
                FindPuppetArt("puppet_left_shin"), 0.99f, 7);
            _leftShoeArt = CreateRigArt("Art Left Shoe", _leftAnkleJoint,
                FindPuppetArt("puppet_left_shoe"), 0.24f, 8);

            _rightThighArt = CreateRigArt("Art Right Thigh", _rightThighJoint,
                FindPuppetArt("puppet_right_thigh"), 1.07f, 6);
            _rightShinArt = CreateRigArt("Art Right Shin", _rightShinJoint,
                FindPuppetArt("puppet_right_shin"), 0.99f, 7, -5.4f);
            _rightShoeArt = CreateRigArt("Art Right Shoe", _rightAnkleJoint,
                FindPuppetArt("puppet_right_shoe"), 0.24f, 8);

            _pelvisArtBaseScale = _pelvisArt.localScale;
            _leftSleeveArtBaseScale = _leftSleeveArt.localScale;
            _rightSleeveArtBaseScale = _rightSleeveArt.localScale;
            _leftHandArtBaseScale = _leftHandArt.localScale;
            _rightHandArtBaseScale = _rightHandArt.localScale;
            _leftThighArtBaseScale = _leftThighArt.localScale;
            _leftShinArtBaseScale = _leftShinArt.localScale;
            _leftShoeArtBaseScale = _leftShoeArt.localScale;
            _rightThighArtBaseScale = _rightThighArt.localScale;
            _rightShinArtBaseScale = _rightShinArt.localScale;
            _rightShoeArtBaseScale = _rightShoeArt.localScale;
            _leftHandArtBaseRotation = _leftHandArt.localRotation;
            _rightHandArtBaseRotation = _rightHandArt.localRotation;
            _usesSegmentedPuppetArt = true;
            SyncSegmentedHandArt();
        }

        private Sprite FindPuppetArt(string spriteName)
        {
            for (int i = 0; i < _puppetArtSprites.Length; i++)
            {
                if (_puppetArtSprites[i].name == spriteName) return _puppetArtSprites[i];
            }

            return null;
        }

        private Sprite FindV2HandArt(PuppetHandShape shape)
        {
            string expectedName = shape switch
            {
                PuppetHandShape.NaturalPalm => "puppet_hand_natural_v2",
                PuppetHandShape.SupportPalm => "puppet_hand_support_v2",
                PuppetHandShape.DirectionPalm => "puppet_hand_point_v2",
                PuppetHandShape.ClosedPalm => "puppet_hand_fist_v2",
                _ => "puppet_hand_natural_v2"
            };
            for (int i = 0; i < _v2HandSprites.Length; i++)
                if (_v2HandSprites[i].name == expectedName) return _v2HandSprites[i];
            return null;
        }

        private void ApplyV2HandShapes(PuppetHandShape left, PuppetHandShape right)
        {
            if (!_usesSegmentedPuppetArt || _v2HandSprites.Length != 4) return;
            Sprite leftSprite = FindV2HandArt(left);
            Sprite rightSprite = FindV2HandArt(right);
            if (leftSprite == null || rightSprite == null) return;

            _leftHandArt.GetComponent<SpriteRenderer>().sprite = leftSprite;
            _rightHandArt.GetComponent<SpriteRenderer>().sprite = rightSprite;
            _leftHandArt.localRotation = _leftHandArtBaseRotation * _leftFingerJoint.localRotation *
                Quaternion.Euler(0f, 0f, 90f);
            _rightHandArt.localRotation = _rightHandArtBaseRotation * _rightFingerJoint.localRotation *
                Quaternion.Euler(0f, 0f, -90f);
            // 四张图保持同一 PPU 与同一腕铆点，统一比例避免切换手型时掌片跳动。
            _leftHandArt.localScale = new Vector3(0.08f, 0.08f, 1f);
            _rightHandArt.localScale = new Vector3(-0.08f, 0.08f, 1f);
        }

        private void RestoreDefaultHandSprites()
        {
            if (_leftHandArt == null || _rightHandArt == null ||
                _leftDefaultHandSprite == null || _rightDefaultHandSprite == null) return;
            _leftHandArt.GetComponent<SpriteRenderer>().sprite = _leftDefaultHandSprite;
            _rightHandArt.GetComponent<SpriteRenderer>().sprite = _rightDefaultHandSprite;
        }

        private Transform CreateRigArt(
            string objectName,
            Transform joint,
            Sprite sprite,
            float targetHeight,
            int sortingOrder,
            float localRotation = 0f)
        {
            var artObject = new GameObject(objectName);
            artObject.transform.SetParent(joint, false);
            float sourceHeight = Mathf.Max(0.001f, sprite.bounds.size.y);
            artObject.transform.localScale = Vector3.one * (targetHeight / sourceHeight);
            artObject.transform.localRotation = Quaternion.Euler(0f, 0f, localRotation);
            var renderer = artObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            PuppetArtRendererCount++;
            return artObject.transform;
        }

        private void UpdateArticulatedDetails(double elapsed)
        {
            float blend = 1f - Mathf.Exp(-5f * (float)System.Math.Min(elapsed, 0.5d));
            float leftWrist = Mathf.Clamp(Mathf.DeltaAngle(0f, _leftForearmJoint.localEulerAngles.z) * 0.22f, -24f, 24f);
            float rightWrist = Mathf.Clamp(Mathf.DeltaAngle(0f, _rightForearmJoint.localEulerAngles.z) * 0.22f, -24f, 24f);
            if (_dancePlayback != null && _dancePlayback.HasExplicitLeftHandPose)
            {
                leftWrist = (float)_dancePlayback.LeftWristAngle;
                SetRotation(_leftWristJoint, leftWrist);
                SetRotation(_leftFingerJoint, _dancePlayback.LeftFingerAngle);
                _leftFingerJoint.localScale = new Vector3((float)_dancePlayback.LeftFingerWidth,
                    (float)_dancePlayback.LeftFingerLength, 1f);
                ApplyLeftPointFinger((float)_dancePlayback.LeftPointFingerAmount);
            }
            else
            {
                EaseRotation(_leftWristJoint, -leftWrist, blend);
                EaseRotation(_leftFingerJoint, leftWrist * 0.45f, blend * 0.7f);
                _leftFingerJoint.localScale = Vector3.Lerp(_leftFingerJoint.localScale,
                    Vector3.one, blend);
                ApplyLeftPointFinger(0f);
            }
            if (_dancePlayback != null && _dancePlayback.HasExplicitRightHandPose)
            {
                rightWrist = (float)_dancePlayback.RightWristAngle;
                SetRotation(_rightWristJoint, rightWrist);
                SetRotation(_rightFingerJoint, _dancePlayback.RightFingerAngle);
                float closure = (float)_dancePlayback.RightHandClosure;
                _rightFingerJoint.localScale = new Vector3(1f - (0.48f * closure),
                    1f - (0.30f * closure), 1f);
            }
            else
            {
                EaseRotation(_rightWristJoint, -rightWrist, blend);
                EaseRotation(_rightFingerJoint, rightWrist * 0.45f, blend * 0.7f);
                _rightFingerJoint.localScale = Vector3.Lerp(_rightFingerJoint.localScale,
                    Vector3.one, blend);
            }
            SyncSegmentedHandArt();
            EaseRotation(_leftSleeveTail, -leftWrist * 0.85f, blend * 0.45f);
            EaseRotation(_rightSleeveTail, -rightWrist * 0.85f, blend * 0.45f);
        }

        private void ApplyGroundedLegs(Vector2 leftAnkle, Vector2 rightAnkle)
        {
            ApplyGroundedLeg(_leftThighJoint, _leftShinJoint, _leftAnkleJoint, leftAnkle);
            ApplyGroundedLeg(_rightThighJoint, _rightShinJoint, _rightAnkleJoint, rightAnkle);
        }

        private void ApplyGroundedLeg(Transform thigh, Transform shin, Transform ankle, Vector2 stageTarget)
        {
            ApplyGroundedLeg(thigh, shin, ankle, stageTarget, 1f);
        }

        private void ApplyGroundedLeg(Transform thigh, Transform shin, Transform ankle,
            Vector2 stageTarget, float bendSide)
        {
            Vector3 targetWorld = _visualRoot.TransformPoint(stageTarget);
            Vector2 targetLocal = _pelvisJoint.InverseTransformPoint(targetWorld);
            NorthernShadowLegSolver.Solve(thigh.localPosition, targetLocal, bendSide,
                out float hipDegrees, out float kneeDegrees);
            SetRotation(thigh, hipDegrees);
            SetRotation(shin, kneeDegrees);
            SetRotation(ankle, -(hipDegrees + kneeDegrees));
        }

        private static void EaseRotation(Transform joint, float target, float blend)
        {
            float current = Mathf.DeltaAngle(0f, joint.localEulerAngles.z);
            SetRotation(joint, Mathf.LerpAngle(current, target, blend));
        }

        private static float Smooth(float t) => t * t * (3f - (2f * t));

        private static Vector3 MirrorX(Vector3 baseScale, float facing) =>
            new Vector3(baseScale.x * facing, baseScale.y, baseScale.z);

        private void RestoreV2LowerArtScale()
        {
            V2VisibleFacing = 1f;
            _leftThighArt.localScale = _leftThighArtBaseScale;
            _leftShinArt.localScale = _leftShinArtBaseScale;
            _leftShoeArt.localScale = _leftShoeArtBaseScale;
            _rightThighArt.localScale = _rightThighArtBaseScale;
            _rightShinArt.localScale = _rightShinArtBaseScale;
            _rightShoeArt.localScale = _rightShoeArtBaseScale;
            ApplyV2FacingSorting(false);
        }

        private void ApplyV2FacingSorting(bool reversed)
        {
            SetArtOrder(_leftSleeveArt, reversed ? 9 : 13);
            SetArtOrder(_leftForearmJoint.Find("Art Left Forearm"), reversed ? 10 : 14);
            SetArtOrder(_leftHandArt, reversed ? 11 : 15);
            SetArtOrder(_rightSleeveArt, reversed ? 13 : 9);
            SetArtOrder(_rightForearmJoint.Find("Art Right Forearm"), reversed ? 14 : 10);
            SetArtOrder(_rightHandArt, reversed ? 15 : 11);
            SetArtOrder(_leftThighArt, reversed ? 6 : 9);
            SetArtOrder(_leftShinArt, reversed ? 7 : 10);
            SetArtOrder(_leftShoeArt, reversed ? 8 : 11);
            SetArtOrder(_rightThighArt, reversed ? 9 : 6);
            SetArtOrder(_rightShinArt, reversed ? 10 : 7);
            SetArtOrder(_rightShoeArt, reversed ? 11 : 8);
        }

        private static void SetArtOrder(Transform art, int order)
        {
            if (art != null) art.GetComponent<SpriteRenderer>().sortingOrder = order;
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
                bool bodyRodBehind = _v2TurnActive && lane == 4 &&
                    _v2TurnProgress > 0.16f && _v2TurnProgress < 0.84f;
                if (_v2TurnActive && lane == 4)
                {
                    // 主杆握端绕角色背面走一整圈投影：右侧起、左后方通过、回到右侧。
                    float orbit = _v2TurnProgress * Mathf.PI * 2f;
                    restGrip = new Vector3(0.55f * Mathf.Cos(orbit),
                        -3.05f + (0.18f * Mathf.Sin(orbit)), 0f);
                }
                Vector3 target = _visualRoot.InverseTransformPoint(_rodTargets[lane].position);
                Vector3 direction = (target - restGrip).normalized;
                Vector3 normal = new Vector3(-direction.y, direction.x, 0f);
                float holdPulse = _heldRods[lane]
                    ? (_dancePlayback != null ? 0.06f
                        : 0.10f * Mathf.Sin((float)(_songTime * Mathf.PI * 5.0d) + lane))
                    : 0f;
                Vector3 grip = restGrip + (direction * ((_rodDrive[lane] * 0.18f) + holdPulse));
                grip += normal * (holdPulse * 0.35f);
                _rods[lane].SetPosition(0, grip);
                _rods[lane].SetPosition(1, target);
                _rods[lane].sortingOrder = bodyRodBehind ? 1 : 9;
                if (_rodGrips[lane] != null)
                {
                    if (lane == 4)
                        _rodGrips[lane].transform.localPosition = _v2TurnActive ? grip : RodGripPoints[lane];
                    _rodGrips[lane].sortingOrder = bodyRodBehind ? 1 : 10;
                }
                bool principalRod = lane == 0 || lane == 2 || lane == 4;
                Color color = Color.Lerp(new Color(0.30f, 0.13f, 0.045f, principalRod ? 0.48f : 0.27f),
                    new Color(0.78f, 0.34f, 0.07f, principalRod ? 1f : 0.68f), _rodDrive[lane]);
                _rods[lane].startColor = color;
                _rods[lane].endColor = color;
                _rods[lane].startWidth = Mathf.Lerp(principalRod ? 0.065f : 0.038f,
                    principalRod ? 0.095f : 0.056f, _rodDrive[lane]) + (_heldRods[lane] ? 0.012f : 0f);
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

        private void ApplyLeftPointFinger(float amount)
        {
            amount = Mathf.Clamp01(amount);
            _leftPointFingerAmount = amount;
            _leftPointFinger.gameObject.SetActive(amount > 0.01f);
            _leftPointFinger.localScale = new Vector3(0.075f * amount, 0.52f * amount, 1f);
        }

        private void SyncSegmentedHandArt()
        {
            if (!_usesSegmentedPuppetArt || _leftHandArt == null || _rightHandArt == null) return;

            Vector3 leftGestureScale = _leftFingerJoint.localScale;
            // 單指原本由獨立的舊剪影顯示。新美術只有完整手掌，因此在美術適配層
            // 將同一張手掌收窄、延長；动作轨迹和手势参数保持不变。
            leftGestureScale.x = Mathf.Lerp(leftGestureScale.x, 0.34f, _leftPointFingerAmount);
            leftGestureScale.y = Mathf.Lerp(leftGestureScale.y, 1.22f, _leftPointFingerAmount);
            _leftHandArt.localRotation = _leftHandArtBaseRotation * _leftFingerJoint.localRotation;
            _leftHandArt.localScale = Vector3.Scale(_leftHandArtBaseScale, leftGestureScale);

            _rightHandArt.localRotation = _rightHandArtBaseRotation * _rightFingerJoint.localRotation;
            _rightHandArt.localScale = Vector3.Scale(_rightHandArtBaseScale, _rightFingerJoint.localScale);
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
            if (_backgroundSprite != null) Destroy(_backgroundSprite);
            if (_squareTexture != null) Destroy(_squareTexture);
            if (_circleTexture != null) Destroy(_circleTexture);
        }
    }
}
