using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;



namespace Tutorials
{

    /// <summary>
    /// Monobehaviour class that listens to the FileHandler's Animation List instance and plays any animation from it as soon as the current animation changes. 
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    public class Player : MonoBehaviour
    {
        private static readonly int jointCount = Enum.GetNames(typeof(TrackedHandJoint)).Length;


        private bool isPlaying = false;

        /// <summary>
        /// Duration of the played animation.
        /// </summary>
        private float Duration;

        private float localTime = 0.0f;

        private bool setFollowMode = false;

        /// <summary>
        /// Contains meta data 
        /// </summary>
        private AnimationWrapper animationWrapper;

        /// <summary>
        /// The actual animation that is being replayed 
        /// </summary>
        public new InputAnimation animation;

        [SerializeField]
        private Transform animationSpecificPointOfReference;

        [SerializeField]
        [Tooltip("The hand model that will be animated using the animation data of the currently loaded animation. This must match the structure used by RiggedHandVisualizer.")]
        private GameObject riggedHandLeftPrefab;


        /// <summary>
        /// The hand object in the scene, instantiated from riggedHandLeftPrefab at runtime. This will be animated. 
        /// </summary>
        private GameObject riggedHandLeft;

        /// <summary>
        /// The rigged hand visualizer that evaluates the hand pose in each frame
        /// </summary>
        private RiggedHandVisualizer riggedHandVisualizerLeft;

        [SerializeField]
        [Tooltip("The hand model that will be animated using the animation data of the currently loaded animation. This must match the structure used by RiggedHandVisualizer.")]
        private GameObject riggedHandRightPrefab;

        /// <summary>
        /// The hand object in the scene, instantiated from riggedHandLeftPrefab at runtime. This will be animated. 
        /// </summary>
        private GameObject riggedHandRight;

        /// <summary>
        /// The rigged hand visualizer that evaluates the hand pose in each frame
        /// </summary>
        private RiggedHandVisualizer riggedHandVisualizerRight;

        [SerializeField]
        private ObjectManager objectManager;
        /// <summary>
        /// Is set at the start of each animation to the objects to be animated
        /// </summary>
        private List<GameObject> objectList;

        /// <summary>
        /// A list of essential components to retain
        /// </summary>
        public static readonly string[] essentialComponents = { "Transform", "MeshFilter", "MeshRenderer", "ConstraintManager" };

        /// <summary>
        /// Material to be used for the animated objects
        /// </summary>
        [SerializeField]
        private Material animatingMaterial;

        private bool startAgain = false;

        public void Start()
        {
            // instantiating the left hand object that will be animated
            riggedHandLeft = Instantiate(riggedHandLeftPrefab, animationSpecificPointOfReference.position, animationSpecificPointOfReference.rotation);

            riggedHandLeft.SetActive(false);
            riggedHandLeft.transform.parent = animationSpecificPointOfReference;
            riggedHandLeft.name = "PlaybackServiceLeftHand";

            riggedHandVisualizerLeft = riggedHandLeft.GetComponent<RiggedHandVisualizer>();

            // instantiating the right hand object that will be animated
            riggedHandRight = Instantiate(riggedHandRightPrefab, animationSpecificPointOfReference.position, animationSpecificPointOfReference.rotation);

            riggedHandRight.SetActive(false);
            riggedHandRight.transform.parent = animationSpecificPointOfReference;
            riggedHandRight.name = "PlaybackServiceRightHand";
            riggedHandVisualizerRight = riggedHandRight.GetComponent<RiggedHandVisualizer>();
        }

        /// <summary>
        /// Sets the animation that is currently being replay to 100%. 
        /// </summary>
        public void StartAgain()
        {
            startAgain = true;
        }

        public void StartFollowAnimation()
        {
            setFollowMode = true;
            PlayCurrent();
        }

        /// <summary>
        /// Plays the currently loaded animation from beginning. If no animation is loaded, the animation specific point of reference is reset and the replay is stopped. 
        /// </summary>
        public void Play()
        {
            if (animation == null)
            {
                animationSpecificPointOfReference.localPosition = Vector3.zero;
                animationSpecificPointOfReference.localRotation = Quaternion.identity;
                Stop();
                return;
            }

            Duration = animation.Duration;

            // the animation specific point of reference is moved according to the stored position and rotation in the animation entity
            animationSpecificPointOfReference.localPosition = new Vector3((float)animationWrapper.position_x, (float)animationWrapper.position_y, (float)animationWrapper.position_z);
            animationSpecificPointOfReference.localRotation = new Quaternion((float)animationWrapper.rotation_x, (float)animationWrapper.rotation_y, (float)animationWrapper.rotation_z, (float)animationWrapper.rotation_w);

            localTime = 0.0f;

            isPlaying = true;
        }

        /// <summary>
        /// Remove all components not needed for animation
        /// </summary>
        private void AdjustComponents(GameObject obj)
        {
            foreach (var comp in obj.GetComponents<Component>())
            {
                if (!essentialComponents.Contains(comp.GetType().Name))
                {
                    Destroy(comp);
                }
            }
            Destroy(obj.GetComponent<ConstraintManager>());
            obj.GetComponent<MeshRenderer>().material = animatingMaterial;
        }

        /// <summary>
        /// Create clones of every object used in the animation and add to list.
        /// Update the curves associated name
        /// </summary>
        public void InstantiateObjects()
        {
            // No recording exists at the moment
            if (animation == null) return;

            // Initialize a new object list
            objectList = new List<GameObject>();


            foreach(var entry in animation.objectCurves)
            {
                GameObject obj = null;
                int index = entry.Key.IndexOf("-");
                if (index < 0)
                {
                    Debug.Log("entry is " + entry.Key);
                    if (entry.Key == "userdefined(Clone)")
                    {
                        // Special case of user created object
                        obj = objectManager.GetUserDefinedObject();
                        if (obj == null)
                        {
                            Debug.Log("Cannot locate the user-defined object");
                        }
                    }
                    else
                    {
                        Debug.Log($"Cannot map {entry.Key} to original objects. Make sure to use the correct naming convention for objects.");
                    }
                }
                else
                {
                    string originalName = entry.Key.Substring(0, index).Trim();
                    obj = objectManager.GetOriginalObject(originalName);
                    if (obj == null)
                    {
                        Debug.Log($"GameObject with name {originalName} could not be found.");
                        continue;
                    }
                }

                if (obj != null)
                {
                    GameObject clone = Instantiate(obj, animationSpecificPointOfReference);
                    AdjustComponents(clone);
                    clone.SetActive(false);
                    clone.name = entry.Key;
                    objectList.Add(clone);
                }
                
            }
        }
        /// <summary>
        /// Plays the specified animation wrapper (assuming the animation data is available).
        /// If animationWrapper is null, this method will stop the playback. 
        /// </summary>
        /// <param name="animationWrapper">The animation wrapper.</param>
        public void Play(AnimationWrapper animationWrapper)
        {
            if (animationWrapper == null)
            {
                animationSpecificPointOfReference.localPosition = Vector3.zero;
                animationSpecificPointOfReference.localRotation = Quaternion.identity;
                Stop();
                return;
            }

            this.animationWrapper = animationWrapper;
            animation = animationWrapper.Animation;
            // Get all objects from the animation and make them available
            objectManager.DeactivateRealObjects();
            InstantiateObjects();

            Play();
        }

        /// <summary>
        /// Plays the current animation from the animation list instance. If there is no currently loaded animation entity, this will call Play(null)
        /// </summary>
        public void PlayCurrent()
        {
            Play(FileHandler.AnimationListInstance.GetCurrentAnimationWrapper());
        }


        /// <summary>
        /// Stops the playback
        /// </summary>
        public void Stop()
        {
            if (!isPlaying)
            {
                return;
            }
            objectManager.ActivateRealObjects();
            localTime = 0.0f;
            isPlaying = false;
            Evaluate();
        }

        public void Update()
        {
            if (setFollowMode)
            {
                if (isPlaying)
                {
                    // local time is updated according to the fps delta time, taking the speed into accound. 
                    // localTime += Time.deltaTime * 0.2f * (float)(animationWrapper.Speed);


                    // if the local time is before the start frame, the local time is set to the time of the first frame (i.e. to the selected beginning of the animation)
                    if (localTime < Duration * (float)animationWrapper.StartFrame)
                    {
                        localTime = Duration * (float)animationWrapper.StartFrame;
                    }

                    // if local time is before the last frame, the current hand pose is evaluated at localTime and the status bars of both hands are updated
                    if (localTime < Duration * (float)animationWrapper.EndFrame && !startAgain)
                    {
                        Evaluate();
                    }
                    // when local time reaches the end of the animation, it is reset to the time of the first frame. 
                    // in study mode, pausing is activated after each iteration, s.t. each animation is only played once. 
                    else
                    {
                        localTime = Duration * (float)animationWrapper.StartFrame;
                        startAgain = false;
                    }
                }
            } else
            {
                if (isPlaying)
                {
                    // local time is updated according to the fps delta time, taking the speed into accound. 
                    localTime += Time.deltaTime * (float)(animationWrapper.Speed);


                    // if the local time is before the start frame, the local time is set to the time of the first frame (i.e. to the selected beginning of the animation)
                    if (localTime < Duration * (float)animationWrapper.StartFrame)
                    {
                        localTime = Duration * (float)animationWrapper.StartFrame;
                    }

                    // if local time is before the last frame, the current hand pose is evaluated at localTime and the status bars of both hands are updated
                    if (localTime < Duration * (float)animationWrapper.EndFrame && !startAgain)
                    {
                        Evaluate();
                    }
                    // when local time reaches the end of the animation, it is reset to the time of the first frame. 
                    // in study mode, pausing is activated after each iteration, s.t. each animation is only played once. 
                    else
                    {
                        localTime = Duration * (float)animationWrapper.StartFrame;
                        startAgain = false;
                    }
                }
            }
        }

        /// Evaluate the animation at localTime
        private void Evaluate()
        {
            // if no replay is currently happening, both hands and all objects are set invisible
            if (!isPlaying)
            {
                riggedHandLeft.gameObject.SetActive(false);
                riggedHandRight.gameObject.SetActive(false);
                foreach(var obj in objectList)
                {
                    obj.SetActive(false);
                }
                return;
            }
            // otherwise, only the hands that are supposed to be visible are set to visible state. 
            else
            {
                if (!animationWrapper.LeftHand)
                {
                    riggedHandLeft.gameObject.SetActive(false);
                }
                else riggedHandLeft.gameObject.SetActive(true);
                if (!animationWrapper.RightHand)
                {
                    riggedHandRight.gameObject.SetActive(false);
                }
                else riggedHandRight.gameObject.SetActive(true);
                foreach(var obj in objectList)
                {
                    obj.SetActive(true);
                }
            }

            if (animation == null)
            {
                localTime = 0.0f;
                isPlaying = false;

                return;
            }

            // evaluate hand poses
            if (animation.HasHandData)
            {
                if (animationWrapper.LeftHand) EvaluateHandData(Handedness.Left);
                if (animationWrapper.RightHand) EvaluateHandData(Handedness.Right);
            }

            EvaluateObjectData();
        }

        /// <summary>
        /// Update object positions and rotations
        /// </summary>
        private void EvaluateObjectData()
        {
            foreach(var obj in objectList)
            {
                TransformData data = animation.EvaluateObject(localTime, obj.name);
                obj.transform.localPosition =data.GetPosition();
                obj.transform.localRotation = data.GetRotation();
                obj.transform.localScale = data.GetScale();
            } 
        }

        /// <summary>
        /// Returns the state of the player
        /// </summary>
        /// <returns>bool</returns>
        public bool IsPlaying()
        {
            return isPlaying;
        }


        /// <summary>
        /// updates the joint positions and rotations in the rigged hand visualizer
        /// </summary>
        /// <param name="handedness">The handedness.</param>
        private void EvaluateHandData(Handedness handedness)
        {
            animation.EvaluateHandState(localTime, handedness, out bool isTracked, out bool isPinching);

            if (handedness == Handedness.Left)
            {
                riggedHandLeft.gameObject.SetActive(isTracked);
            }
            else
            {
                riggedHandRight.gameObject.SetActive(isTracked);
            }

            IDictionary<TrackedHandJoint, TransformData> joints = new Dictionary<TrackedHandJoint, TransformData>();


            for (int i = 0; i < jointCount; ++i)
            {
                joints.Add(new KeyValuePair<TrackedHandJoint, TransformData>(
                    (TrackedHandJoint)i,
                    animation.EvaluateHandJoint(localTime, handedness, (TrackedHandJoint)i)));
            }

            if (handedness == Handedness.Left)
            {
                riggedHandVisualizerLeft.UpdateHandJoints(joints, isTracked);
            }
            if (handedness == Handedness.Right)
            {
                riggedHandVisualizerRight.UpdateHandJoints(joints, isTracked);
            }

        }

        public IDictionary<TrackedHandJoint, TransformData> GetAnimationByTime(float time)
        {
            IDictionary<TrackedHandJoint, TransformData> joints = new Dictionary<TrackedHandJoint, TransformData>();


            for (int i = 0; i < jointCount; ++i)
            {
                joints.Add(new KeyValuePair<TrackedHandJoint, TransformData>(
                    (TrackedHandJoint)i,
                    animation.EvaluateHandJoint(time, Handedness.Right, (TrackedHandJoint)i)));
            }
            return joints;
        }

        public void setLocalTime(float time)
        {
            localTime = time;
        }
    }

}