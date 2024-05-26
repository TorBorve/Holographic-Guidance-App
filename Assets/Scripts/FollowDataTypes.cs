
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using System;
using System.Threading.Tasks;

namespace Tutorials
{
    public class DataPoint
    {
        public float timeStamp;
        public bool leftIsTracked;
        public bool rightIsTracked;
        public Dictionary<TrackedHandJoint, MixedRealityPose> leftHand;
        public float leftSpeed;
        public Dictionary<TrackedHandJoint, MixedRealityPose> rightHand;
        public float rightSpeed;

        public DataPoint() : this(0f) { }

        public DataPoint(float time)
        {
            timeStamp = time;
            leftIsTracked = false;
            rightIsTracked = false;
            leftHand = new Dictionary<TrackedHandJoint, MixedRealityPose>();
            leftSpeed = 0f;
            rightHand = new Dictionary<TrackedHandJoint, MixedRealityPose>();
            rightSpeed = 0f;
        }

        public static DataPoint Interpolate(DataPoint firstDataPoint, DataPoint secondDataPoint, float alpha)
        {
            DataPoint interpolatedDataPoint = new DataPoint(firstDataPoint.timeStamp + alpha * (secondDataPoint.timeStamp - firstDataPoint.timeStamp));

            if (!firstDataPoint.leftIsTracked)
            {
                interpolatedDataPoint.leftHand = secondDataPoint.leftHand;
                interpolatedDataPoint.leftIsTracked = secondDataPoint.leftIsTracked;
                interpolatedDataPoint.leftSpeed = secondDataPoint.leftSpeed;
            }
            else if (!secondDataPoint.leftIsTracked)
            {
                interpolatedDataPoint.leftHand = firstDataPoint.leftHand;
                interpolatedDataPoint.leftIsTracked = firstDataPoint.leftIsTracked;
                interpolatedDataPoint.leftSpeed = firstDataPoint.leftSpeed;
            }
            else
            {
                interpolatedDataPoint.leftIsTracked = true;
                interpolatedDataPoint.leftHand = InterpolateJoints(firstDataPoint.leftHand, secondDataPoint.leftHand, alpha);
                interpolatedDataPoint.leftSpeed = InterpolateSpeed(firstDataPoint.leftSpeed, secondDataPoint.leftSpeed, alpha);
            }

            if (!firstDataPoint.rightIsTracked)
            {
                interpolatedDataPoint.rightHand = secondDataPoint.rightHand;
                interpolatedDataPoint.rightIsTracked = secondDataPoint.rightIsTracked;
                interpolatedDataPoint.rightSpeed = secondDataPoint.rightSpeed;
            }
            else if (!secondDataPoint.rightIsTracked)
            {
                interpolatedDataPoint.rightHand = firstDataPoint.rightHand;
                interpolatedDataPoint.rightIsTracked = firstDataPoint.rightIsTracked;
                interpolatedDataPoint.rightSpeed = firstDataPoint.rightSpeed;
            }
            else
            {
                interpolatedDataPoint.rightIsTracked = true;
                interpolatedDataPoint.rightHand = InterpolateJoints(firstDataPoint.rightHand, secondDataPoint.rightHand, alpha);
                interpolatedDataPoint.rightSpeed = InterpolateSpeed(firstDataPoint.rightSpeed, secondDataPoint.rightSpeed, alpha);
            }
            return interpolatedDataPoint;
        }

        private static Dictionary<TrackedHandJoint, MixedRealityPose> InterpolateJoints(Dictionary<TrackedHandJoint, MixedRealityPose> firstJoints, Dictionary<TrackedHandJoint, MixedRealityPose> secondJoints, float alpha)
        {
            var result = new Dictionary<TrackedHandJoint, MixedRealityPose>();
            int num_joints = Enum.GetNames(typeof(TrackedHandJoint)).Length;
            for (int i = 1; i < num_joints; ++i)
            {
                var firstPose = firstJoints[(TrackedHandJoint)i];
                var secondPose = secondJoints[(TrackedHandJoint)i];
                var resPose = InterpolatePose(firstPose, secondPose, alpha);
                result.Add((TrackedHandJoint)i, resPose);
            }
            return result;
        }

