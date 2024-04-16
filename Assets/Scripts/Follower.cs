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
        [SerializeField]
        private Player _player;

        [SerializeField]
        private LogToUIText _debugger;

        private bool followMode = false;
        private float timeKeeper = 0f;

        [SerializeField]
        private GameObject debugger;

        public Follower() {}
        public void SetFollower(Player player, Recorder recorder)
        {
            //_recorder = recorder;
            //_player = player;
            //TextMesh txt = debugger.GetComponent<TextMesh>();
            //txt.text = "wtf";
        }
        public void playAnimation()
        {
            _debugger.logInfo("Play Animation Called");
            if (followMode)
            {
                timeKeeper += 0.3f;
            }
            else
            {
                followMode = true;
                _player.StartFollowAnimation();
            }
            //_recorder.StartRecording();
        }
        public void Update()
        {
            //TextMesh txt = debugger.GetComponent<TextMesh>();
            InputRecordingBuffer.Keyframe key;
            if (followMode)
            {
                //txt.text = "follow mode on, Time: ";
                //txt.text += timeKeeper.ToString();
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
                _player.setLocalTime(timeKeeper);
            } else
            {
                //txt.text = "Follow mode off";
            }

            
        }

    }
}