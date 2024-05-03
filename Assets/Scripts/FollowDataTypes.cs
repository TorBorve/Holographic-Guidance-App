
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
        public float speedLeft;
        public Dictionary<TrackedHandJoint, MixedRealityPose> rightHand;
        public float speedRight;

        public DataPoint() : this(0f) { }

        public DataPoint(float time)
        {
            timeStamp = time;
            leftIsTracked = false;
            rightIsTracked = false;
            leftHand = new Dictionary<TrackedHandJoint, MixedRealityPose>();
            speedLeft = 1f;
            rightHand = new Dictionary<TrackedHandJoint, MixedRealityPose>();
            speedRight = 1f;
        }

        public static DataPoint Interpolate(DataPoint firstDataPoint, DataPoint secondDataPoint, float alpha)
        {
            DataPoint interpolatedDataPoint = new DataPoint(firstDataPoint.timeStamp + alpha * (secondDataPoint.timeStamp - firstDataPoint.timeStamp));

            if (!firstDataPoint.leftIsTracked)
            {
                interpolatedDataPoint.leftHand = secondDataPoint.leftHand;
                interpolatedDataPoint.leftIsTracked = secondDataPoint.leftIsTracked;
                interpolatedDataPoint.speedLeft = secondDataPoint.speedLeft;
            } else if (!secondDataPoint.leftIsTracked)
            {
                interpolatedDataPoint.leftHand = firstDataPoint.leftHand;
                interpolatedDataPoint.leftIsTracked = firstDataPoint.leftIsTracked;
                interpolatedDataPoint.speedLeft = firstDataPoint.speedLeft;
            } else
            {
                interpolatedDataPoint.leftIsTracked = true;
                interpolatedDataPoint.leftHand = InterpolateJoints(firstDataPoint.leftHand, secondDataPoint.leftHand, alpha);
                interpolatedDataPoint.speedLeft = InterpolateSpeed(firstDataPoint.speedLeft, secondDataPoint.speedLeft, alpha);
            }

            if (!firstDataPoint.rightIsTracked)
            {
                interpolatedDataPoint.rightHand = secondDataPoint.rightHand;
                interpolatedDataPoint.rightIsTracked = secondDataPoint.rightIsTracked;
                interpolatedDataPoint.speedRight = secondDataPoint.speedRight;
            } else if (!secondDataPoint.rightIsTracked)
            {
                interpolatedDataPoint.rightHand = firstDataPoint.rightHand;
                interpolatedDataPoint.rightIsTracked = firstDataPoint.rightIsTracked;
                interpolatedDataPoint.speedLeft = firstDataPoint.speedRight;
            } else
            {
                interpolatedDataPoint.rightIsTracked = true;
                interpolatedDataPoint.rightHand = InterpolateJoints(firstDataPoint.rightHand, secondDataPoint.rightHand, alpha);
                interpolatedDataPoint.speedLeft = InterpolateSpeed(firstDataPoint.speedRight, secondDataPoint.speedRight, alpha)
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

        private static float InterpolateSpeed(float firstSpeed, float secondSpeed, float alpha) {
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

        public FingerAndWristData(IMixedRealityHand hand) {
            bool success = true;
            if(hand.TryGetJoint(TrackedHandJoint.Wrist, out MixedRealityPose wristPose)){
                wristPosition = wristPose.Position;                
            } else {
                success = false;
            }
            if(hand.TryGetJoint(TrackedHandJoint.ThumbTip, out MixedRealityPose thumbPose)) {
                thumbPosition = thumbPose.Position;
            } else {
                success = false;
            }
            if(hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose indexPose)) {
                indexPosition = indexPose.Position;
            } else {
                success = false;
            }
            if(hand.TryGetJoint(TrackedHandJoint.MiddleTip, out MixedRealityPose middlePose)) {
                middlePosition = middlePose.Position;
            } else {
                success = false;
            }
            if(hand.TryGetJoint(TrackedHandJoint.RingTip, out MixedRealityPose ringPose)){
                ringPosition = ringPose.Position;
            } else {
                success = false;
            }
            if(hand.TryGetJoint(TrackedHandJoint.PinkyTip, out MixedRealityPose pinkyPose)){
                pinkyPosition = pinkyPose.Position;
            } else {
                success = false;
            }
            if (!success) {
                Debug.LogError("Could not get all joint from hand");
                throw new Exception("Could not get all joints from hand");
            }
        }
        public override string ToString() {
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
            if (weight == null) {
                weight = new float[N_dim]{1f/N_dim, 1f/N_dim, 1f/N_dim, 1f/N_dim, 1f/N_dim, 1f/N_dim};   
            }
            if (weight.Count() != N_dim) {
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
        public List<float> estimatedSpeed;

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
            for (int j = 1; j < num_joints; ++j)
            {
                { 
                    if (!handJointCurvesLeft.TryGetValue((TrackedHandJoint)j, out var curves))
                    {
                        throw new Exception("Joint Not present in data: " + (TrackedHandJoint)j);
                    }
                    Keyframe[] PosXCurve = curves.GlobalPositionX.keys;
                    Debug.Assert(PosXCurve.Count() == num_data_points);
                    Keyframe[] PosYCurve = curves.GlobalPositionY.keys;
                    Debug.Assert(PosYCurve.Count() == num_data_points);
                    Keyframe[] PosZCurve = curves.GlobalPositionZ.keys;
                    Debug.Assert(PosZCurve.Count() == num_data_points);
                    Keyframe[] RotXCurve = curves.GlobalRotationX.keys;
                    Debug.Assert(RotXCurve.Count() == num_data_points);
                    Keyframe[] RotYCurve = curves.GlobalRotationY.keys;
                    Debug.Assert(RotYCurve.Count() == num_data_points);
                    Keyframe[] RotZCurve = curves.GlobalRotationZ.keys;
                    Debug.Assert(RotZCurve.Count() == num_data_points);
                    Keyframe[] RotWCurve = curves.GlobalRotationW.keys;
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
                    Keyframe[] PosXCurve = curves.GlobalPositionX.keys;
                    Debug.Assert(PosXCurve.Count() == num_data_points);
                    Keyframe[] PosYCurve = curves.GlobalPositionY.keys;
                    Debug.Assert(PosYCurve.Count() == num_data_points);
                    Keyframe[] PosZCurve = curves.GlobalPositionZ.keys;
                    Debug.Assert(PosZCurve.Count() == num_data_points);
                    Keyframe[] RotXCurve = curves.GlobalRotationX.keys;
                    Debug.Assert(RotXCurve.Count() == num_data_points);
                    Keyframe[] RotYCurve = curves.GlobalRotationY.keys;
                    Debug.Assert(RotYCurve.Count() == num_data_points);
                    Keyframe[] RotZCurve = curves.GlobalRotationZ.keys;
                    Debug.Assert(RotZCurve.Count() == num_data_points);
                    Keyframe[] RotWCurve = curves.GlobalRotationW.keys;
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

        private void UpdateSpeedEstimate() {
            if (dataPoints.Count() < 3) {
                return;
            }
            float[] newSpeedEst = new float[dataPoints.Count()];
            float[] rawSpeed = new float[dataPoints.Count() - 1];

            for (int i = 0; i < rawSpeed.Count(); i++) {
                rawSpeed[i] = CalcSpeed(dataPoints[i], dataPoints[i+1], Handedness.Right);
            }


            int halfWindowSize = Math.Min((dataPoints.Count()-1)/2, 5);
            // speed = 1/3(rawSpeed[-1] + rawSpeed[0] + rawSpeed[1])
            float totalSpeedOverWindow = 0;
            for (int i = 0; i < 2*halfWindowSize + 1; i++) {
                // totalSpeedOverWindow += CalcSpeed();
            }
            for (int i = halfWindowSize + 1; i < dataPoints.Count() - halfWindowSize; i++) {
                totalSpeedOverWindow -= 
            }          
            
        }

        private float CalcSpeed(DataPoint point1, DataPoint point2, Handedness hand)
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

            float speed = distance / timeDifference;

            return speed;
        }
    }