        private static MixedRealityPose InterpolatePose(MixedRealityPose firstPose, MixedRealityPose secondPose, float alpha)
        {
            var resQuat = Quaternion.Slerp(firstPose.Rotation, secondPose.Rotation, alpha);
            var resPos = firstPose.Position + alpha * (secondPose.Position - firstPose.Position);
            var result = new MixedRealityPose(resPos, resQuat);
            return result;
        }

        private static float InterpolateSpeed(float firstSpeed, float secondSpeed, float alpha)
        {
            return firstSpeed + alpha * (secondSpeed - firstSpeed);
        }

        public static DataPoint FromKeyframe(InputRecordingBuffer.Keyframe keyframe)
        {
            DataPoint dataPoint = new DataPoint(keyframe.Time);
            dataPoint.leftHand = FromTFDictionary(keyframe.LeftJointsTransformData);
            dataPoint.rightHand = FromTFDictionary(keyframe.RightJointsTransformData);
            return dataPoint;
        }

        private static Dictionary<TrackedHandJoint, MixedRealityPose> FromTFDictionary(Dictionary<TrackedHandJoint, TransformData> tfDict)
        {
            var MRPoseDict = new Dictionary<TrackedHandJoint, MixedRealityPose>();
            foreach (TrackedHandJoint jointType in tfDict.Keys)
            {
                TransformData tfData = tfDict[jointType];
                UnityEngine.Vector3 position = new UnityEngine.Vector3(tfData.posx, tfData.posy, tfData.posz);
                UnityEngine.Quaternion rotation = new UnityEngine.Quaternion(tfData.rotx, tfData.roty, tfData.rotz, tfData.rotw);
                MixedRealityPose MRPose = new MixedRealityPose(position, rotation);
                MRPoseDict.Add(jointType, MRPose);
            }
            return MRPoseDict;
        }

        public static float CalcSpeed(DataPoint point1, DataPoint point2, Handedness hand)
        {
            MixedRealityPose wristPose1, wristPose2;

            switch (hand)
            {
                case Handedness.Right:
                    wristPose1 = point1.rightHand[TrackedHandJoint.Wrist];
                    wristPose2 = point2.rightHand[TrackedHandJoint.Wrist];
                    break;
                case Handedness.Left:
                    wristPose1 = point1.leftHand[TrackedHandJoint.Wrist];
                    wristPose2 = point2.leftHand[TrackedHandJoint.Wrist];
                    break;
                default:
                    throw new ArgumentException("Invalid hand type specified.");
            }

            Vector3 position1 = wristPose1.Position;
            Vector3 position2 = wristPose2.Position;
            float distance = Vector3.Distance(position1, position2);

            float timeDifference = point2.timeStamp - point1.timeStamp;
            if (timeDifference == 0)
            {
                timeDifference = 1f / 60; // assume 60fps
            }

            float speed = distance / timeDifference;
            return speed;
        }
    }

    public class FingerAndWristData
    {
        public Vector3 wristPosition;
        public Vector3 thumbPosition;
        public Vector3 indexPosition;
        public Vector3 middlePosition;
        public Vector3 ringPosition;
        public Vector3 pinkyPosition;

        public FingerAndWristData(
            Vector3 wristPos,
            Vector3 thumbPos,
            Vector3 indexPos,
            Vector3 middlePos,
            Vector3 ringPos,
            Vector3 pinkyPos)
        {
            this.wristPosition = wristPos;
            this.thumbPosition = thumbPos;
            this.indexPosition = indexPos;
            this.middlePosition = middlePos;
            this.ringPosition = ringPos;
            this.pinkyPosition = pinkyPos;
        }

        public FingerAndWristData(Dictionary<TrackedHandJoint, MixedRealityPose> handDict)
        {
            wristPosition = handDict[TrackedHandJoint.Wrist].Position;
            thumbPosition = handDict[TrackedHandJoint.ThumbTip].Position;
            indexPosition = handDict[TrackedHandJoint.IndexTip].Position;
            middlePosition = handDict[TrackedHandJoint.MiddleTip].Position;
            ringPosition = handDict[TrackedHandJoint.RingTip].Position;
            pinkyPosition = handDict[TrackedHandJoint.PinkyTip].Position;
        }

