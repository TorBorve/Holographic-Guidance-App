using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Tutorials
{
    public class Follower : MonoBehaviour
    {
        //[SerializeField]
        private Recorder _recorder;
        //[SerializeField]
        private Player _player;

        private bool followMode = false;
        private float timeKeeper = 0f;

        [SerializeField]
        private GameObject debugger;

        public Follower() {}
        public void SetFollower(Player player, Recorder recorder)
        {
            _recorder = recorder;
            _player = player;
            TextMesh txt = debugger.GetComponent<TextMesh>();
            txt.text = "wtf";
        }
        public void playAnimation()
        {
            //TextMesh txt = obj.GetComponent<TextMesh>();
            //txt.text = "PlayAnimation";
            followMode = true;
            //_player.StartFollowAnimation();
            //_recorder.StartRecording();
        }
        public void Update()
        {
            TextMesh txt = debugger.GetComponent<TextMesh>();
            if (timeKeeper == 0)
            {
                timeKeeper = 2.0f;
            }
            InputRecordingBuffer.Keyframe key;
            if (followMode)
            {
                //txt.text = "follow mode on";
                txt.text += timeKeeper.ToString();
                //key = _recorder.getlatestkeyframe();
                //idictionary<trackedhandjoint, transformdata> joints = _player.getanimationbytime(timekeeper);
                //vector3 john = joints[trackedhandjoint.wrist].getposition();
                //vector3 vect = key.rightjoints[trackedhandjoint.wrist].position;

                //float distance = vector3.distance(john, vect);
                //debug.log(distance);
                //if (timekeeper == 0)
                //{
                //    timekeeper = 5.0f;
                //}
                //if (distance < 0.20)
                //{
                //    timekeeper += 0.1f;
                //}
                // Debug.Log(timeKeeper);
            } else
            {
                txt.text = "Follow mode off";
            }

            //_player.setLocalTime(timeKeeper);
        }

    }
}