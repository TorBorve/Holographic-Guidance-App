using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine.Events;
using TMPro;


namespace Tutorials
{
    /// <summary>
    /// Monobehaviour class that offers control over the hand recording.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    public class Recorder : MonoBehaviour
    {
        [SerializeField]
        private GameObject handLeftPrefab;
        private GameObject recordingHandLeft;

        [SerializeField]
        private GameObject handRightPrefab;
        private GameObject recordingHandRight;

        [SerializeField]
        private ObjectManager objectManager;
        private Dictionary<GameObject, GameObject> objectList;

        [SerializeField]
        [Tooltip("The point of reference that can be set for each animation specifically (will be stored in animation entity). Default is (0,0,0),(0,0,0,1)")]
        private Transform animationSpecificPointOfReference;

        public TextMeshPro recordingCountdownText;


        /// <summary>
        /// Event raised when input recording is started.
        /// </summary>
        public UnityEvent OnRecordingStarted = new UnityEvent();

        /// <summary>
        /// Event raised when input recording is stopped.
        /// </summary>
        public UnityEvent OnRecordingStopped = new UnityEvent();

        private static float FRAMERATE = 60f;

        private static Vector3 ModelFingerPointing = new Vector3(0, -1, 0);

        private static Vector3 ModelPalmFacing = new Vector3(-1, 0, 0);

        // transforms for the root joints for each finger of each hand
        Transform leftHand;
        Transform leftHandRoot;
        Transform leftWrist;
        Transform leftThumbRoot;
        Transform leftIndexRoot;
        Transform leftMiddleRoot;
        Transform leftRingRoot;
        Transform leftPinkyRoot;

        Transform rightHand;
        Transform rightHandRoot;
        Transform rightWrist;
        Transform rightThumbRoot;
        Transform rightIndexRoot;
        Transform rightMiddleRoot;
        Transform rightRingRoot;
        Transform rightPinkyRoot;


        private Dictionary<TrackedHandJoint, Transform> leftJoints = new Dictionary<TrackedHandJoint, Transform>();
        private Dictionary<TrackedHandJoint, Transform> rightJoints = new Dictionary<TrackedHandJoint, Transform>();


        private IMixedRealityEyeGazeProvider eyeGazeProvider;

        private static string RECORDINGS_DIRECTORY = "Recordings";


        private void Awake()
        {
            // todo: move this to some utility class
            string path = Path.Combine(Application.persistentDataPath, RECORDINGS_DIRECTORY);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Set the objectList variable to a collection of instantiated objects currently active and meant to be tracked
        /// </summary>
        private void SetActiveObjectList()
        {
            objectList = new Dictionary<GameObject, GameObject>();
            List<GameObject> spawnedObjects = objectManager.GetSpawnedObjects();
            foreach (GameObject obj in spawnedObjects)
            {
                GameObject clone = Instantiate(obj,
                    animationSpecificPointOfReference);
                clone.SetActive(false);
                objectList.Add(obj, clone);
                Debug.Log($"Object position at recording: {clone.transform.position}");
            }
        }

        private void Start()
        {
            // instatiate the (invisible) hand model that is used by the recording service
            recordingHandLeft = Instantiate(handLeftPrefab, animationSpecificPointOfReference);
            recordingHandLeft.SetActive(false);
            recordingHandLeft.name = "RecorderLeftHand";

            // instatiate the (invisible) hand model that is used by the recording service
            recordingHandRight = Instantiate(handRightPrefab, animationSpecificPointOfReference);
            recordingHandRight.SetActive(false);
            recordingHandRight.name = "RecorderRightHand";

            InitializeDictionary(recordingHandLeft.transform, recordingHandRight.transform);
        }

        public Transform getRecordingInvisibleHandTransform(Handedness handedness)
        {
            if (handedness == Handedness.Left)
            {
                return recordingHandLeft.transform;
            }
            else
            {
                return recordingHandRight.transform;
            }
        }

        /// <summary>
        /// Creates a new animation wrapper that will be opened in the Editor. 
        /// The animation specific point of reference will be reset to the global origin that is either at the MixedReality
        /// space's origin or the origin that has been set through QR Code calibration. 
        /// </summary>
        public AnimationWrapper CreateNewAnimationWrapper()
        {
            animationSpecificPointOfReference.localPosition = Vector3.zero;
            animationSpecificPointOfReference.localRotation = Quaternion.identity;
            return FileHandler.AnimationListInstance.CreateNewAnimationEntity(null, animationSpecificPointOfReference);
        }


        /// <summary>
        /// Start Input Recording.
        /// </summary>
        public void StartRecording()
        {
            IsRecording = true;

            eyeGazeProvider = CoreServices.InputSystem.EyeGazeProvider;

            frameRate = FRAMERATE;
            frameInterval = 1f / frameRate;
            nextFrame = Time.time + frameInterval;

            if (UseBufferTimeLimit)
            {
                PruneBuffer();
            }
            if (!unlimitedRecordingStartTime.HasValue)
            {
                unlimitedRecordingStartTime = Time.time;
            }

            // Get all active objects to track in the scene
            SetActiveObjectList();

            OnRecordingStarted.Invoke();
        }


        /// <summary>
        /// Stop Input Recording.
        /// </summary>
        public void StopRecording()
        {
            if (IsRecording)
            {
                IsRecording = false;
                OnRecordingStopped.Invoke();
            }
        }

        /// <summary>
        /// Export recorded input.
        /// </summary>
        public void SaveRecordedInput()
        {
            if (IsRecording)
            {
                StopRecording();

                try
                {
                    FileHandler.AnimationListInstance.OverwriteAnimationData(InputAnimation.FromRecordingBuffer(recordingBuffer), animationSpecificPointOfReference);
                    DiscardRecordedInput();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }
        }

        /// <summary>
        /// Closes the current animation and removes it from the open animation list
        /// </summary>
        public void CloseAnimation()
        {
            FileHandler.AnimationListInstance.RemoveCurrentAnimation();
        }

        /// <summary>
        /// Cancels recording if IsRecording is true (i.e. if the recording service is currently actively recording hand data)
        /// </summary>
        public void Cancel()
        {
            if (IsRecording)
            {
                StopRecording();
                DiscardRecordedInput();
            }
        }

        /// <summary>
        /// Adds a user visible description to the animation
        /// </summary>
        public void NameCurrentAnimation(string description)
        {
            if (!IsRecording)
            {
                AnimationWrapper animationWrapper = FileHandler.AnimationListInstance.GetCurrentAnimationWrapper();
                if (animationWrapper == null)
                {
                    animationWrapper = CreateNewAnimationWrapper();
                }

                animationWrapper.Description = description;
                
                try
                {
                    FileHandler.AnimationListInstance.OverwriteAnimationData(animationWrapper.Animation, animationSpecificPointOfReference);
                    DiscardRecordedInput();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }
           
        }

        /// <summary>
        /// Initializes the dictionary that contains all transforms in the given handmodels and matches the transforms with the corresponding TrackedHandJoint
        /// </summary>
        /// <param name="handLeft">The left hand.</param>
        /// <param name="handRight">The right hand.</param>
        /// <seealso cref="TrackedHandJoint"/>
        public void InitializeDictionary(Transform handLeft, Transform handRight)
        {
            leftHand = handLeft;
            leftHandRoot = leftHand.GetChild(0);
            leftWrist = leftHandRoot.GetChild(1);
            leftThumbRoot = leftWrist.GetChild(3);
            leftIndexRoot = leftWrist.GetChild(1);
            leftMiddleRoot = leftWrist.GetChild(0);
            leftRingRoot = leftWrist.GetChild(2);
            leftPinkyRoot = leftWrist.GetChild(4).GetChild(0); // Wrist padding 

            rightHand = handRight;
            rightHandRoot = rightHand.GetChild(0);
            rightWrist = rightHandRoot.GetChild(1);
            rightThumbRoot = rightWrist.GetChild(3);
            rightIndexRoot = rightWrist.GetChild(1);
            rightMiddleRoot = rightWrist.GetChild(0);
            rightRingRoot = rightWrist.GetChild(2);
            rightPinkyRoot = rightWrist.GetChild(4).GetChild(0);

            //LEFT
            {
                // Initialize joint dictionary with their corresponding joint transforms
                leftJoints[TrackedHandJoint.Wrist] = leftWrist;

                // Thumb joints, first node is user assigned, note that there are only 4 joints in the thumb
                if (leftThumbRoot)
                {
                    leftJoints[TrackedHandJoint.ThumbMetacarpalJoint] = leftThumbRoot;
                    leftJoints[TrackedHandJoint.ThumbProximalJoint] = RetrieveChild(TrackedHandJoint.ThumbMetacarpalJoint, Handedness.Left);
                    leftJoints[TrackedHandJoint.ThumbDistalJoint] = RetrieveChild(TrackedHandJoint.ThumbProximalJoint, Handedness.Left);
                    leftJoints[TrackedHandJoint.ThumbTip] = RetrieveChild(TrackedHandJoint.ThumbDistalJoint, Handedness.Left);
                }
                // Look up index finger joints below the index finger root joint
                if (leftIndexRoot)
                {
                    //leftJoints[TrackedHandJoint.IndexMetacarpal] = leftIndexRoot;
                    leftJoints[TrackedHandJoint.IndexKnuckle] = leftIndexRoot;
                    leftJoints[TrackedHandJoint.IndexMiddleJoint] = RetrieveChild(TrackedHandJoint.IndexKnuckle, Handedness.Left);
                    leftJoints[TrackedHandJoint.IndexDistalJoint] = RetrieveChild(TrackedHandJoint.IndexMiddleJoint, Handedness.Left);
                    leftJoints[TrackedHandJoint.IndexTip] = RetrieveChild(TrackedHandJoint.IndexDistalJoint, Handedness.Left);
                }

                // Look up middle finger joints below the middle finger root joint
                if (leftMiddleRoot)
                {
                    //leftJoints[TrackedHandJoint.MiddleMetacarpal] = leftMiddleRoot;
                    leftJoints[TrackedHandJoint.MiddleKnuckle] = leftMiddleRoot;
                    leftJoints[TrackedHandJoint.MiddleMiddleJoint] = RetrieveChild(TrackedHandJoint.MiddleKnuckle, Handedness.Left);
                    leftJoints[TrackedHandJoint.MiddleDistalJoint] = RetrieveChild(TrackedHandJoint.MiddleMiddleJoint, Handedness.Left);
                    leftJoints[TrackedHandJoint.MiddleTip] = RetrieveChild(TrackedHandJoint.MiddleDistalJoint, Handedness.Left);
                }

                // Look up ring finger joints below the ring finger root joint
                if (leftRingRoot)
                {
                    //leftJoints[TrackedHandJoint.RingMetacarpal] = leftRingRoot;
                    leftJoints[TrackedHandJoint.RingKnuckle] = leftRingRoot;
                    leftJoints[TrackedHandJoint.RingMiddleJoint] = RetrieveChild(TrackedHandJoint.RingKnuckle, Handedness.Left);
                    leftJoints[TrackedHandJoint.RingDistalJoint] = RetrieveChild(TrackedHandJoint.RingMiddleJoint, Handedness.Left);
                    leftJoints[TrackedHandJoint.RingTip] = RetrieveChild(TrackedHandJoint.RingDistalJoint, Handedness.Left);
                }

                // Look up pinky joints below the pinky root joint
                if (leftPinkyRoot)
                {
                    //leftJoints[TrackedHandJoint.PinkyMetacarpal] = leftPinkyRoot;
                    leftJoints[TrackedHandJoint.PinkyKnuckle] = leftPinkyRoot;
                    leftJoints[TrackedHandJoint.PinkyMiddleJoint] = RetrieveChild(TrackedHandJoint.PinkyKnuckle, Handedness.Left);
                    leftJoints[TrackedHandJoint.PinkyDistalJoint] = RetrieveChild(TrackedHandJoint.PinkyMiddleJoint, Handedness.Left);
                    leftJoints[TrackedHandJoint.PinkyTip] = RetrieveChild(TrackedHandJoint.PinkyDistalJoint, Handedness.Left);
                }
            }

            //RIGHT
            {
                {
                    // Initialize joint dictionary with their corresponding joint transforms
                    rightJoints[TrackedHandJoint.Wrist] = rightWrist;

                    // Thumb joints, first node is user assigned, note that there are only 4 joints in the thumb
                    if (rightThumbRoot)
                    {
                        rightJoints[TrackedHandJoint.ThumbMetacarpalJoint] = rightThumbRoot;
                        rightJoints[TrackedHandJoint.ThumbProximalJoint] = RetrieveChild(TrackedHandJoint.ThumbMetacarpalJoint, Handedness.Right);
                        rightJoints[TrackedHandJoint.ThumbDistalJoint] = RetrieveChild(TrackedHandJoint.ThumbProximalJoint, Handedness.Right);
                        rightJoints[TrackedHandJoint.ThumbTip] = RetrieveChild(TrackedHandJoint.ThumbDistalJoint, Handedness.Right);
                    }
                    // Look up index finger joints below the index finger root joint
                    if (rightIndexRoot)
                    {
                        //rightJoints[TrackedHandJoint.IndexMetacarpal] = rightIndexRoot;
                        rightJoints[TrackedHandJoint.IndexKnuckle] = rightIndexRoot;
                        rightJoints[TrackedHandJoint.IndexMiddleJoint] = RetrieveChild(TrackedHandJoint.IndexKnuckle, Handedness.Right);
                        rightJoints[TrackedHandJoint.IndexDistalJoint] = RetrieveChild(TrackedHandJoint.IndexMiddleJoint, Handedness.Right);
                        rightJoints[TrackedHandJoint.IndexTip] = RetrieveChild(TrackedHandJoint.IndexDistalJoint, Handedness.Right);
                    }

                    // Look up middle finger joints below the middle finger root joint
                    if (rightMiddleRoot)
                    {
                        //rightJoints[TrackedHandJoint.MiddleMetacarpal] = rightMiddleRoot;
                        rightJoints[TrackedHandJoint.MiddleKnuckle] = rightMiddleRoot;
                        rightJoints[TrackedHandJoint.MiddleMiddleJoint] = RetrieveChild(TrackedHandJoint.MiddleKnuckle, Handedness.Right);
                        rightJoints[TrackedHandJoint.MiddleDistalJoint] = RetrieveChild(TrackedHandJoint.MiddleMiddleJoint, Handedness.Right);
                        rightJoints[TrackedHandJoint.MiddleTip] = RetrieveChild(TrackedHandJoint.MiddleDistalJoint, Handedness.Right);
                    }

                    // Look up ring finger joints below the ring finger root joint
                    if (rightRingRoot)
                    {
                        //rightJoints[TrackedHandJoint.RingMetacarpal] = rightRingRoot;
                        rightJoints[TrackedHandJoint.RingKnuckle] = rightRingRoot;
                        rightJoints[TrackedHandJoint.RingMiddleJoint] = RetrieveChild(TrackedHandJoint.RingKnuckle, Handedness.Right);
                        rightJoints[TrackedHandJoint.RingDistalJoint] = RetrieveChild(TrackedHandJoint.RingMiddleJoint, Handedness.Right);
                        rightJoints[TrackedHandJoint.RingTip] = RetrieveChild(TrackedHandJoint.RingDistalJoint, Handedness.Right);
                    }

                    // Look up pinky joints below the pinky root joint
                    if (rightPinkyRoot)
                    {
                        //rightJoints[TrackedHandJoint.PinkyMetacarpal] = rightPinkyRoot;
                        rightJoints[TrackedHandJoint.PinkyKnuckle] = rightPinkyRoot;
                        rightJoints[TrackedHandJoint.PinkyMiddleJoint] = RetrieveChild(TrackedHandJoint.PinkyKnuckle, Handedness.Right);
                        rightJoints[TrackedHandJoint.PinkyDistalJoint] = RetrieveChild(TrackedHandJoint.PinkyMiddleJoint, Handedness.Right);
                        rightJoints[TrackedHandJoint.PinkyTip] = RetrieveChild(TrackedHandJoint.PinkyDistalJoint, Handedness.Right);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the child joint for a given parent joint.
        /// </summary>
        /// <param name="parentJoint">The parent joint.</param>
        /// <param name="handedness">The handedness (left or right).</param>
        /// <returns></returns>
        private Transform RetrieveChild(TrackedHandJoint parentJoint, Handedness handedness)
        {
            if (handedness == Handedness.Left)
            {
                if (leftJoints[parentJoint] != null && leftJoints[parentJoint].childCount > 0)
                {
                    return leftJoints[parentJoint].GetChild(0);
                }
            }
            else if (handedness == Handedness.Right)
            {
                if (rightJoints[parentJoint] != null && rightJoints[parentJoint].childCount > 0)
                {
                    return rightJoints[parentJoint].GetChild(0);
                }
            }

            return null;
        }


        private static Quaternion Reorientation()
        {
            return Quaternion.Inverse(Quaternion.LookRotation(ModelFingerPointing, -ModelPalmFacing));
        }

        public static Quaternion RecordingFrameInverse(Handedness handedness)
        {
            var rot = new Quaternion();
            if (handedness == Handedness.Right)
            {
                rot = Quaternion.Inverse(Reorientation() * Quaternion.Euler(0, 0, -90));
            }
            else
            {
                rot = Quaternion.Inverse(Reorientation() * Quaternion.Euler(0, 0, 90));
            }
            return rot;
        }

        public bool IsRecording { get; private set; } = false;

        private bool useBufferTimeLimit = false;
        public bool UseBufferTimeLimit
        {
            get { return useBufferTimeLimit; }
            set
            {
                if (useBufferTimeLimit && !value)
                {
                    // Start at buffer limit when making buffer unlimited
                    unlimitedRecordingStartTime = StartTime;
                }

                useBufferTimeLimit = value;

                if (useBufferTimeLimit)
                {
                    PruneBuffer();
                }
            }
        }

        private float recordingBufferTimeLimit = 60.0f;
        public float RecordingBufferTimeLimit
        {
            get { return recordingBufferTimeLimit; }
            set
            {
                recordingBufferTimeLimit = Mathf.Max(value, 0.0f);

                if (useBufferTimeLimit)
                {
                    PruneBuffer();
                }
            }
        }

        // Start time of recording if buffer is unlimited.
        // Nullable to determine when time needs to be reset.
        private float? unlimitedRecordingStartTime = null;
        /// <summary>
        /// Start time of recording.
        /// </summary>
        public float StartTime
        {
            get
            {
                if (unlimitedRecordingStartTime.HasValue)
                {
                    if (useBufferTimeLimit)
                    {
                        return Mathf.Max(unlimitedRecordingStartTime.Value, EndTime - recordingBufferTimeLimit);
                    }
                    else
                    {
                        return unlimitedRecordingStartTime.Value;
                    }
                }

                return EndTime;
            }
        }

        /// <summary>
        /// End time of recording.
        /// </summary>
        public float EndTime { get; private set; }


        private float frameRate;
        private float frameInterval;
        private float nextFrame;

        private InputRecordingBuffer recordingBuffer = null;

        public InputRecordingBuffer RecordingBuffer
        {
            get
            {
                if (recordingBuffer == null)
                {
                    recordingBuffer = new InputRecordingBuffer();
                }
                return recordingBuffer;
            }
            set => recordingBuffer = value;
        }



        /// <summary>
        /// After both hand models have been moved and adjusted to the user's hand input (like a glove) the pose of each individual joint is saved in a keyframe.
        /// Note: This "glove" behaviour is a simple way of finding out the relative positions and rotations of each joints.
        /// The hand input from MRTK only provides global joint positions and rotations, making this workaround necessary.
        /// This way the local position and rotation relative to the animation specific point of reference can be obtained. 
        /// </summary>
        public void LateUpdate()
        {
            if (IsRecording && Time.time > nextFrame)
            {
                EndTime = Time.time;
                nextFrame += frameInterval * (Mathf.Floor((Time.time - nextFrame) * frameRate) + 1f);

                if (UseBufferTimeLimit)
                {
                    PruneBuffer();
                }

                RecordKeyframe();
            }
        }

        /// <summary>
        /// Discards the recorded input.
        /// </summary>
        public void DiscardRecordedInput()
        {
            RecordingBuffer.Clear();
            ResetStartTime();
        }


        /// <summary>
        /// Resets the start time.
        /// </summary>
        private void ResetStartTime()
        {
            if (IsRecording)
            {
                unlimitedRecordingStartTime = Time.time;
            }
            else
            {
                unlimitedRecordingStartTime = null;
            }
        }

        /// <summary>
        /// Record a keyframe with (local) position and rotation data of the hand models that were adjusted to the user's handinput in this frame. 
        /// </summary>
        private void RecordKeyframe()
        {
            float time = Time.time;

            RecordingBuffer.NewKeyframe(time);

            RecordInputHandData(Handedness.Left);
            RecordInputHandData(Handedness.Right);
            RecordInputObjects();
        }

        /// <summary>
        /// Record a keyframe at the given time for a hand with the given handedness it is tracked.
        /// </summary>
        private void RecordInputHandData(Handedness handedness)
        {
            var hand = HandJointUtils.FindHand(handedness);
            if (hand == null)
            {
                RecordingBuffer.SetHandState(handedness, false, false);

                return;
            }

            bool isTracked = (hand.TrackingState == TrackingState.Tracked);

            // check if hand pinched. this will also be recorded, but will not be displayed in a specific way in the playback service
            bool isPinching = false;
            for (int i = 0; i < hand.Interactions?.Length; i++)
            {
                var interaction = hand.Interactions[i];
                switch (interaction.InputType)
                {
                    case DeviceInputType.Select:
                        isPinching = interaction.BoolData;
                        break;
                }
            }

            RecordingBuffer.SetHandState(handedness, isTracked, isPinching);

            // only record hand data if this hand has been tracked in this frame
            if (isTracked)
            {

                // prepare for live updates of hands
                Dictionary<TrackedHandJoint, Transform> tempJoints;

                if (handedness == Handedness.Left)
                {
                    tempJoints = leftJoints;
                }
                else if (handedness == Handedness.Right)
                {
                    tempJoints = rightJoints;
                }
                else return;

                Transform jointTransform;

                // go through each hand joint and update the corresponding transform's position and rotation. 
                foreach (TrackedHandJoint handJoint in tempJoints.Keys)
                {
                    if (tempJoints.TryGetValue(handJoint, out jointTransform))
                    {
                        if (hand.TryGetJoint(handJoint, out MixedRealityPose jointPose))
                        {
                            if (handJoint == TrackedHandJoint.Wrist)
                            {
                                jointTransform.position = jointPose.Position;
                            } else
                            {
                                jointTransform.position = jointPose.Position;
                            }

                            // Model specific rotation
                            if (handedness == Handedness.Right)
                            {
                                jointTransform.rotation = jointPose.Rotation * Reorientation() * Quaternion.Euler(0, 0, -90);
                            }
                            else
                            {
                                jointTransform.rotation = jointPose.Rotation * Reorientation() * Quaternion.Euler(0, 0, 90);
                            }



                            RecordingBuffer.SetJointTransform(handedness, handJoint, jointTransform);
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Records the transformations for each object in the current key frame
        /// Note that the positions of the instantiated and recorded objects are recorded in relation to the reference point (therefore local)
        /// However, the tracked (displayed) objects are not in relation to the reference point as we do not want them to change positions when moving
        /// the reference point handle.
        /// </summary>
        private void RecordInputObjects()
        {
            foreach(var obj in objectList)
            {
                obj.Value.transform.localPosition = animationSpecificPointOfReference.InverseTransformPoint(obj.Key.transform.position);
                obj.Value.transform.localRotation = Quaternion.Inverse(animationSpecificPointOfReference.rotation) * obj.Key.transform.rotation;
                obj.Value.transform.localScale = obj.Key.transform.localScale;
                RecordingBuffer.SetObjectState(obj.Value.transform);
            }
        }

        /// Discard keyframes before the cutoff time.
        private void PruneBuffer()
        {
            RecordingBuffer.RemoveBeforeTime(StartTime);
        }

        public InputRecordingBuffer.Keyframe GetLatestKeyFrame()
        {
            return RecordingBuffer.GetLatestKeyframe();
        }
    }

}