        public FingerAndWristData(IMixedRealityHand hand)
        {
            bool success = true;
            if (hand.TryGetJoint(TrackedHandJoint.Wrist, out MixedRealityPose wristPose))
            {
                wristPosition = wristPose.Position;
            }
            else
            {
                success = false;
            }
            if (hand.TryGetJoint(TrackedHandJoint.ThumbTip, out MixedRealityPose thumbPose))
            {
                thumbPosition = thumbPose.Position;
            }
            else
            {
                success = false;
            }
            if (hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose indexPose))
            {
                indexPosition = indexPose.Position;
            }
            else
            {
                success = false;
            }
            if (hand.TryGetJoint(TrackedHandJoint.MiddleTip, out MixedRealityPose middlePose))
            {
                middlePosition = middlePose.Position;
            }
            else
            {
                success = false;
            }
            if (hand.TryGetJoint(TrackedHandJoint.RingTip, out MixedRealityPose ringPose))
            {
                ringPosition = ringPose.Position;
            }
            else
            {
                success = false;
            }
            if (hand.TryGetJoint(TrackedHandJoint.PinkyTip, out MixedRealityPose pinkyPose))
            {
                pinkyPosition = pinkyPose.Position;
            }
            else
            {
                success = false;
            }
            if (!success)
            {
                Debug.LogError("Could not get all joint from hand");
                throw new Exception("Could not get all joints from hand");
            }
        }
        public override string ToString()
        {
            string txt = "";
            txt += "Wrist: " + wristPosition.ToString();
            txt += ", Thumb: " + thumbPosition.ToString();
            txt += ", Index: " + indexPosition.ToString();
            txt += ", Middle: " + middlePosition.ToString();
            txt += ", Ring: " + ringPosition.ToString();
            txt += ", Pinky: " + pinkyPosition.ToString();
            return txt;
        }

