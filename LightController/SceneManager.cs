using LightController.BacNet;
using LightController.Config;
using LightController.Dmx;
using LightController.Midi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LightController
{
    public class SceneManager
    {
        private List<Scene> scenes;
        private Scene activeScene;
        private MidiInput midiDevice;
        private DmxProcessor dmx;
        private readonly BacNetProcessor bacNet;
        private System.Windows.Controls.ListBox sceneList;
        private CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

        private MidiNote incomingNote;
        private object incomingNoteLock = new object();

        public string ActiveSceneName => activeScene?.Name;

        public SceneManager(List<Scene> scenes, string midiDevice, string defaultScene, DmxProcessor dmx, 
            double transitionTime, System.Windows.Controls.ListBox sceneList, BacNetProcessor bacNet)
        {
            this.scenes = scenes;
            this.dmx = dmx;
            this.bacNet = bacNet;
            this.sceneList = sceneList;
            if (double.IsNaN(transitionTime) || double.IsInfinity(transitionTime) || transitionTime < 0)
                transitionTime = 0;

            foreach (var scene in scenes)
                sceneList.Items.Add(scene.ToString());

            MidiDeviceList midiDevices = new MidiDeviceList();

            if (string.IsNullOrWhiteSpace(midiDevice))
            {
                while (!midiDevices.TryGetFirstDevice(out this.midiDevice))
                {
#if DEBUG
                    break;
#else
                    ErrorBox.ExitOnCancel("Midi device not found. Press OK to try again or Cancel to exit.");
                    midiDevices.UpdateMidiDeviceList();
#endif
                }

                if(this.midiDevice != null)
                {
                    this.midiDevice.NoteEvent += MidiDevice_NoteEvent;
                    this.midiDevice.Input.Start();
                }
            }
            else
            {
                while (!midiDevices.TryGetDevice(midiDevice, out this.midiDevice))
                {
                    ErrorBox.ExitOnCancel("No Midi device found with name '" + midiDevice + "', please check your config. Press OK to try again or Cancel to exit.");
                    midiDevices.UpdateMidiDeviceList();
                }

                this.midiDevice.NoteEvent += MidiDevice_NoteEvent;
                this.midiDevice.Input.Start();
            }

            foreach (Scene s in scenes)
                s.Init(transitionTime);

            if (!string.IsNullOrWhiteSpace(defaultScene))
            {
                if (TryFindScene(x => x.Name == defaultScene.Trim(), out Scene scene, out int sceneIndex))
                {
                    LogFile.Info("Activating scene " + scene.Name);
                    activeScene = scene;
                    UpdateSceneUI(sceneIndex);
                    UpdateDmx(scene, false);
                }
            }

        }

        public Task ActivateSceneAsync()
        {
            if (activeScene != null)
                return activeScene.ActivateAsync(cancelTokenSource.Token);
            return Task.CompletedTask;
        }

        public async Task UpdateAsync()
        {
            await ProcessIncomingNote();
            if (activeScene != null)
                await activeScene.UpdateAsync(cancelTokenSource.Token);
        }

        private void MidiDevice_NoteEvent(MidiNote note)
        {
            lock(incomingNoteLock)
            {
                incomingNote = note;

                cancelTokenSource.Cancel();
                cancelTokenSource = new CancellationTokenSource();
            }
        }

        private bool TryGetIncomingNote(out MidiNote note, out CancellationToken cancelToken)
        {
            lock (incomingNoteLock)
            {
                note = incomingNote;
                incomingNote = null;

                if (note == null)
                    cancelToken = CancellationToken.None;
                else
                    cancelToken = cancelTokenSource.Token;
            }

            return note != null;
        }

        private async Task ProcessIncomingNote()
        {
            while(TryGetIncomingNote(out MidiNote note, out CancellationToken cancelToken))
            {
                if (TryFindScene(s => s.MidiNote == note, out Scene newScene, out int newSceneIndex))
                {
                    LogFile.Info("Activating scene " + newScene.Name);

                    foreach (Scene s in scenes)
                        await s.DeactivateAsync(cancelTokenSource.Token);

                    await newScene.ActivateAsync(cancelTokenSource.Token, note);
                    activeScene = newScene;
                    UpdateSceneUI(newSceneIndex);
                    UpdateDmx(newScene);

                    bacNet.TriggerEvents(note, newScene.BacNetEvents);
                }
                else
                {
                    bacNet.TriggerEvents(note);
                }
            }
        }

        private void UpdateDmx(Scene scene, bool useAnimation = true)
        {
            dmx.SetInputs(scene.Inputs, useAnimation ? scene.TransitionAnimation : new Animation());
        }

        private bool TryFindScene(Func<Scene, bool> func, out Scene scene, out int index)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                Scene s = scenes[i];
                if (func(s))
                {
                    scene = s;
                    index = i;
                    return true;
                }
            }
            scene = null;
            index = -1;
            return false;
        }

        private void UpdateSceneUI(int sceneIndex)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                int count = sceneList.Items.Count;
                if (count == 0)
                    return;

                if (sceneIndex >= count)
                    sceneList.SelectedIndex = count - 1;
                else
                    sceneList.SelectedIndex = sceneIndex;
            });
        }

        public void Disable()
        {
            if(midiDevice != null)
                midiDevice.NoteEvent -= MidiDevice_NoteEvent;
        }
    }
}
