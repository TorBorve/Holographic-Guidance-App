using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using TMPro;

namespace Tutorials
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField]
        private Recorder recorder;

        [SerializeField]
        private Player player;

        [SerializeField]
        private Follower follower;

        [SerializeField]
        private Transform animationSpecificPointOfReference;

        [SerializeField]
        private TextMeshPro stepNumber;

        [SerializeField]
        private TextMeshPro stepName;

        [SerializeField]
        private StepNameHandler stepNameHandler;

        [SerializeField]
        private Interactable recordButton;

        [SerializeField]
        private Interactable playStopButton;

        [SerializeField]
        private GameObject objectManagerPanel;

        [SerializeField]
        private Interactable objectManagerButton;

        [SerializeField]
        private Interactable guidanceButton;

        public TextMeshPro recordingCountdownText;

        private string countdownText = "";
        private string shouldStartRecord = "no"; // Using strings because locks don't work on bools
        private CancellationTokenSource cancelRecordingCountdownToken;

        public void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.I))
            {
                RecordAnimation();
            }
            else if (Input.GetKeyDown(KeyCode.O))
            {
                SaveAnimation();
            }
            else if (Input.GetKeyDown(KeyCode.P)) {
                CreateNewAnimationWrapper();
            }
            else if (Input.GetKeyDown(KeyCode.U))
            {
                CloseAnimation();
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                Debug.Log("Pressed K for Previous");
                Previous();
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log("Pressed L for Next");
                Next();
            }