        public static float Distance(FingerAndWristData a, FingerAndWristData b, float[] weight = null)
        {
            const int N_dim = 6;
            if (weight == null)
            {
                //weight = new float[N_dim] { 1f / N_dim, 1f / N_dim, 1f / N_dim, 1f / N_dim, 1f / N_dim, 1f / N_dim };
                weight = new float[N_dim] { 1f, 0f, 0f, 0f, 0f, 0f};
            }
            if (weight.Count() != N_dim)
            {
                Debug.LogError("Invalid Weight list");
                throw new Exception("Invalid Weights for distance caluculation");
            }
            float dist = 0.0f;
            dist += weight[0] * (float)(a.wristPosition - b.wristPosition).magnitude;
            dist += weight[1] * (float)(a.thumbPosition - b.thumbPosition).magnitude;
            dist += weight[2] * (float)(a.indexPosition - b.indexPosition).magnitude;
            dist += weight[3] * (float)(a.middlePosition - b.middlePosition).magnitude;
            dist += weight[4] * (float)(a.ringPosition - b.ringPosition).magnitude;
            dist += weight[5] * (float)(a.pinkyPosition - b.pinkyPosition).magnitude;
            return dist;
        }
    }
    public class RecordingData
    {
        private List<DataPoint> dataPoints;

        public RecordingData()
        {
            dataPoints = new List<DataPoint>();
        }

        public RecordingData(List<DataPoint> dataPoints)
        {
            this.dataPoints = new List<DataPoint>(dataPoints);
            UpdateSpeedEstimates();
        }

        public float GetEndTime()
        {
            if (dataPoints.Count() == 0)
            {
                return 0f;
            }
            return dataPoints[dataPoints.Count() - 1].timeStamp;
        }

        public float GetStartTime()
        {
            if (dataPoints.Count() == 0)
            {
                return 0f;
            }
            return dataPoints[0].timeStamp;
        }

        public float GetDuration()
        {
            return GetEndTime() - GetStartTime();
        }

        public DataPoint GetDataPointIndex(int i)
        {
            return dataPoints[i];
        }

        public int Count()
        {
            return dataPoints.Count();
        }

        private int ClosestIndex(float queryTime)
        {
            if (dataPoints.Count() == 0)
            {
                throw new Exception("DataPoints is empty");
            }
            int first = 0;
            int last = dataPoints.Count() - 1;
            int mid = 0;
            do
            {
                mid = first + (last - first) / 2;
                if (queryTime > dataPoints[mid].timeStamp)
                {
                    first = mid + 1;
                }
                else
                {
                    last = mid - 1;
                }
                if (queryTime == dataPoints[mid].timeStamp)
                {
                    return mid;
                }
            } while (first <= last);
            return mid;
        }

        public DataPoint InterpolateDataAtTime(float time)
        {
            int secondIndex = ClosestIndex(time);
            if (secondIndex == 0)
            {
                return dataPoints[0];
            }
            int firstIndex = secondIndex - 1;

            float firstTime = dataPoints[firstIndex].timeStamp;
            float secondTime = dataPoints[secondIndex].timeStamp;
            if (firstTime == secondTime)
            {
                return dataPoints[firstIndex];
            }
            else if (firstTime >= time)
            {
                return dataPoints[firstIndex];
            }
            else if (secondTime <= time)
            {
                return dataPoints[secondIndex];
            }

            float alpha = (time - firstTime) / (secondTime - firstTime);
            DataPoint interpolatedPoint = DataPoint.Interpolate(dataPoints[firstIndex], dataPoints[secondIndex], alpha);
            return interpolatedPoint;
        }

        public static async Task<RecordingData> FromInputAnimationAsync(InputAnimation inputAnimation)
        {
            return FromInputAnimation(inputAnimation);
        }

        public static RecordingData FromInputAnimation(InputAnimation inputAnimation)
        {
            if (inputAnimation == null)
            {
                throw new Exception("InputAnimation is null");
            }
            Keyframe[] leftTrackedCurve = inputAnimation.handTrackedCurveLeft.keys;
            Keyframe[] rightTrackedCurve = inputAnimation.handTrackedCurveRight.keys;

            int num_data_points = leftTrackedCurve.Count();
            Debug.Assert(leftTrackedCurve.Count() == rightTrackedCurve.Count());
            List<DataPoint> dataPoints = new List<DataPoint>(new DataPoint[num_data_points]);
            for (int i = 0; i < num_data_points; i++)
            {
                DataPoint dataPoint = new DataPoint(leftTrackedCurve[i].time);
                Debug.Assert(leftTrackedCurve[i].time == rightTrackedCurve[i].time);

                dataPoint.leftIsTracked = leftTrackedCurve[i].value > 0.5f;
                dataPoint.rightIsTracked = rightTrackedCurve[i].value > 0.5f;
                dataPoints[i] = dataPoint;
            }

            Dictionary<TrackedHandJoint, InputAnimation.PoseCurves> handJointCurvesLeft = inputAnimation.handJointCurvesLeft;
            Dictionary<TrackedHandJoint, InputAnimation.PoseCurves> handJointCurvesRight = inputAnimation.handJointCurvesRight;
            int num_joints = Enum.GetNames(typeof(TrackedHandJoint)).Length;

            var zeroCurve = new AnimationCurve();
            var oneCurve = new AnimationCurve();
            for (int i = 0; i < num_data_points; i++)
            {
                float time = leftTrackedCurve[i].time;
                var zeroKeyframe = new Keyframe(time, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
                zeroKeyframe.weightedMode = WeightedMode.Both;
                var oneKeyframe = new Keyframe(time, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f);
                oneKeyframe.weightedMode = WeightedMode.Both;

                zeroCurve.AddKey(zeroKeyframe);
                oneCurve.AddKey(oneKeyframe);
            }

            for (int j = 1; j < num_joints; ++j)
            {
                {
                    Keyframe[] PosXCurve = zeroCurve.keys;
                    Keyframe[] PosYCurve = zeroCurve.keys;
                    Keyframe[] PosZCurve = zeroCurve.keys;
                    Keyframe[] RotXCurve = zeroCurve.keys;
                    Keyframe[] RotYCurve = zeroCurve.keys;
                    Keyframe[] RotZCurve = zeroCurve.keys;
                    Keyframe[] RotWCurve = oneCurve.keys;

                    if (handJointCurvesLeft.TryGetValue((TrackedHandJoint)j, out var curves))
                    {
                        PosXCurve = curves.GlobalPositionX.keys;
                        PosYCurve = curves.GlobalPositionY.keys;
                        PosZCurve = curves.GlobalPositionZ.keys;
                        RotXCurve = curves.GlobalRotationX.keys;
                        RotYCurve = curves.GlobalRotationY.keys;
                        RotZCurve = curves.GlobalRotationZ.keys;
                        RotWCurve = curves.GlobalRotationW.keys;
                    }

                    Debug.Assert(PosXCurve.Count() == num_data_points);
                    Debug.Assert(PosYCurve.Count() == num_data_points);
                    Debug.Assert(PosZCurve.Count() == num_data_points);
                    Debug.Assert(RotXCurve.Count() == num_data_points);
                    Debug.Assert(RotYCurve.Count() == num_data_points);
                    Debug.Assert(RotZCurve.Count() == num_data_points);
                    Debug.Assert(RotWCurve.Count() == num_data_points);

                    for (int i = 0; i < num_data_points; i++)
                    {
                        float time = PosXCurve[i].time;
                        Debug.Assert(dataPoints[i].timeStamp == time);
                        Vector3 jointPos = new Vector3(PosXCurve[i].value, PosYCurve[i].value, PosZCurve[i].value);
                        Quaternion jointRot = new Quaternion(RotXCurve[i].value, RotYCurve[i].value, RotZCurve[i].value, RotWCurve[i].value);
                        jointRot = jointRot * Recorder.RecordingFrameInverse(Handedness.Left);
                        MixedRealityPose jointPose = new MixedRealityPose(jointPos, jointRot);
                        dataPoints[i].leftHand.Add((TrackedHandJoint)j, jointPose);
                    }
                }
                {
                    Keyframe[] PosXCurve = zeroCurve.keys;
                    Keyframe[] PosYCurve = zeroCurve.keys;
                    Keyframe[] PosZCurve = zeroCurve.keys;
                    Keyframe[] RotXCurve = zeroCurve.keys;
                    Keyframe[] RotYCurve = zeroCurve.keys;
                    Keyframe[] RotZCurve = zeroCurve.keys;
                    Keyframe[] RotWCurve = oneCurve.keys;

                    if (handJointCurvesRight.TryGetValue((TrackedHandJoint)j, out var curves))
                    {
                        PosXCurve = curves.GlobalPositionX.keys;
                        PosYCurve = curves.GlobalPositionY.keys;
                        PosZCurve = curves.GlobalPositionZ.keys;
                        RotXCurve = curves.GlobalRotationX.keys;
                        RotYCurve = curves.GlobalRotationY.keys;
                        RotZCurve = curves.GlobalRotationZ.keys;
                        RotWCurve = curves.GlobalRotationW.keys;
                    }

                    Debug.Assert(PosXCurve.Count() == num_data_points);
                    Debug.Assert(PosYCurve.Count() == num_data_points);
                    Debug.Assert(PosZCurve.Count() == num_data_points);
                    Debug.Assert(RotXCurve.Count() == num_data_points);
                    Debug.Assert(RotYCurve.Count() == num_data_points);
                    Debug.Assert(RotZCurve.Count() == num_data_points);
                    Debug.Assert(RotWCurve.Count() == num_data_points);

                    for (int i = 0; i < num_data_points; i++)
                    {
                        float time = PosXCurve[i].time;
                        Debug.Assert(dataPoints[i].timeStamp == time);
                        Vector3 jointPos = new Vector3(PosXCurve[i].value, PosYCurve[i].value, PosZCurve[i].value);
                        Quaternion jointRot = new Quaternion(RotXCurve[i].value, RotYCurve[i].value, RotZCurve[i].value, RotWCurve[i].value);
                        jointRot = jointRot * Recorder.RecordingFrameInverse(Handedness.Right);
                        MixedRealityPose jointPose = new MixedRealityPose(jointPos, jointRot);
                        dataPoints[i].rightHand.Add((TrackedHandJoint)j, jointPose);
                    }
                }
            }

            // compansate for QR code anchor

            // find valid tracked data
            
            Quaternion R_right = new Quaternion(0, 0, 0, 1);
            Vector3 offset_right = new Vector3(0, 0, 0);

            if (handJointCurvesRight.TryGetValue(TrackedHandJoint.Wrist, out var wristCurve))
            {
                for (int i = 0; i < dataPoints.Count(); i++)
                {
                    if (dataPoints[i].rightIsTracked)
                        {
                            Vector3 locWristPos = new Vector3(wristCurve.PositionX.keys[i].value,
                                                                wristCurve.PositionY.keys[i].value,
                                                                wristCurve.PositionZ.keys[i].value);
                            Quaternion locWristRot = new Quaternion(wristCurve.RotationX.keys[i].value,
                                                                    wristCurve.RotationY.keys[i].value,
                                                                    wristCurve.RotationZ.keys[i].value,
                                                                    wristCurve.RotationW.keys[i].value);
                            Vector3 globWristPos = new Vector3(wristCurve.GlobalPositionX.keys[i].value,
                                                                wristCurve.GlobalPositionY.keys[i].value,
                                                                wristCurve.GlobalPositionZ.keys[i].value);
                            Quaternion globWristRot = new Quaternion(wristCurve.GlobalRotationX.keys[i].value,
                                                                    wristCurve.GlobalRotationY.keys[i].value,
                                                                    wristCurve.GlobalRotationZ.keys[i].value,
                                                                    wristCurve.GlobalRotationW.keys[i].value);
                            // R * (locPos) + offset = globPos
                            // globRot = R * locrot
                            R_right = globWristRot * Quaternion.Inverse(locWristRot);
                            offset_right = globWristPos - (R_right * locWristPos);
                            break;
                        }
                }   
            }
            Quaternion R_left = new Quaternion(0, 0, 0, 1);
            Vector3 offset_left = new Vector3(0, 0, 0);

            if (handJointCurvesLeft.TryGetValue(TrackedHandJoint.Wrist, out var wristCurveLeft))
            {
                for (int i = 0; i < dataPoints.Count(); i++)
                {
                    if (dataPoints[i].leftIsTracked)
                        {
                            Vector3 locWristPos = new Vector3(wristCurveLeft.PositionX.keys[i].value,
                                                                wristCurveLeft.PositionY.keys[i].value,
                                                                wristCurveLeft.PositionZ.keys[i].value);
                            Quaternion locWristRot = new Quaternion(wristCurveLeft.RotationX.keys[i].value,
                                                                    wristCurveLeft.RotationY.keys[i].value,
                                                                    wristCurveLeft.RotationZ.keys[i].value,
                                                                    wristCurveLeft.RotationW.keys[i].value);
                            Vector3 globWristPos = new Vector3(wristCurveLeft.GlobalPositionX.keys[i].value,
                                                                wristCurveLeft.GlobalPositionY.keys[i].value,
                                                                wristCurveLeft.GlobalPositionZ.keys[i].value);
                            Quaternion globWristRot = new Quaternion(wristCurveLeft.GlobalRotationX.keys[i].value,
                                                                    wristCurveLeft.GlobalRotationY.keys[i].value,
                                                                    wristCurveLeft.GlobalRotationZ.keys[i].value,
                                                                    wristCurveLeft.GlobalRotationW.keys[i].value);
                            // R * (locPos) + offset = globPos
                            // globRot = R * locrot
                            R_left = globWristRot * Quaternion.Inverse(locWristRot);
                            offset_left = globWristPos - (R_left * locWristPos);
                            break;
                        }
                }   
            }
            

           // Transform all joint to mach with MRTK reference system
           // localPos = R_inv * (globalPos-offset);
           // localRot = R_inv*globRot;

            Debug.Log("R_right: " + R_right.ToString());
            Debug.Log("d_right: " + offset_right.ToString());
            Debug.Log("R_left: " + R_left.ToString());
            Debug.Log("d_left: " + offset_left.ToString());

           
            for (int i = 0; i < dataPoints.Count(); i++)
            {
                if (dataPoints[i].rightIsTracked)
                {
                    for (int j = 1; j < num_joints; ++j)
                    {
                        if (dataPoints[i].rightHand.TryGetValue((TrackedHandJoint)j, out var pose))
                        {
                            Vector3 newPos = Quaternion.Inverse(R_right) * (pose.Position - offset_right);
                            Quaternion newRot = Quaternion.Inverse(R_right) * pose.Rotation;
                            MixedRealityPose newPose = new MixedRealityPose(newPos, newRot);
                            dataPoints[i].rightHand[(TrackedHandJoint)j] = newPose;
                        }
                    }
                }
                if (dataPoints[i].leftIsTracked)
                {
                    for (int j = 1; j < num_joints; ++j)
                    {
                        if (dataPoints[i].leftHand.TryGetValue((TrackedHandJoint)j, out var pose))
                        {
                            Vector3 newPos = Quaternion.Inverse(R_right) * (pose.Position - offset_left);
                            Quaternion newRot = Quaternion.Inverse(R_right) * pose.Rotation;
                            MixedRealityPose newPose = new MixedRealityPose(newPos, newRot);
                            dataPoints[i].leftHand[(TrackedHandJoint)j] = newPose;
                        }
                    }
                }
            }            

            RecordingData recordingData = new RecordingData(dataPoints);
            return recordingData;
        }

        private void UpdateSpeedEstimates()
        {
            float[] leftSpeed = UpdateSpeedEstimateHand(Handedness.Left);
            float[] rightSpeed = UpdateSpeedEstimateHand(Handedness.Right);

            Debug.Log("Checking Speed");
            string msg = "";
            for (int i = 0; i < rightSpeed.Count(); i++)
            {
                if (rightSpeed[i] == 3f)
                {
                    Debug.Log("Speed is three at: " + i.ToString());
                }
                msg += rightSpeed[i].ToString() + ", ";
            }
            Debug.Log(msg);

            if (leftSpeed.Count() != dataPoints.Count() || rightSpeed.Count() != dataPoints.Count())
            {
                throw new Exception("Mismatch in length of speed estimates and datapoints");
            }

            for (int i = 0; i < dataPoints.Count(); i++)
            {
                dataPoints[i].leftSpeed = leftSpeed[i];
                dataPoints[i].rightSpeed = rightSpeed[i];
            }
        }

        private float[] UpdateSpeedEstimateHand(Handedness handedness)
        {
            if (dataPoints.Count() < 3)
            {
                throw new Exception("To few datapoints");
            }
            float[] newSpeedEst = new float[dataPoints.Count()];
            float[] rawSpeed = new float[dataPoints.Count() - 1];

            for (int i = 0; i < rawSpeed.Count(); i++)
            {
                rawSpeed[i] = DataPoint.CalcSpeed(dataPoints[i], dataPoints[i + 1], handedness);
            }

            int half_window_size = Math.Min((dataPoints.Count() - 1) / 2, 5);
            int window_size = 2 * half_window_size + 1;

            for (int i = half_window_size; i < rawSpeed.Count() - half_window_size; i++)
            {
                float sum = 0f;
                for (int j = -half_window_size; j <= half_window_size; j++)
                {
                    sum += rawSpeed[i + j];
                }
                newSpeedEst[i] = (1f / window_size) * sum;
            }

            // Pad speed at front and rear
            for (int i = 0; i < half_window_size; i++)
            {
                newSpeedEst[i] = newSpeedEst[half_window_size];
            }
            for (int i = rawSpeed.Count() - half_window_size; i < newSpeedEst.Count(); i++)
            {
                newSpeedEst[i] = newSpeedEst[rawSpeed.Count() - half_window_size - 1];
            }
            return newSpeedEst;
        }
    }
}