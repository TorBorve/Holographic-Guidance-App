using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Tutorials
{

    /// <summary>
    /// A used-defined marker on the input animation timeline.
    /// </summary>
    [Serializable]
    public class InputAnimationMarker : ICloneable
    {
        /// <summary>
        /// Placement of the marker relative to the input animation start time.
        /// </summary>
        public float time = 0.0f;

        /// <summary>
        /// Custom name of the marker.
        /// </summary>
        public string name = "";

        public object Clone()
        {
            var copy = new InputAnimationMarker();
            copy.time = time;
            copy.name = (string)name.Clone();
            return copy;
        }
    }

    /// <summary>
    /// Contains a set of animation curves that describe motion of camera and hands.
    /// </summary>
    [System.Serializable]
    public class InputAnimation : ICloneable
    {
        protected static readonly int jointCount = Enum.GetNames(typeof(TrackedHandJoint)).Length;

        /// <summary>
        /// Arbitrarily large weight for representing a boolean value in float curves.
        /// </summary>
        private const float BoolOutWeight = 1.0e6f;

        /// <summary>
        /// Maximum duration of all animations curves.
        /// </summary>
        [SerializeField]
        private float duration = 0.0f;

        /// <summary>
        /// Name of the animation visible to the user
        /// </summary>
        public string description = "Unnamed scene";

        /// <summary>
        /// Maximum duration of all animations curves.
        /// </summary>
        public float Duration
        {
            get
            {
                if (duration > 0.0f) return duration;

                ComputeDuration();

                return duration;
            }
        }

        /// <summary>
        /// Name of the animation visible to the user
        /// </summary>
        /*public string Description { get
            {
                return description;
            }
            protected internal set
            {
                this.description = value;
            }
        }*/

        /// <summary>
        /// Class that contains all animation curves for one joint / object (position and rotation)
        /// </summary>
        public class PoseCurves : ICloneable
        {
            public static int CURVE_COUNT = 17;

            public AnimationCurve PositionX = new AnimationCurve();
            public AnimationCurve PositionY = new AnimationCurve();
            public AnimationCurve PositionZ = new AnimationCurve();
            public AnimationCurve RotationX = new AnimationCurve();
            public AnimationCurve RotationY = new AnimationCurve();
            public AnimationCurve RotationZ = new AnimationCurve();
            public AnimationCurve RotationW = new AnimationCurve();
            public AnimationCurve ScaleX = new AnimationCurve();
            public AnimationCurve ScaleY = new AnimationCurve();
            public AnimationCurve ScaleZ = new AnimationCurve();

            public AnimationCurve GlobalPositionX = new AnimationCurve();
            public AnimationCurve GlobalPositionY = new AnimationCurve();
            public AnimationCurve GlobalPositionZ = new AnimationCurve();
            public AnimationCurve GlobalRotationX = new AnimationCurve();
            public AnimationCurve GlobalRotationY = new AnimationCurve();
            public AnimationCurve GlobalRotationZ = new AnimationCurve();
            public AnimationCurve GlobalRotationW = new AnimationCurve();
            //public AnimationCurve LocalScaleX = new AnimationCurve();
            //public AnimationCurve LocalScaleY = new AnimationCurve();
            //public AnimationCurve LocalScaleZ = new AnimationCurve();

            public void AddKey(float time, MixedRealityPose pose)
            {
                AddFloatKey(PositionX, time, pose.Position.x);
                AddFloatKey(PositionY, time, pose.Position.y);
                AddFloatKey(PositionZ, time, pose.Position.z);

                AddFloatKey(RotationX, time, pose.Rotation.x);
                AddFloatKey(RotationY, time, pose.Rotation.y);
                AddFloatKey(RotationZ, time, pose.Rotation.z);
                AddFloatKey(RotationW, time, pose.Rotation.w);
            }

            public void AddKey(float time, TransformData transformData)
            {
                AddFloatKey(PositionX, time, transformData.posx);
                AddFloatKey(PositionY, time, transformData.posy);
                AddFloatKey(PositionZ, time, transformData.posz);

                AddFloatKey(RotationX, time, transformData.rotx);
                AddFloatKey(RotationY, time, transformData.roty);
                AddFloatKey(RotationZ, time, transformData.rotz);
                AddFloatKey(RotationW, time, transformData.rotw);

                AddFloatKey(ScaleX, time, transformData.scalex);
                AddFloatKey(ScaleY, time, transformData.scaley);
                AddFloatKey(ScaleZ, time, transformData.scalez);

                AddFloatKey(GlobalPositionX, time, transformData.globPosx);
                AddFloatKey(GlobalPositionY, time, transformData.globPosy);
                AddFloatKey(GlobalPositionZ, time, transformData.globPosz);

                AddFloatKey(GlobalRotationX, time, transformData.globRotx);
                AddFloatKey(GlobalRotationY, time, transformData.globRoty);
                AddFloatKey(GlobalRotationZ, time, transformData.globRotz);
                AddFloatKey(GlobalRotationW, time, transformData.globRotw);

                //AddFloatKey(LocalScaleX, time, transformData.locscalex);
                //AddFloatKey(LocalScaleY, time, transformData.locscaley);
                //AddFloatKey(LocalScaleZ, time, transformData.locscalez);
            }

            /// <summary>
            /// Optimizes the set of curves.
            /// </summary>
            /// <param name="positionThreshold">The maximum permitted error between the positions of the old and new curves, in units.</param>
            /// <param name="rotationThreshold">The maximum permitted error between the rotations of the old and new curves, in degrees.</param>
            /// <param name="partitionSize">The size of the partitions of the curves that will be optimized independently. Larger values will optimize the curves better, but may take longer.</param>
            public void Optimize(float positionThreshold, float rotationThreshold, int partitionSize)
            {
                OptimizePositionCurve(ref PositionX, ref PositionY, ref PositionZ, positionThreshold, partitionSize);
                OptimizeRotationCurve(ref RotationX, ref RotationY, ref RotationZ, ref RotationW, rotationThreshold, partitionSize);
            }

            public TransformData Evaluate(float time)
            {
                var transformData = new TransformData(PositionX.Evaluate(time),
                                                PositionY.Evaluate(time),
                                                PositionZ.Evaluate(time),
                                                RotationX.Evaluate(time),
                                                RotationY.Evaluate(time),
                                                RotationZ.Evaluate(time),
                                                RotationW.Evaluate(time),
                                                ScaleX.Evaluate(time),
                                                ScaleY.Evaluate(time),
                                                ScaleZ.Evaluate(time),
                                                GlobalPositionX.Evaluate(time),
                                                GlobalPositionY.Evaluate(time),
                                                GlobalPositionZ.Evaluate(time),
                                                GlobalRotationX.Evaluate(time),
                                                GlobalRotationY.Evaluate(time),
                                                GlobalRotationZ.Evaluate(time),
                                                GlobalRotationW.Evaluate(time));
                                                //LocalScaleX.Evaluate(time),
                                                //LocalScaleY.Evaluate(time),
                                                //LocalScaleZ.Evaluate(time));
                return transformData;
            }

            public List<AnimationCurve> GetAnimationCurves()
            {
                List<AnimationCurve> result = new List<AnimationCurve>();
                result.Add(PositionX);
                result.Add(PositionY);
                result.Add(PositionZ);
                result.Add(RotationX);
                result.Add(RotationY);
                result.Add(RotationZ);
                result.Add(RotationW);
                result.Add(ScaleX);
                result.Add(ScaleY);
                result.Add(ScaleZ);

                result.Add(GlobalPositionX);
                result.Add(GlobalPositionY);
                result.Add(GlobalPositionZ);
                result.Add(GlobalRotationX);
                result.Add(GlobalRotationY);
                result.Add(GlobalRotationZ);
                result.Add(GlobalRotationW);
                //result.Add(LocalScaleX);
                //result.Add(LocalScaleY);
                //result.Add(LocalScaleZ);
                return result;
            }

            public static PoseCurves FromAnimationCurves(List<AnimationCurve> curves)
            {
                if (curves.Count != CURVE_COUNT)
                {
                    return null;
                }
                PoseCurves result = new PoseCurves();
                result.PositionX = curves[0];
                result.PositionY = curves[1];
                result.PositionZ = curves[2];
                result.RotationX = curves[3];
                result.RotationY = curves[4];
                result.RotationZ = curves[5];
                result.RotationW = curves[6];
                result.ScaleX = curves[7];
                result.ScaleY = curves[8];
                result.ScaleZ = curves[9];

                result.GlobalPositionX = curves[10];
                result.GlobalPositionY = curves[11];
                result.GlobalPositionZ = curves[12];
                result.GlobalRotationX = curves[13];
                result.GlobalRotationY = curves[14];
                result.GlobalRotationZ = curves[15];
                result.GlobalRotationW = curves[16];
                //result.LocalScaleX = curves[17];
                //result.LocalScaleY = curves[18];
                //result.LocalScaleZ = curves[19];
                return result;
            }

            public void Prune(float startTime = float.MinValue, float endTime = float.MaxValue)
            {
                foreach (var curve in GetAnimationCurves())
                {
                    while (curve.length > 0 && curve.keys[0].time < startTime)
                    {
                        curve.RemoveKey(0);
                    }
                    int idx = curve.length - 1;
                    while (idx >= 0 && curve.keys[idx].time > endTime)
                    {
                        curve.RemoveKey(idx--);
                    }
                }
            }

            public object Clone()
            {
                var curves = GetAnimationCurves();
                for (int i = 0; i < curves.Count; ++i)
                {
                    curves[i] = InputAnimation.Clone(curves[i]);
                }
                return FromAnimationCurves(curves);
            }
        }

        private class RayCurves
        {
            public AnimationCurve OriginX = new AnimationCurve();
            public AnimationCurve OriginY = new AnimationCurve();
            public AnimationCurve OriginZ = new AnimationCurve();
            public AnimationCurve DirectionX = new AnimationCurve();
            public AnimationCurve DirectionY = new AnimationCurve();
            public AnimationCurve DirectionZ = new AnimationCurve();

            public void AddKey(float time, Ray ray)
            {
                AddVectorKey(OriginX, OriginY, OriginZ, time, ray.origin);
                AddVectorKey(DirectionX, DirectionY, DirectionZ, time, ray.direction);
            }

            /// <summary>
            /// Optimizes the set of curves.
            /// </summary>
            /// <param name="originThreshold">The maximum permitted error between the origins of the old and new curves, in units.</param>
            /// <param name="directionThreshold">The maximum permitted error between the directions of the old and new curves, in degrees.</param>
            /// <param name="partitionSize">The size of the partitions of the curves that will be optimized independently. Larger values will optimize the curves better, but may take longer.</param>
            public void Optimize(float originThreshold, float directionThreshold, int partitionSize)
            {
                OptimizePositionCurve(ref OriginX, ref OriginY, ref OriginZ, originThreshold, partitionSize);
                OptimizeDirectionCurve(ref DirectionX, ref DirectionY, ref DirectionZ, directionThreshold, partitionSize);
            }

            public Ray Evaluate(float time)
            {
                float ox = OriginX.Evaluate(time);
                float oy = OriginY.Evaluate(time);
                float oz = OriginZ.Evaluate(time);
                float dx = DirectionX.Evaluate(time);
                float dy = DirectionY.Evaluate(time);
                float dz = DirectionZ.Evaluate(time);

                var ray = new Ray();

                ray.origin = new Vector3(ox, oy, oz);
                ray.direction = new Vector3(dx, dy, dz);
                ray.direction.Normalize();

                return ray;
            }
        }

        internal class CompareMarkers : IComparer<InputAnimationMarker>
        {
            public int Compare(InputAnimationMarker a, InputAnimationMarker b)
            {
                return a.time.CompareTo(b.time);
            }
        }

        [SerializeField]
        public AnimationCurve handTrackedCurveLeft;
        [SerializeField]
        public AnimationCurve handTrackedCurveRight;
        [SerializeField]
        private AnimationCurve handPinchCurveLeft;
        [SerializeField]
        private AnimationCurve handPinchCurveRight;
        //[SerializeField]
        //private AnimationCurve handGripCurveLeft;
        //[SerializeField]
        //private AnimationCurve handGripCurveRight;
        [SerializeField]
        public Dictionary<TrackedHandJoint, PoseCurves> handJointCurvesLeft;
        [SerializeField]
        public Dictionary<TrackedHandJoint, PoseCurves> handJointCurvesRight;
        [SerializeField]
        private PoseCurves cameraCurves;
        [SerializeField]
        private PoseCurves gazeCurves;
        [SerializeField]
        public Dictionary<string, PoseCurves> objectCurves;

        //public MixedRealityPose qrCoordinate;

        /// <summary>
        /// Whether the animation has hand state and joint curves
        /// </summary>
        public bool HasHandData { get; private set; } = false;
        /// <summary>
        /// Whether the animation has camera pose curves
        /// </summary>
        public bool HasCameraPose { get; private set; } = false;
        /// <summary>
        /// Whether the animation has eye gaze curves
        /// </summary>
        public bool HasEyeGaze { get; private set; } = false;

        /// <summary>
        /// Number of markers in the animation.
        /// </summary>
        [SerializeField]
        private List<InputAnimationMarker> markers;
        /// <summary>
        /// Number of markers in the animation.
        /// </summary>
        public int markerCount => markers.Count;

        /// <summary>
        /// Default constructor
        /// </summary>
        public InputAnimation()
        {
            handTrackedCurveLeft = new AnimationCurve();
            handTrackedCurveRight = new AnimationCurve();
            handPinchCurveLeft = new AnimationCurve();
            handPinchCurveRight = new AnimationCurve();
            //handGripCurveLeft = new AnimationCurve();
            //handGripCurveRight = new AnimationCurve();
            handJointCurvesLeft = new Dictionary<TrackedHandJoint, PoseCurves>();
            handJointCurvesRight = new Dictionary<TrackedHandJoint, PoseCurves>();
            cameraCurves = new PoseCurves();
            gazeCurves = new PoseCurves();
            objectCurves = new Dictionary<string, PoseCurves>();
            markers = new List<InputAnimationMarker>();
            //qrCoordinate = new MixedRealityPose();
            //referenceCoordinate = new Transform;
        }

        public object Clone()
        {
            var copy = new InputAnimation();

            copy.handTrackedCurveLeft = Clone(handTrackedCurveLeft);
            copy.handTrackedCurveRight = Clone(handTrackedCurveRight);
            copy.handPinchCurveLeft = Clone(handPinchCurveLeft);
            copy.handPinchCurveRight = Clone(handPinchCurveRight);
            //copy.handGripCurveLeft = Clone(handGripCurveLeft);
            //copy.handGripCurveRight = Clone(handGripCurveRight);
            foreach (var item in handJointCurvesLeft)
            {
                copy.handJointCurvesLeft.Add(item.Key, (PoseCurves)item.Value.Clone());
            }
            foreach (var item in handJointCurvesRight)
            {
                copy.handJointCurvesRight.Add(item.Key, (PoseCurves)item.Value.Clone());
            }
            copy.cameraCurves = (PoseCurves)cameraCurves.Clone();
            copy.gazeCurves = (PoseCurves)gazeCurves.Clone();
            foreach (var item in objectCurves)
            {
                copy.objectCurves.Add(item.Key, (PoseCurves)item.Value.Clone());
            }
            foreach (var item in markers)
            {
                copy.markers.Add((InputAnimationMarker)item.Clone());
            }
            //copy.qrCoordinate = new MixedRealityPose(qrCoordinate.Position, qrCoordinate.Rotation);

            return copy;
        }

        /// <summary>
        /// Add a keyframe for the tracking state of a hand.
        /// </summary>
        [Obsolete("Use FromRecordingBuffer to construct new InputAnimations")]
        public void AddHandStateKey(float time, Handedness handedness, bool isTracked, bool isPinching)
        {
            if (handedness == Handedness.Left)
            {
                AddHandStateKey(time, isTracked, isPinching, handTrackedCurveLeft, handPinchCurveLeft);
            }
            else if (handedness == Handedness.Right)
            {
                AddHandStateKey(time, isTracked, isPinching, handTrackedCurveRight, handPinchCurveRight);
            }
        }

        /// <summary>
        /// Serialize animation data into a stream.
        /// </summary>
        public void ToStream(StreamWriter writer, TransformData aspor = null)
        {
            var defaultCurves = new PoseCurves();
            if (aspor == null) {
                aspor = TransformData.ZeroIdentity();
            }
            InputAnimationSerializationUtils.WriteHeader(writer);
            writer.WriteLine(HasCameraPose);
            writer.WriteLine(HasHandData);
            writer.WriteLine(HasEyeGaze);

            writer.WriteLine("REFERENCE_COORDINATE_SYSTEM");
            writer.WriteLine(aspor.posx + ", " + aspor.posy + ", " + aspor.posz + ", "
                + aspor.rotx + ", " + aspor.roty + ", " + aspor.rotz + ", " + aspor.rotw
                + ", " + aspor.scalex + ", " + aspor.scaley + ", " + aspor.scalez
                +", " + aspor.globPosx + ", " + aspor.globPosy + ", " + aspor.globPosz
                + ", " + aspor.globRotx + ", " + aspor.globRoty + ", " + aspor.globRotz + ", " + aspor.globRotw);
                //+ ", " + aspor.locscalex + ", " + aspor.locscaley + ", " + aspor.locscalez);

            writer.WriteLine("HEAD_POSES");
            CurvesToStream(writer, cameraCurves.GetAnimationCurves());

            writer.WriteLine("LEFT_HAND_POSES");
            List<AnimationCurve> leftHandCurves = new List<AnimationCurve>
                {
                    //handGripCurveLeft,
                    handPinchCurveLeft,
                    handTrackedCurveLeft
                };
            // TrackedHandJoint 0 is None, so we can start with Index 1
            for (int i = 1; i < jointCount; ++i)
            {
                if (!handJointCurvesLeft.TryGetValue((TrackedHandJoint)i, out var curves))
                {
                    curves = defaultCurves;
                }
                leftHandCurves.AddRange(curves.GetAnimationCurves());
            }
            CurvesToStream(writer, leftHandCurves);

            writer.WriteLine("RIGHT_HAND_POSES");
            List<AnimationCurve> rightHandCurves = new List<AnimationCurve>
                {
                    //handGripCurveRight,
                    handPinchCurveRight,
                    handTrackedCurveRight
                };
            // TrackedHandJoint 0 is None, so we can start with Index 1
            for (int i = 1; i < jointCount; ++i)
            {
                if (!handJointCurvesRight.TryGetValue((TrackedHandJoint)i, out var curves))
                {
                    curves = defaultCurves;
                }
                rightHandCurves.AddRange(curves.GetAnimationCurves());
            }
            CurvesToStream(writer, rightHandCurves);

            writer.WriteLine("EYE_GAZE");
            CurvesToStream(writer, gazeCurves.GetAnimationCurves());

            ObjectCurvesToStream(writer, objectCurves);

            InputAnimationSerializationUtils.WriteMarkerList(writer, markers);
        }

        /// <summary>
        /// Serialize animation data into a stream asynchronously.
        /// </summary>
        public async Task ToStreamAsync(StreamWriter stream, TransformData aspor = null, Action callback = null)
        {
            await Task.Run(() => ToStream(stream, aspor));

            callback?.Invoke();
        }

        /// <summary>
        /// Evaluate hand tracking state at the given time.
        /// </summary>
        public void EvaluateHandState(float time, Handedness handedness, out bool isTracked, out bool isPinching)
        {
            if (!HasHandData)
            {
                isTracked = false;
                isPinching = false;
            }

            if (handedness == Handedness.Left)
            {
                EvaluateHandState(time, handTrackedCurveLeft, handPinchCurveLeft, out isTracked, out isPinching);
            }
            else if (handedness == Handedness.Right)
            {
                EvaluateHandState(time, handTrackedCurveRight, handPinchCurveRight, out isTracked, out isPinching);
            }
            else
            {
                isTracked = false;
                isPinching = false;
            }
        }

        /// <summary>
        /// Find an index i in the sorted events list, such that events[i].time &lt;= time &lt; events[i+1].time.
        /// </summary>
        /// <returns>
        /// 0 &lt;= i &lt; eventCount if a full interval could be found.
        /// -1 if time is less than the first event time.
        /// eventCount-1 if time is greater than the last event time.
        /// </returns>
        /// <remarks>
        /// Uses binary search.
        /// </remarks>
        public int FindMarkerInterval(float time)
        {
            int lowIdx = -1;
            int highIdx = markers.Count;
            while (lowIdx < highIdx - 1)
            {
                int midIdx = (lowIdx + highIdx) >> 1;
                if (time >= markers[midIdx].time)
                {
                    lowIdx = midIdx;
                }
                else
                {
                    highIdx = midIdx;
                }
            }
            return lowIdx;
        }

        /*public double[] EvaluateGlobalJoint(out int stepCount, int numSteps = 1000)
        {

            int keyframeCount = handTrackedCurveLeft.length;
            int count = 0;
            int[] validJoints = new int[] { 1, 3, 4, 5, 6, 8, 9, 10, 11, 13, 14, 15, 16, 18, 19, 20, 21, 23, 24, 25, 26 };
            double[] handJoints = new double[numSteps * validJoints.Length * 2 * 3];

            int startTimeIdx;
            if (keyframeCount <= numSteps)
            {
                startTimeIdx = 0;
                stepCount = keyframeCount;
            }
            else
            {
                startTimeIdx = keyframeCount - numSteps;
                stepCount = numSteps;
            }
            for (var stepIdx = 0; stepIdx < stepCount; stepIdx++)
            {
                var time = handTrackedCurveLeft.keys[startTimeIdx + stepIdx].time;
                foreach (var joint in validJoints)
                {
                    var leftData = EvaluateHandJoint(time, Handedness.Left, (TrackedHandJoint)joint);

                    handJoints[count] = (double)leftData.posx;
                    count++;
                    handJoints[count] = (double)leftData.posy;
                    count++;
                    handJoints[count] = (double)leftData.posz;
                    count++;
                }
                foreach (var joint in validJoints)
                {
                    var rightData = EvaluateHandJoint(time, Handedness.Right, (TrackedHandJoint)joint);

                    handJoints[count] = (double)rightData.posx;
                    count++;
                    handJoints[count] = (double)rightData.posy;
                    count++;
                    handJoints[count] = (double)rightData.posz;
                    count++;
                }
            }
            //handTrackedCurveLeft.keys[1].;
            //Debug.Log($"data: {rightData.posx}");

            return handJoints;

        }


        public double[] EvaluateQRJoint(out int stepCount, int numSteps = 1000)
        {

            int keyframeCount = handTrackedCurveLeft.length;
            int count = 0;
            int[] validJoints = new int[] { 1, 3, 4, 5, 6, 8, 9, 10, 11, 13, 14, 15, 16, 18, 19, 20, 21, 23, 24, 25, 26 };
            double[] handJoints = new double[numSteps * validJoints.Length * 2 * 3];

            int startTimeIdx;
            if (keyframeCount <= numSteps)
            {
                startTimeIdx = 0;
                stepCount = keyframeCount;
            }
            else
            {
                startTimeIdx = keyframeCount - numSteps;
                stepCount = numSteps;
            }



            for (var stepIdx = 0; stepIdx < stepCount; stepIdx++)
            {
                var time = handTrackedCurveLeft.keys[startTimeIdx + stepIdx].time;
                foreach (var joint in validJoints)
                {
                    var leftData = EvaluateHandJoint(time, Handedness.Left, (TrackedHandJoint)joint);
                    Vector3 leftLoc = leftData.GetGlobalPosition();
                    leftLoc = Quaternion.Inverse(qrCoordinate.Rotation) * (leftLoc - qrCoordinate.Position);
                    handJoints[count] = (double)leftLoc[0];// - qrCoordinate.Position[0];
                    count++;
                    handJoints[count] = (double)leftLoc[1];
                    count++;
                    handJoints[count] = (double)leftLoc[2];
                    count++;
                }
                foreach (var joint in validJoints)
                {
                    var rightData = EvaluateHandJoint(time, Handedness.Right, (TrackedHandJoint)joint);
                    Vector3 rightLoc = rightData.GetGlobalPosition();
                    rightLoc = Quaternion.Inverse(qrCoordinate.Rotation) * (rightLoc - qrCoordinate.Position);
                    handJoints[count] = (double)rightLoc[0];
                    count++;
                    handJoints[count] = (double)rightLoc[1];
                    count++;
                    handJoints[count] = (double)rightLoc[2];
                    count++;
                }
            }
            //handTrackedCurveLeft.keys[1].;
            //Debug.Log($"data: {rightData.posx}");

            return handJoints;

        }


        public float[] EvaluateQRJointFloat(out int stepCount, int numSteps = 1000)
        {

            int keyframeCount = handTrackedCurveLeft.length;
            int count = 0;
            int[] validJoints = new int[] { 1, 3, 4, 5, 6, 8, 9, 10, 11, 13, 14, 15, 16, 18, 19, 20, 21, 23, 24, 25, 26 };
            float[] handJoints = new float[numSteps * validJoints.Length * 2 * 3];

            int startTimeIdx;
            if (keyframeCount < numSteps)
            {
                startTimeIdx = 0;
                stepCount = keyframeCount;
            }
            else
            {
                startTimeIdx = keyframeCount - numSteps;
                stepCount = numSteps - 1;
            }



            for (var stepIdx = 0; stepIdx < stepCount; stepIdx++)
            {
                var time = handTrackedCurveLeft.keys[startTimeIdx + stepIdx].time;
                foreach (var joint in validJoints)
                {
                    var leftData = EvaluateHandJoint(time, Handedness.Left, (TrackedHandJoint)joint);
                    Vector3 leftLoc = leftData.GetGlobalPosition();

                    leftLoc = Quaternion.Inverse(qrCoordinate.Rotation) * (leftLoc - qrCoordinate.Position);
                    handJoints[count] = leftLoc[0];// - qrCoordinate.Position[0];
                    count++;
                    handJoints[count] = leftLoc[1];
                    count++;
                    handJoints[count] = leftLoc[2];
                    count++;
                }
                foreach (var joint in validJoints)
                {
                    var rightData = EvaluateHandJoint(time, Handedness.Right, (TrackedHandJoint)joint);
                    Vector3 rightLoc = rightData.GetGlobalPosition();
                    rightLoc = Quaternion.Inverse(qrCoordinate.Rotation) * (rightLoc - qrCoordinate.Position);
                    handJoints[count] = rightLoc[0];
                    count++;
                    handJoints[count] = rightLoc[1];
                    count++;
                    handJoints[count] = rightLoc[2];
                    count++;
                }
            }
            //handTrackedCurveLeft.keys[1].;
            //Debug.Log($"data: {rightData.posx}");

            return handJoints;

        }
        */
        /// <summary>
        /// Evaluate joint pose at the given time.
        /// </summary>
        public TransformData EvaluateHandJoint(float time, Handedness handedness, TrackedHandJoint joint)
        {
            if (!HasHandData)
            {
                return TransformData.ZeroIdentity();
            }

            if (handedness == Handedness.Left)
            {
                return EvaluateHandJoint(time, joint, handJointCurvesLeft);
            }
            else if (handedness == Handedness.Right)
            {
                return EvaluateHandJoint(time, joint, handJointCurvesRight);
            }
            else
            {
                return TransformData.ZeroIdentity();
            }
        }

        /// <summary>
        /// Evaluate the eye gaze pose at the given time.
        /// </summary>
        public TransformData EvaluateEyeGaze(float time)
        {
            if (!HasEyeGaze)
            {
                return TransformData.ZeroIdentity();
            }

            return gazeCurves.Evaluate(time);
        }

        public TransformData EvaluateCameraPose(float time)
        {
            if (!HasCameraPose)
            {
                return TransformData.ZeroIdentity();
            }

            return cameraCurves.Evaluate(time);
        }

        /// <summary>
        /// Generates an input animation from the contents of a recording buffer.
        /// </summary>
        /// <param name="recordingBuffer">The buffer to convert to an animation</param>
        public static InputAnimation FromRecordingBuffer(InputRecordingBuffer recordingBuffer)
        {
            var animation = new InputAnimation();
            if (recordingBuffer.Empty())
                return animation;
            float startTime = recordingBuffer.StartTime;

            foreach (var keyframe in recordingBuffer)
            {
                float localTime = keyframe.Time - startTime;

                animation.HasHandData |= keyframe.LeftTracked | keyframe.RightTracked;
                AddBoolKey(animation.handTrackedCurveLeft, localTime, keyframe.LeftTracked);
                AddBoolKey(animation.handTrackedCurveRight, localTime, keyframe.RightTracked);
                AddBoolKey(animation.handPinchCurveLeft, localTime, keyframe.LeftPinch);
                AddBoolKey(animation.handPinchCurveRight, localTime, keyframe.RightPinch);
                //AddBoolKey(animation.handGripCurveLeft, localTime, keyframe.LeftGrip);
                //AddBoolKey(animation.handGripCurveRight, localTime, keyframe.RightGrip);

                /*if (keyframe.HasCameraPose)
                {
                    animation.HasCameraPose = true;
                    animation.cameraCurves.AddKey(localTime, keyframe.CameraPose);
                }
                if (keyframe.HasGazePose)
                {
                    animation.HasEyeGaze = true;
                    animation.gazeCurves.AddKey(localTime, keyframe.GazePose);
                }  */

                foreach (var joint in (TrackedHandJoint[])Enum.GetValues(typeof(TrackedHandJoint)))
                {
                    AddJointPoseKeys(animation.handJointCurvesLeft, keyframe.LeftJointsTransformData, joint, localTime);
                    AddJointPoseKeys(animation.handJointCurvesRight, keyframe.RightJointsTransformData, joint, localTime);
                }
                foreach(var objectData in keyframe.ObjectsTransformData)
                {
                    AddObjectPoseKeys(animation.objectCurves, objectData.Value, objectData.Key, localTime);
                }
            }

            animation.ComputeDuration();

            return animation;

            void AddBoolKeyIfChanged(AnimationCurve curve, float time, bool value)
            {
                if (curve.length > 0 && (curve[curve.length - 1].value > 0.5f) == value)
                {
                    return;
                }

                AddBoolKey(curve, time, value);
            }
        }

        /// <summary>
        /// Deserializes animation data from a stream.
        /// </summary>
        public static InputAnimation FromStream(Stream stream)
        {
            var animation = new InputAnimation();
            var reader = new StreamReader(stream);

            InputAnimationSerializationUtils.ReadHeader(reader);
            animation.HasCameraPose = bool.Parse(reader.ReadLine());
            animation.HasHandData = bool.Parse(reader.ReadLine());
            animation.HasEyeGaze = bool.Parse(reader.ReadLine());

            var header = reader.ReadLine();
            if (header != "REFERENCE_COORDINATE_SYSTEM")
            {
                Debug.LogError("Excepted REFERENCE_COORDINATE_SYSTEM header, got: " + header);
            }
            else
            {
                // Why the next two lines here?
                //var curves = CurvesFromStream(reader, PoseCurves.CURVE_COUNT);
                //animation.cameraCurves = PoseCurves.FromAnimationCurves(curves);
                float[] val = new float[17];
                var poseString = reader.ReadLine().Split(',');
                for (int i = 0; i < 17; i++)
                {
                    val[i] = float.Parse(poseString[i]);
                }
                TransformData aspor = new TransformData(val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7], val[8], val[9], val[10], val[11], val[12], val[13], val[14], val[15], val[16]); //, val[17], val[18], val[19]);
                //Vector3 qrPosition = new Vector3(val[0], val[1], val[2]);
                //Quaternion qrRotation = new Quaternion(val[3], val[4], val[5], val[6]);

                //animation.qrCoordinate = new MixedRealityPose(qrPosition, qrRotation);
                // aspor is not used currently
            }
            header = reader.ReadLine();
            if (header != "HEAD_POSES")
            {
                Debug.LogError("Excepted HEAD_POSES header, got: " + header);
            } else
            {
                var curves = CurvesFromStream(reader, PoseCurves.CURVE_COUNT);
                animation.cameraCurves = PoseCurves.FromAnimationCurves(curves);
            }

            header = reader.ReadLine();
            if (header != "LEFT_HAND_POSES")
            {
                Debug.LogError("Excepted LEFT_HAND_POSES header, got: " + header);
            } else
            {
                var curves = CurvesFromStream(reader, 2 + PoseCurves.CURVE_COUNT * (jointCount - 1));
                //animation.handGripCurveLeft = curves[0];
                animation.handPinchCurveLeft = curves[0];
                animation.handTrackedCurveLeft = curves[1];

                // TrackedHandJoint 0 is None, so we can start with Index 1
                for (int i = 1; i < jointCount; ++i)
                {
                    List<AnimationCurve> jointCurves = new List<AnimationCurve>();
                    for (int j = 0; j < PoseCurves.CURVE_COUNT; ++j)
                    {
                        jointCurves.Add(curves[2 + PoseCurves.CURVE_COUNT * (i - 1) + j]);
                    }
                    PoseCurves poseCurve = PoseCurves.FromAnimationCurves(jointCurves);
                    animation.handJointCurvesLeft.Add((TrackedHandJoint)i, poseCurve);
                }
            }
            header = reader.ReadLine();
            if (header != "RIGHT_HAND_POSES")
            {
                Debug.LogError("Excepted RIGHT_HAND_POSES header, got: " + header);
            }
            else
            {
                var curves = CurvesFromStream(reader, 2 + PoseCurves.CURVE_COUNT * (jointCount - 1));
                //animation.handGripCurveRight = curves[0];
                animation.handPinchCurveRight = curves[0];
                animation.handTrackedCurveRight = curves[1];

                // TrackedHandJoint 0 is None, so we can start with Index 1
                for (int i = 1; i < jointCount; ++i)
                {
                    List<AnimationCurve> jointCurves = new List<AnimationCurve>();
                    for(int j = 0; j < PoseCurves.CURVE_COUNT; ++j)
                    {
                        jointCurves.Add(curves[2 + PoseCurves.CURVE_COUNT * (i - 1) + j]);
                    }
                    PoseCurves poseCurve = PoseCurves.FromAnimationCurves(jointCurves);
                    animation.handJointCurvesRight.Add((TrackedHandJoint)i, poseCurve);
                }
            }

            header = reader.ReadLine();
            if (header != "EYE_GAZE")
            {
                Debug.LogError("Excepted EYE_GAZE header, got: " + header);
            }
            else
            {
                var curves = CurvesFromStream(reader, PoseCurves.CURVE_COUNT);
                animation.gazeCurves = PoseCurves.FromAnimationCurves(curves);
            }

            ObjectCurvesFromStream(reader, animation.objectCurves);

            InputAnimationSerializationUtils.ReadMarkerList(reader, animation.markers);
            animation.ComputeDuration();

            return animation;
        }

        /// <summary>
        /// Deserialize animation data from a stream asynchronously.
        /// </summary>
        public static async Task<InputAnimation> FromStreamAsync(Stream stream, Action callback = null)
        {
            var result = await Task.Run(() => FromStream(stream));

            callback?.Invoke();

            return result;
        }

        /// <summary>
        /// Add a keyframe for the tracking state of a hand.
        /// </summary>
        [Obsolete("Use FromRecordingBuffer to construct new InputAnimation")]
        private void AddHandStateKey(float time, bool isTracked, bool isPinching, AnimationCurve trackedCurve, AnimationCurve pinchCurve)
        {
            AddBoolKeyFiltered(trackedCurve, time, isTracked);
            AddBoolKeyFiltered(pinchCurve, time, isPinching);

            duration = Mathf.Max(duration, time);
        }

        public void ComputeDuration()
        {
            duration = 0.0f;
            foreach (var curve in GetAllAnimationCurves())
            {
                float curveDuration = (curve.length > 0 ? curve.keys[curve.length - 1].time - curve.keys[0].time : 0.0f);
                duration = Mathf.Max(duration, curveDuration);
            }
        }

        public float getEarliestTimestamp()
        {
            float earliest = float.MaxValue;
            foreach (var curve in GetAllAnimationCurves())
            {
                if (curve.length > 0)
                    earliest = Mathf.Min(earliest, curve.keys[0].time);
            }
            return earliest;
        }

        public float GetLatestTimestamp()
        {
            float latest = 0.0f;
            foreach (var curve in GetAllAnimationCurves())
            {
                if(curve.length > 0)
                    latest = Mathf.Max(latest, curve.keys[curve.length - 1].time);
            }
            return latest;
        }

        /// <summary>
        /// Evaluate hand tracking state at the given time.
        /// </summary>
        private void EvaluateHandState(float time, AnimationCurve trackedCurve, AnimationCurve pinchCurve, out bool isTracked, out bool isPinching)
        {
            isTracked = (trackedCurve.Evaluate(time) > 0.5f);
            isPinching = (pinchCurve.Evaluate(time) > 0.5f);
        }

        /// <summary>
        /// Evaluate joint pose at the given time.
        /// </summary>
        private TransformData EvaluateHandJoint(float time, TrackedHandJoint joint, Dictionary<TrackedHandJoint, PoseCurves> jointCurves)
        {
            if (jointCurves.TryGetValue(joint, out var curves))
            {
                return curves.Evaluate(time);
            }
            else
            {
                // Zero Identity Transform (pos 0, rot 0)
                return TransformData.ZeroIdentity();
            }
        }

        public TransformData EvaluateObject(float time, string name)
        {
            if (objectCurves.TryGetValue(name, out var curves))
            {
                return curves.Evaluate(time);
            }
            else
            {
                // Zero Identity Transform (pos 0, rot 0)
                return TransformData.ZeroIdentity();
            }
        }

        private IEnumerable<AnimationCurve> GetAllAnimationCurves()
        {
            yield return handTrackedCurveLeft;
            yield return handTrackedCurveRight;
            yield return handPinchCurveLeft;
            yield return handPinchCurveRight;

            foreach (var curves in handJointCurvesLeft.Values)
            {
                yield return curves.PositionX;
                yield return curves.PositionY;
                yield return curves.PositionZ;
                yield return curves.RotationX;
                yield return curves.RotationY;
                yield return curves.RotationZ;
                yield return curves.RotationW;
            }

            foreach (var curves in handJointCurvesRight.Values)
            {
                yield return curves.PositionX;
                yield return curves.PositionY;
                yield return curves.PositionZ;
                yield return curves.RotationX;
                yield return curves.RotationY;
                yield return curves.RotationZ;
                yield return curves.RotationW;
            }

            foreach(var curves in objectCurves.Values)
            {
                yield return curves.PositionX;
                yield return curves.PositionY;
                yield return curves.PositionZ;
                yield return curves.RotationX;
                yield return curves.RotationY;
                yield return curves.RotationZ;
            }

            yield return cameraCurves.PositionX;
            yield return cameraCurves.PositionY;
            yield return cameraCurves.PositionZ;
            yield return cameraCurves.RotationX;
            yield return cameraCurves.RotationY;
            yield return cameraCurves.RotationZ;
            yield return cameraCurves.RotationW;
            yield return gazeCurves.PositionX;
            yield return gazeCurves.PositionY;
            yield return gazeCurves.PositionZ;
            yield return gazeCurves.RotationX;
            yield return gazeCurves.RotationY;
            yield return gazeCurves.RotationZ;
            yield return gazeCurves.RotationW;
        }

        /// <summary>
        /// Utility function that creates a non-interpolated keyframe suitable for boolean values.
        /// </summary>
        private static void AddBoolKey(AnimationCurve curve, float time, bool value)
        {
            float fvalue = value ? 1.0f : 0.0f;
            // Set tangents and weights such than the input value is cut off and out tangent is constant.
            var keyframe = new Keyframe(time, fvalue, 0.0f, 0.0f, 0.0f, BoolOutWeight);

            keyframe.weightedMode = WeightedMode.Both;
            curve.AddKey(keyframe);
        }

        /// <summary>
        /// Add a float value to an animation curve.
        /// </summary>
        private static void AddFloatKey(AnimationCurve curve, float time, float value)
        {
            // Use linear interpolation by setting tangents and weights to zero.
            var keyframe = new Keyframe(time, value, 0.0f, 0.0f, 0.0f, 0.0f);

            keyframe.weightedMode = WeightedMode.Both;
            curve.AddKey(keyframe);
        }

        /// <summary>
        /// Add a vector value to an animation curve.
        /// </summary>
        private static void AddVectorKey(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, float time, Vector3 vector)
        {
            curveX.AddKey(time, vector.x);
            curveY.AddKey(time, vector.y);
            curveZ.AddKey(time, vector.z);
        }

        private static void AddJointPoseKeys(Dictionary<TrackedHandJoint, PoseCurves> jointCurves, Dictionary<TrackedHandJoint, TransformData> jointsTransformData, TrackedHandJoint joint, float time)
        {
            if (!jointsTransformData.TryGetValue(joint, out var transformData))
            {
                return;
            }

            if (!jointCurves.TryGetValue(joint, out var curves))
            {
                curves = new PoseCurves();
                jointCurves.Add(joint, curves);
            }

            curves.AddKey(time, transformData);
        }

        private static void AddObjectPoseKeys(Dictionary<string, PoseCurves> objectCurves, TransformData objectTransform, string objectName, float time)
        {
            if (!objectCurves.TryGetValue(objectName, out var curves))
            {
                curves = new PoseCurves();
                objectCurves.Add(objectName, curves);
            }
            curves.AddKey(time, objectTransform);
        }

        /// <summary>
        /// Add a pose keyframe to an animation curve.
        /// Keys are only added if the value changes sufficiently.
        /// </summary>
        [Obsolete("Use FromRecordingBuffer to construct new InputAnimations")]
        private static void AddPoseKeyFiltered(PoseCurves curves, float time, MixedRealityPose pose, float positionThreshold, float rotationThreshold)
        {
            AddPositionKeyFiltered(curves.PositionX, curves.PositionY, curves.PositionZ, time, pose.Position, positionThreshold);
            AddRotationKeyFiltered(curves.RotationX, curves.RotationY, curves.RotationZ, curves.RotationW, time, pose.Rotation, rotationThreshold);
        }

        /// <summary>
        /// Add a vector keyframe to animation curve if the threshold distance to the previous value is exceeded.
        /// Otherwise replace the last keyframe instead of adding a new one.
        /// </summary>
        [Obsolete("Use FromRecordingBuffer to construct new InputAnimations")]
        private static void AddPositionKeyFiltered(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, float time, Vector3 position, float threshold)
        {
            float sqrThreshold = threshold * threshold;

            int iX = FindKeyframeInterval(curveX, time);
            int iY = FindKeyframeInterval(curveY, time);
            int iZ = FindKeyframeInterval(curveZ, time);

            if (iX > 0 && iY > 0 && iZ > 0)
            {
                var v0 = new Vector3(curveX.keys[iX - 1].value, curveY.keys[iY - 1].value, curveZ.keys[iZ - 1].value);
                var v1 = new Vector3(curveX.keys[iX].value, curveY.keys[iY].value, curveZ.keys[iZ].value);

                // Merge the preceding two intervals if difference is small enough
                if ((v1 - v0).sqrMagnitude <= sqrThreshold && (position - v1).sqrMagnitude <= sqrThreshold)
                {
                    curveX.RemoveKey(iX);
                    curveY.RemoveKey(iY);
                    curveZ.RemoveKey(iZ);
                }
            }

            AddFloatKey(curveX, time, position.x);
            AddFloatKey(curveY, time, position.y);
            AddFloatKey(curveZ, time, position.z);
        }

        /// <summary>
        /// Add a quaternion keyframe to animation curve if the threshold angular difference (in degrees) to the previous value is exceeded.
        /// Otherwise replace the last keyframe instead of adding a new one.
        /// </summary>
        [Obsolete("Use FromRecordingBuffer to construct new InputAnimations")]
        private static void AddRotationKeyFiltered(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW, float time, Quaternion rotation, float threshold)
        {
            // Precompute the dot product threshold so that dot product can be used for comparison instead of angular difference
            float compThreshold = Mathf.Sqrt((Mathf.Cos(threshold * Mathf.PI / 180f) + 1f) / 2f);
            int iX = FindKeyframeInterval(curveX, time);
            int iY = FindKeyframeInterval(curveY, time);
            int iZ = FindKeyframeInterval(curveZ, time);
            int iW = FindKeyframeInterval(curveW, time);

            if (iX > 0 && iY > 0 && iZ > 0 && iW > 0)
            {
                var v0 = new Quaternion(curveX.keys[iX - 1].value, curveY.keys[iY - 1].value, curveZ.keys[iZ - 1].value, curveW.keys[iW - 1].value);
                var v1 = new Quaternion(curveX.keys[iX].value, curveY.keys[iY].value, curveZ.keys[iZ].value, curveW.keys[iW].value);

                // Merge the preceding two intervals if difference is small enough
                if (Quaternion.Dot(v0, v1) >= compThreshold && Quaternion.Dot(rotation, v1) >= compThreshold)
                {
                    curveX.RemoveKey(iX);
                    curveY.RemoveKey(iY);
                    curveZ.RemoveKey(iZ);
                    curveW.RemoveKey(iW);
                }
            }

            AddFloatKey(curveX, time, rotation.x);
            AddFloatKey(curveY, time, rotation.y);
            AddFloatKey(curveZ, time, rotation.z);
            AddFloatKey(curveW, time, rotation.w);
        }

        private static void CurvesToStream(StreamWriter writer, List<AnimationCurve> curves)
        {
            if (curves == null || curves.Count == 0)
            {
                writer.WriteLine(0);
                return;
            }

            int keyframeCount = 0;
            int longestCurveIdx = -1;
            for (int i = 0; i < curves.Count; ++i)
            {
                if (curves[i].length > keyframeCount)
                {
                    keyframeCount = curves[i].length;
                    longestCurveIdx = i;
                }
            }

            writer.WriteLine(keyframeCount);
            for (int i = 0; i < keyframeCount; ++i)
            {
                var time = curves[longestCurveIdx].keys[i].time;
                writer.Write(time);
                for (int j = 0; j < curves.Count; ++j)
                {
                    writer.Write(", ");
                    if (curves[j].length <= i || curves[j].keys[i].time != time)
                    {
                        writer.Write(curves[j].Evaluate(time));
                    } else
                    {
                        writer.Write(curves[j].keys[i].value);
                    }
                }
                writer.WriteLine("");
            }
        }

        private static List<AnimationCurve> CurvesFromStream(StreamReader reader, int curveCount)
        {
            if (curveCount == 0)
            {
                return new List<AnimationCurve>();
            }
            List<Keyframe[]> keyframes = new List<Keyframe[]>(curveCount);
            //Debug.Log($"reader.ReadLine(),{reader.ReadLine()}");
            int keyframeCount = int.Parse(reader.ReadLine());
            for (int j = 0; j < curveCount; ++j)
            {
                keyframes.Add(new Keyframe[keyframeCount]);
            }
            for (int i = 0; i < keyframeCount; ++i)
            {
                var poseString = reader.ReadLine().Split(',');
                var time = float.Parse(poseString[0]);
                for (int j = 0; j < curveCount; ++j)
                {
                    keyframes[j][i].time = time;
                    keyframes[j][i].value = float.Parse(poseString[j + 1]);
                    keyframes[j][i].weightedMode = WeightedMode.Both;
                }
            }
            List<AnimationCurve> result = new List<AnimationCurve>(curveCount);
            for (int j = 0; j < curveCount; ++j)
            {
                AnimationCurve curve = new AnimationCurve();
                curve.keys = keyframes[j];
                result.Add(curve);
            }
            return result;
        }

        private static void ObjectCurvesToStream(StreamWriter writer, Dictionary<string, PoseCurves> objectCurves)
        {
            writer.WriteLine("OBJECT_POSES");
            writer.WriteLine(objectCurves.Count);

            foreach (var entry in objectCurves)
            {
                writer.WriteLine(entry.Key);
                CurvesToStream(writer, entry.Value.GetAnimationCurves());
            }
        }

        private static void ObjectCurvesFromStream(StreamReader reader, Dictionary<string, PoseCurves> objectCurves)
        {
            var header = reader.ReadLine();
            if (header != "OBJECT_POSES")
            {
                Debug.LogError("Excepted OBJECT_POSES header, got: " + header);
                return;
            }
            int objectCount = int.Parse(reader.ReadLine());
            for (int i = 0; i < objectCount; i++)
            {
                string name = reader.ReadLine();
                var curves = CurvesFromStream(reader, 10);
                PoseCurves poseCurves = new PoseCurves();
                poseCurves.PositionX = curves[0];
                poseCurves.PositionY = curves[1];
                poseCurves.PositionZ = curves[2];
                poseCurves.RotationX = curves[3];
                objectCurves.Add(name, poseCurves);
            }
        }

        /// <summary>
        /// Removes points from a set of curves representing a 3D position, such that the error resulting from removing a point never exceeds 'threshold' units.
        /// </summary>
        /// <param name="threshold">The maximum permitted error between the old and new curves, in units.</param>
        /// <param name="partitionSize">The size of the partitions of the curves that will be optimized independently. Larger values will optimize the curves better, but may take longer.</param>
        /// <remarks>Uses the Ramer–Douglas–Peucker algorithm</remarks>
        private static void OptimizePositionCurve(ref AnimationCurve curveX, ref AnimationCurve curveY, ref AnimationCurve curveZ, float threshold, int partitionSize)
        {
            float sqrThreshold = threshold * threshold;
            var inCurveX = curveX;
            var inCurveY = curveY;
            var inCurveZ = curveZ;
            // Create new curves to avoid deleting points while iterating.
            var outCurveX = new AnimationCurve();
            var outCurveY = new AnimationCurve();
            var outCurveZ = new AnimationCurve();

            outCurveX.AddKey(curveX[0]);
            outCurveY.AddKey(curveY[0]);
            outCurveZ.AddKey(curveZ[0]);

            if (partitionSize == 0)
            {
                Recurse(0, curveX.length - 1);
                outCurveX.AddKey(curveX[curveX.length - 1]);
                outCurveY.AddKey(curveY[curveY.length - 1]);
                outCurveZ.AddKey(curveZ[curveZ.length - 1]);
            }
            else
            {
                for (int i = 0, j = partitionSize; i < curveX.length - partitionSize; i += partitionSize, j = Mathf.Min(j + partitionSize, curveX.length - 1))
                {
                    Recurse(i, j);
                    outCurveX.AddKey(curveX[j]);
                    outCurveY.AddKey(curveY[j]);
                    outCurveZ.AddKey(curveZ[j]);
                }
            }

            curveX = outCurveX;
            curveY = outCurveY;
            curveZ = outCurveZ;

            void Recurse(int start, int end)
            {
                if (start + 1 >= end - 1)
                {
                    return;
                }

                int bestIndex = -1;
                float bestDistance = 0f;
                float startTime = inCurveX[start].time;
                float endTime = inCurveX[end].time;
                var startPosition = new Vector3(inCurveX[start].value, inCurveY[start].value, inCurveZ[start].value);
                var endPosition = new Vector3(inCurveX[end].value, inCurveY[end].value, inCurveZ[end].value);

                for (int i = start + 1; i <= end - 1; i++)
                {
                    var position = new Vector3(inCurveX[i].value, inCurveY[i].value, inCurveZ[i].value);
                    var interp = Vector3.Lerp(startPosition, endPosition, Mathf.InverseLerp(startTime, endTime, inCurveX[i].time));

                    float distance = (position - interp).sqrMagnitude;

                    if (distance > bestDistance)
                    {
                        bestIndex = i;
                        bestDistance = distance;
                    }
                }

                if (bestDistance < sqrThreshold || bestIndex < 0)
                {
                    return;
                }

                outCurveX.AddKey(inCurveX[bestIndex]);
                outCurveY.AddKey(inCurveY[bestIndex]);
                outCurveZ.AddKey(inCurveZ[bestIndex]);
                Recurse(start, bestIndex);
                Recurse(bestIndex, end);
            }
        }

        /// <summary>
        /// Removes points from a set of curves representing a 3D direction vector, such that the error resulting from removing a point never exceeds 'threshold' degrees.
        /// </summary>
        /// <param name="threshold">The maximum permitted error between the old and new curves, in degrees.</param>
        /// <param name="partitionSize">The size of the partitions of the curves that will be optimized independently. Larger values will optimize the curves better, but may take longer.</param>
        /// <remarks>Uses the Ramer–Douglas–Peucker algorithm</remarks>
        private static void OptimizeDirectionCurve(ref AnimationCurve curveX, ref AnimationCurve curveY, ref AnimationCurve curveZ, float threshold, int partitionSize)
        {
            float cosThreshold = Mathf.Cos(threshold * Mathf.PI / 180f);
            var inCurveX = curveX;
            var inCurveY = curveY;
            var inCurveZ = curveZ;
            // Create new curves to avoid deleting points while iterating.
            var outCurveX = new AnimationCurve();
            var outCurveY = new AnimationCurve();
            var outCurveZ = new AnimationCurve();

            outCurveX.AddKey(curveX[0]);
            outCurveY.AddKey(curveY[0]);
            outCurveZ.AddKey(curveZ[0]);

            if (partitionSize == 0)
            {
                Recurse(0, curveX.length - 1);
                outCurveX.AddKey(curveX[curveX.length - 1]);
                outCurveY.AddKey(curveY[curveY.length - 1]);
                outCurveZ.AddKey(curveZ[curveZ.length - 1]);
            }
            else
            {
                for (int i = 0, j = partitionSize; i < curveX.length - partitionSize; i += partitionSize, j = Mathf.Min(j + partitionSize, curveX.length - 1))
                {
                    Recurse(i, j);
                    outCurveX.AddKey(curveX[j]);
                    outCurveY.AddKey(curveY[j]);
                    outCurveZ.AddKey(curveZ[j]);
                }
            }

            curveX = outCurveX;
            curveY = outCurveY;
            curveZ = outCurveZ;

            void Recurse(int start, int end)
            {
                if (start + 1 >= end - 1)
                {
                    return;
                }

                int bestIndex = -1;
                float bestDot = 1f;
                float startTime = inCurveX[start].time;
                float endTime = inCurveX[end].time;
                var startPosition = new Vector3(inCurveX[start].value, inCurveY[start].value, inCurveZ[start].value);
                var endPosition = new Vector3(inCurveX[end].value, inCurveY[end].value, inCurveZ[end].value);

                for (int i = start + 1; i <= end - 1; i++)
                {
                    var position = new Vector3(inCurveX[i].value, inCurveY[i].value, inCurveZ[i].value);
                    var interp = Vector3.Lerp(startPosition, endPosition, Mathf.InverseLerp(startTime, endTime, inCurveX[i].time)).normalized;

                    float dot = Vector3.Dot(position, interp);

                    if (dot < bestDot)
                    {
                        bestIndex = i;
                        bestDot = dot;
                    }
                }

                if (bestDot > cosThreshold || bestIndex < 0)
                {
                    return;
                }

                outCurveX.AddKey(inCurveX[bestIndex]);
                outCurveY.AddKey(inCurveY[bestIndex]);
                outCurveZ.AddKey(inCurveZ[bestIndex]);
                Recurse(start, bestIndex);
                Recurse(bestIndex, end);
            }
        }

        /// <summary>
        /// Removes points from a set of curves representing a quaternion, such that the error resulting from removing a point never exceeds 'threshold' degrees.
        /// </summary>
        /// <param name="threshold">The maximum permitted error between the old and new curves, in degrees</param>
        /// <param name="partitionSize">The size of the partitions of the curves that will be optimized independently. Larger values will optimize the curves better, but may take longer.</param>
        /// <remarks>Uses the Ramer–Douglas–Peucker algorithm</remarks>
        private static void OptimizeRotationCurve(ref AnimationCurve curveX, ref AnimationCurve curveY, ref AnimationCurve curveZ, ref AnimationCurve curveW, float threshold, int partitionSize)
        {
            float compThreshold = Mathf.Sqrt((Mathf.Cos(threshold * Mathf.PI / 180f) + 1f) / 2f);
            var inCurveX = curveX;
            var inCurveY = curveY;
            var inCurveZ = curveZ;
            var inCurveW = curveW;
            // Create new curves to avoid deleting points while iterating.
            var outCurveX = new AnimationCurve();
            var outCurveY = new AnimationCurve();
            var outCurveZ = new AnimationCurve();
            var outCurveW = new AnimationCurve();

            outCurveX.AddKey(curveX[0]);
            outCurveY.AddKey(curveY[0]);
            outCurveZ.AddKey(curveZ[0]);
            outCurveW.AddKey(curveW[0]);

            if (partitionSize == 0)
            {
                Recurse(0, curveX.length - 1);
                outCurveX.AddKey(curveX[curveX.length - 1]);
                outCurveY.AddKey(curveY[curveY.length - 1]);
                outCurveZ.AddKey(curveZ[curveZ.length - 1]);
                outCurveW.AddKey(curveZ[curveW.length - 1]);
            }
            else
            {
                for (int i = 0, j = partitionSize; i < curveX.length - partitionSize; i += partitionSize, j = Mathf.Min(j + partitionSize, curveX.length - 1))
                {
                    Recurse(i, j);
                    outCurveX.AddKey(curveX[j]);
                    outCurveY.AddKey(curveY[j]);
                    outCurveZ.AddKey(curveZ[j]);
                    outCurveZ.AddKey(curveW[j]);
                }
            }

            curveX = outCurveX;
            curveY = outCurveY;
            curveZ = outCurveZ;
            curveW = outCurveW;

            void Recurse(int start, int end)
            {
                if (start + 1 >= end - 1)
                {
                    return;
                }

                int bestIndex = -1;
                float bestDot = 1f;
                float startTime = inCurveX[start].time;
                float endTime = inCurveX[end].time;
                var startRotation = new Quaternion(inCurveX[start].value, inCurveY[start].value, inCurveZ[start].value, inCurveW[start].value).normalized;
                var endRotation = new Quaternion(inCurveX[end].value, inCurveY[end].value, inCurveZ[end].value, inCurveW[end].value).normalized;

                for (int i = start + 1; i <= end - 1; i++)
                {
                    var rotation = new Quaternion(inCurveX[i].value, inCurveY[i].value, inCurveZ[i].value, inCurveW[i].value).normalized;
                    var interp = Quaternion.Lerp(startRotation, endRotation, Mathf.InverseLerp(startTime, endTime, inCurveX[i].time));

                    float dot = Quaternion.Dot(rotation, interp);

                    if (dot < bestDot)
                    {
                        bestIndex = i;
                        bestDot = dot;
                    }
                }

                if (bestDot > compThreshold || bestIndex < 0)
                {
                    return;
                }

                outCurveX.AddKey(inCurveX[bestIndex]);
                outCurveY.AddKey(inCurveY[bestIndex]);
                outCurveZ.AddKey(inCurveZ[bestIndex]);
                Recurse(start, bestIndex);
                Recurse(bestIndex, end);
            }
        }

        /// <summary>
        /// Utility function that creates a non-interpolated keyframe suitable for boolean values.
        /// Keys are only added if the value changes.
        /// Returns the index of the newly added keyframe, or -1 if no keyframe has been added.
        /// </summary>
        [Obsolete("Use FromRecordingBuffer to construct new InputAnimations")]
        private static int AddBoolKeyFiltered(AnimationCurve curve, float time, bool value)
        {
            float fvalue = value ? 1.0f : 0.0f;
            // Set tangents and weights such than the input value is cut off and out tangent is constant.
            var keyframe = new Keyframe(time, fvalue, 0.0f, 0.0f, 0.0f, BoolOutWeight);
            keyframe.weightedMode = WeightedMode.Both;

            int insertAfter = FindKeyframeInterval(curve, time);
            if (insertAfter >= 0 && curve.keys[insertAfter].value == fvalue)
            {
                // Value unchanged from previous key, ignore
                return -1;
            }

            int insertBefore = insertAfter + 1;
            if (insertBefore < curve.keys.Length && curve.keys[insertBefore].value == fvalue)
            {
                // Same value as next key, replace next key
                return curve.MoveKey(insertBefore, keyframe);
            }

            return curve.AddKey(keyframe);
        }

        /// <summary>
        /// Find an index i in the sorted events list, such that events[i].time &lt;= time &lt; events[i+1].time.
        /// </summary>
        /// <returns>
        /// 0 &lt;= i &lt; eventCount if a full interval could be found.
        /// -1 if time is less than the first event time.
        /// eventCount-1 if time is greater than the last event time.
        /// </returns>
        /// <remarks>
        /// Uses binary search.
        /// </remarks>
        private static int FindKeyframeInterval(AnimationCurve curve, float time)
        {
            var keys = curve.keys;
            int lowIdx = -1;
            int highIdx = keys.Length;
            while (lowIdx < highIdx - 1)
            {
                int midIdx = (lowIdx + highIdx) >> 1;
                if (time >= keys[midIdx].time)
                {
                    lowIdx = midIdx;
                }
                else
                {
                    highIdx = midIdx;
                }
            }
            return lowIdx;
        }


        private static AnimationCurve Clone(AnimationCurve curve)
        {
            var copy = new AnimationCurve();
            copy.preWrapMode = curve.preWrapMode;
            copy.postWrapMode = curve.postWrapMode;
            foreach (var key in curve.keys)
            {
                copy.AddKey(Clone(key));
            }
            return copy;
        }

        private static Keyframe Clone(Keyframe keyframe)
        {
            var copy = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, keyframe.outTangent, keyframe.inWeight, keyframe.outWeight);
            copy.weightedMode = WeightedMode.Both;
            return copy;
        }
    }
}