#endif

            if (recordingCountdownText.text != countdownText)
            {
                // Checking value first because it might be more efficient than setting label text on every update
                recordingCountdownText.text = countdownText;
            }

            if (shouldStartRecord == "yes")
            {
                // Start the recording from the main thread
                shouldStartRecord = "no";
                RecordAnimation();
            }


        }

        private void Start()
        {
            // adding all the listeners to UnityEvents, C# Events and Actions
            recorder.OnRecordingStarted.AddListener(OnStartRecording);
            recorder.OnRecordingStopped.AddListener(OnStopRecording);
            FileHandler.AnimationListInstance.CurrentAnimationChanged.AddListener(OnAnimationChanged);
            OnAnimationChanged();
        }

        /// <summary>
        /// Function to handle speech recording
        /// </summary>
        public void SpeechRecord()
        {
            //return;
            if (recordButton.IsToggled)
            {
                return;
            }
            recordButton.IsToggled = true;
            RecordAction();
        }

        /// <summary>
        /// Function to handle speech save recording
        /// </summary>
        public void SpeechSave()
        {
            //return;
            if (!recordButton.IsToggled)
            {
                return;
            }
            recordButton.IsToggled = false;
            RecordAction();
        }

        /// <summary>
        /// Record UI button pressed; take correct action to start/stop
        /// </summary>
        public void RecordAction()
        {
            //return;
            // Do not record while there is an animation playing
            if (playStopButton.IsToggled)
            {
                recordButton.IsToggled = false;
                return;
            }

            // Countdown is currently running and user wants to cancel
            if (!string.IsNullOrEmpty(countdownText))
            {
                cancelRecordingCountdownToken.Cancel();
                countdownText = "";
                recordButton.IsToggled = false;
                objectManagerButton.IsToggled = true;
                objectManagerPanel.SetActive(true);
                return;
            }

            // User is done recording
            if (recorder.IsRecording)
            {
                SaveAnimation();
                // FileHandler.AnimationListInstance = null; // make sure new animation is loaded
                recordButton.IsToggled = false;
                objectManagerButton.IsToggled = true;
                objectManagerPanel.SetActive(true);
                return;   
            }
            // Countdown has not started so user is wanting to start recording
            recordButton.IsToggled = true;
            cancelRecordingCountdownToken = new CancellationTokenSource();
            Thread t = new Thread(() => StartCountdownThenRecord(cancelRecordingCountdownToken));
            t.Start();
            objectManagerButton.IsToggled = false;
            objectManagerPanel.SetActive(false);
        }

        public void StartCountdownThenRecord(CancellationTokenSource ct)
        {
            for (int i = 3; i > 0; i--)
            {
                if (ct.IsCancellationRequested)
                {
                    // User has requested to not record
                    return;
                }

                lock (countdownText)
                {
                    countdownText = i.ToString();
                }
                var canceled = ct.Token.WaitHandle.WaitOne(1000);
                if (canceled)
                {
                    return;
                }
            }

            lock (countdownText)
            {
                countdownText = "";
            }

            lock (shouldStartRecord)
            {
                // Tell the main thread to start recording
                shouldStartRecord = "yes";
            }

            cancelRecordingCountdownToken.Dispose();
        }

        /// <summary>
        /// Starts the recording if there isn't already recorded content in the currently open animation entity. 
        /// </summary>
        public void RecordAnimation()
        {
            //return;
            if (FileHandler.AnimationListInstance.CurrentNode == null)
            {
                player.Stop();
                recorder.CreateNewAnimationWrapper();
            }

            recorder.StartRecording();
        }

        /// <summary>
        /// Creates a new animation appended to the animation list
        /// </summary>
        public void CreateNewAnimationWrapper()
        {
            //return;
            if(recordButton.IsToggled)
                return;
            player.Stop();
            recorder.CreateNewAnimationWrapper();
        }

        /// <summary>
        /// Update the description of the current name through user input
        /// </summary>
        public void EditStepName()
        {
            //return;
            if(recordButton.IsToggled)
                return;
            stepNameHandler.EditSceneName();
        }

        /// <summary>
        /// Updates the step number and step name displayed in the recording panel
        /// </summary>
        private void OnAnimationChanged()
        {
            stepNumber.text = "0 / 0";
            stepName.text = "None";
            if (FileHandler.AnimationListInstance == null || FileHandler.AnimationListInstance.CurrentNode == null)
                return;
            stepNumber.text = $"{FileHandler.AnimationListInstance.CurrentIndex()} / {FileHandler.AnimationListInstance.Count}";
            if (FileHandler.AnimationListInstance.CurrentNode.Value == null)
                return;
            stepName.text = FileHandler.AnimationListInstance.CurrentNode.Value.Description;
        }

        /// <summary>
        /// Called when recording starts.
        /// </summary>
        private void OnStartRecording()
        {
        }

        /// <summary>
        /// Called when recording is stopped. 
        /// </summary>
        private void OnStopRecording()
        {
            SaveAnimation();
        }

        /// <summary>
        /// Saves the animation that has just been recorded. This activates the loading rotating orbs on the recording button to inform the user that the recording is currently being saved. 
        /// </summary>
        public void SaveAnimation()
        {
            recorder.SaveRecordedInput();
        }

        /// <summary>
        /// Closes the animation that is currently open in the editor. This animation will be removed from the animations list. 
        /// </summary>
        public void CloseAnimation()
        {
            //return;
            if(recordButton.IsToggled)
                return;
            recorder.CloseAnimation();
            if (playStopButton.IsToggled)
            {
                player.Stop();
                player.PlayCurrent();
            }
        }

        /// <summary>
        /// Cancels the recording and discards the recorded content. Note that this method is only available to the user through voice command. 
        /// </summary>
        public void Cancel()
        {
            recorder.Cancel();
        }

        /// <summary>
        /// Opens the next animation entity in the editor. If there is no successor to the currently open animation entity, this method has no effect. 
        /// </summary>
        public void Next()
        {
            if(recordButton.IsToggled)
                return;
            FileHandler.AnimationListInstance.Next();
            if (playStopButton.IsToggled)
            {
                player.Stop();
                player.PlayCurrent();
            }
        }

        /// <summary>
        /// Opens the previous animation entity in the editor. If there is no predecessor to the currently open animation entity, this method has no effect. 
        /// </summary>
        public void Previous()
        {
            if(recordButton.IsToggled)
                return;
            FileHandler.AnimationListInstance.Previous();
            if (playStopButton.IsToggled)
            {
                player.Stop();
                player.PlayCurrent();
            }
        }

        /// <summary>
        /// Resets the current animation to the start frame (100% in the loading bar).
        /// </summary>
        public void StartAgain()
        {
            player.StartAgain();
        }

        /// <summary>
        /// Function to handle speech play playback
        /// </summary>
        public void SpeechPlay()
        {
            //return;
            if (playStopButton.IsToggled)
            {
                return;
            }
            playStopButton.IsToggled = true;
            PlayAction();
        }

        /// <summary>
        /// Function to handle speech stop playback
        /// </summary>
        public void SpeechStop()
        {
            //return;
            if (!playStopButton.IsToggled)
            {
                return;
            }
            playStopButton.IsToggled = false;
            PlayAction();
        }

        /// <summary>
        /// Play/Stop the current animation when issued
        /// </summary>
        public void PlayAction()
        {
            // Do not play animation if recording
            if(recordButton.IsToggled)
            {
                playStopButton.IsToggled = false;
                return;
            }
            // Start playback
            if (playStopButton.IsToggled)
            {
                player.PlayCurrent();
            }
            // Stop if currently played back
            else
            {
                player.Stop();
            }
        }

        public void GuidanceButton()
        {
            player.setGuidanceMode(guidanceButton.IsToggled);
            follower.playAnimation();
        }

        /// <summary>
        /// Should be called when the user changes the position or rotation of the animation specific point of reference changed.
        /// </summary>
        /// <param name="animationSpecificPointOfReference">The animation specific point of reference.</param>
        public void OnAnimationSpecificPointOfReferenceChanged() 
        {
            if (FileHandler.AnimationListInstance.GetCurrentAnimationWrapper() == null) return;
            FileHandler.AnimationListInstance.GetCurrentAnimationWrapper().position_x = animationSpecificPointOfReference.transform.localPosition.x;
            FileHandler.AnimationListInstance.GetCurrentAnimationWrapper().position_y = animationSpecificPointOfReference.transform.localPosition.y;
            FileHandler.AnimationListInstance.GetCurrentAnimationWrapper().position_z = animationSpecificPointOfReference.transform.localPosition.z;

            FileHandler.AnimationListInstance.GetCurrentAnimationWrapper().rotation_x = animationSpecificPointOfReference.transform.localRotation.x;
            FileHandler.AnimationListInstance.GetCurrentAnimationWrapper().rotation_y = animationSpecificPointOfReference.transform.localRotation.y;
            FileHandler.AnimationListInstance.GetCurrentAnimationWrapper().rotation_z = animationSpecificPointOfReference.transform.localRotation.z;
            FileHandler.AnimationListInstance.GetCurrentAnimationWrapper().rotation_w = animationSpecificPointOfReference.transform.localRotation.w;
        }

    }
}