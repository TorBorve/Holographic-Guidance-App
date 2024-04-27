
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Utilities;
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
        public Dictionary<TrackedHandJoint, MixedRealityPose> rightHand;

        public DataPoint() : this(0f) { }

        public DataPoint(float time)
        {
            timeStamp = time;
            leftIsTracked = false;
            rightIsTracked = false;
            leftHand = new Dictionary<TrackedHandJoint, MixedRealityPose>();
            rightHand = new Dictionary<TrackedHandJoint, MixedRealityPose>();
        }

        public static DataPoint Interpolate(DataPoint firstDataPoint, DataPoint secondDataPoint, float alpha)
        {
            DataPoint interpolatedDataPoint = new DataPoint(firstDataPoint.timeStamp + alpha * (secondDataPoint.timeStamp - firstDataPoint.timeStamp));

            if (!firstDataPoint.leftIsTracked)
            {
                interpolatedDataPoint.leftHand = secondDataPoint.leftHand;
                interpolatedDataPoint.leftIsTracked = secondDataPoint.leftIsTracked;
            } else if (!secondDataPoint.leftIsTracked)
            {
                interpolatedDataPoint.leftHand = firstDataPoint.leftHand;
                interpolatedDataPoint.leftIsTracked = firstDataPoint.leftIsTracked;
            } else
            {
                interpolatedDataPoint.leftIsTracked = true;
                interpolatedDataPoint.leftHand = InterpolateJoints(firstDataPoint.leftHand, secondDataPoint.leftHand, alpha);
            }

            if (!firstDataPoint.rightIsTracked)
            {
                interpolatedDataPoint.rightHand = secondDataPoint.rightHand;
                interpolatedDataPoint.rightIsTracked = secondDataPoint.rightIsTracked;
            } else if (!secondDataPoint.rightIsTracked)
            {
                interpolatedDataPoint.rightHand = firstDataPoint.rightHand;
                interpolatedDataPoint.rightIsTracked = firstDataPoint.rightIsTracked;
            } else
            {
                interpolatedDataPoint.rightIsTracked = true;
                interpolatedDataPoint.rightHand = InterpolateJoints(firstDataPoint.rightHand, secondDataPoint.rightHand, alpha);
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
        }

        public void AddDataPoint(DataPoint dataPoint)
        {
            if (dataPoints.Count > 0 && dataPoint.timeStamp < dataPoints[
                dataPoints.Count - 1].timeStamp)
            {
                throw new Exception("DataPoint is out of date.");
            }
            dataPoints.Add(dataPoint);
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
                } else
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
            } else if (firstTime >= time)
            {
                return dataPoints[firstIndex];
            } else if (secondTime <= time)
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
            for (int j = 1; j < num_joints; ++j)
            {
                { 
                    if (!handJointCurvesLeft.TryGetValue((TrackedHandJoint)j, out var curves))
                    {
                        throw new Exception("Joint Not present in data: " + (TrackedHandJoint)j);
                    }
                    Keyframe[] PosXCurve = curves.PositionX.keys;
                    Debug.Assert(PosXCurve.Count() == num_data_points);
                    Keyframe[] PosYCurve = curves.PositionY.keys;
                    Debug.Assert(PosYCurve.Count() == num_data_points);
                    Keyframe[] PosZCurve = curves.PositionZ.keys;
                    Debug.Assert(PosZCurve.Count() == num_data_points);
                    Keyframe[] RotXCurve = curves.RotationX.keys;
                    Debug.Assert(RotXCurve.Count() == num_data_points);
                    Keyframe[] RotYCurve = curves.RotationY.keys;
                    Debug.Assert(RotYCurve.Count() == num_data_points);
                    Keyframe[] RotZCurve = curves.RotationZ.keys;
                    Debug.Assert(RotZCurve.Count() == num_data_points);
                    Keyframe[] RotWCurve = curves.RotationW.keys;
                    Debug.Assert(RotWCurve.Count() == num_data_points);

                    for (int i = 0; i < num_data_points; i++)
                    {
                        float time = PosXCurve[i].time;
                        Debug.Assert(dataPoints[i].timeStamp == time);
                        Vector3 jointPos = new Vector3(PosXCurve[i].value, PosYCurve[i].value, PosZCurve[i].value);
                        Quaternion jointRot = new Quaternion(RotXCurve[i].value, RotYCurve[i].value, RotZCurve[i].value, RotWCurve[i].value);
                        MixedRealityPose jointPose = new MixedRealityPose(jointPos, jointRot);
                        dataPoints[i].leftHand.Add((TrackedHandJoint)j, jointPose);
                    }
                }
                {
                    if (!handJointCurvesRight.TryGetValue((TrackedHandJoint)j, out var curves))
                    {
                        throw new Exception("Joint Not present in data: " + (TrackedHandJoint)j);
                    }
                    Keyframe[] PosXCurve = curves.PositionX.keys;
                    Debug.Assert(PosXCurve.Count() == num_data_points);
                    Keyframe[] PosYCurve = curves.PositionY.keys;
                    Debug.Assert(PosYCurve.Count() == num_data_points);
                    Keyframe[] PosZCurve = curves.PositionZ.keys;
                    Debug.Assert(PosZCurve.Count() == num_data_points);
                    Keyframe[] RotXCurve = curves.RotationX.keys;
                    Debug.Assert(RotXCurve.Count() == num_data_points);
                    Keyframe[] RotYCurve = curves.RotationY.keys;
                    Debug.Assert(RotYCurve.Count() == num_data_points);
                    Keyframe[] RotZCurve = curves.RotationZ.keys;
                    Debug.Assert(RotZCurve.Count() == num_data_points);
                    Keyframe[] RotWCurve = curves.RotationW.keys;
                    Debug.Assert(RotWCurve.Count() == num_data_points);

                    for (int i = 0; i < num_data_points; i++)
                    {
                        float time = PosXCurve[i].time;
                        Debug.Assert(dataPoints[i].timeStamp == time);
                        Vector3 jointPos = new Vector3(PosXCurve[i].value, PosYCurve[i].value, PosZCurve[i].value);
                        Quaternion jointRot = new Quaternion(RotXCurve[i].value, RotYCurve[i].value, RotZCurve[i].value, RotWCurve[i].value);
                        MixedRealityPose jointPose = new MixedRealityPose(jointPos, jointRot);
                        dataPoints[i].rightHand.Add((TrackedHandJoint)j, jointPose);
                    }
                }
            }

            RecordingData recordingData = new RecordingData(dataPoints);
            return recordingData;
        }

        public static RecordingData FromRecordingBuffer(InputRecordingBuffer recordingBuffer)
        {
            RecordingData recordingData = new RecordingData();
            foreach (InputRecordingBuffer.Keyframe keyframe in recordingBuffer)
            {
                DataPoint dataPoint = DataPoint.FromKeyframe(keyframe);
                recordingData.AddDataPoint(dataPoint);
            }
            return recordingData;
        }
    }
}