using LightController.Bacnet;
using LightController.Config;
using LightController.Config.Animation;
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
        private readonly BacnetProcessor bacNet;
        private System.Windows.Controls.ListBox sceneList;
        private CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

        private Scene incomingScene;
        private MidiNote incomingNote;
        private object incomingSceneLock = new object();

        public string ActiveSceneName => activeScene?.Name;

        public SceneManager(List<Scene> scenes, string midiDevice, string defaultScene, DmxProcessor dmx, 
            double transitionTime, System.Windows.Controls.ListBox sceneList, BacnetProcessor bacNet)
        {
            this.scenes = scenes;
            this.dmx = dmx;
            this.bacNet = bacNet;
            this.sceneList = sceneList;
            if (double.IsNaN(transitionTime) || double.IsInfinity(transitionTime) || transitionTime < 0)
                transitionTime = 0;

            foreach (Scene scene in scenes)
            {
                scene.Index = sceneList.Items.Count;
                sceneList.Items.Add(scene.ToString());
            }
            sceneList.SelectionChanged += SceneListChanged;

            MidiDeviceList midiDevices = new MidiDeviceList();

            if (string.IsNullOrWhiteSpace(midiDevice))
            {
                while (!midiDevices.TryGetAnyInput(out this.midiDevice))
                {
#if DEBUG
                    LogFile.Warn("No Midi devices found!");
                    break;
#else
                    ErrorBox.ExitOnCancel("No Midi devices found. Press OK to try again or Cancel to exit.");
                    midiDevices.UpdateMidiDeviceList();
#endif
                }
            }
            else
            {
                while (!midiDevices.TryGetInput(midiDevice, out this.midiDevice))
                {
                    ErrorBox.ExitOnCancel("No Midi device found with name '" + midiDevice + "', please check your config. Press OK to try again or Cancel to exit.");
                    midiDevices.UpdateMidiDeviceList();
                }
            }


            if (this.midiDevice != null)
            {
                this.midiDevice.NoteEvent += MidiDevice_NoteEvent;
                this.midiDevice.Start();
                LogFile.Info("Using midi device: " + this.midiDevice.Name);
            }

            foreach (Scene s in scenes)
                s.Init(transitionTime);

            if (!string.IsNullOrWhiteSpace(defaultScene))
            {
                if (TryFindScene(x => x.Name == defaultScene.Trim(), out Scene scene))
                {
                    LogFile.Info("Activating scene " + scene.Name);
                    activeScene = scene;
                    UpdateSceneUI(scene.Index);
                    UpdateDmx(scene, false);

                    bacNet.TriggerEvents(scene.MidiNote, scene.BacnetEvents);
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
            if (TryFindScene(x => x.MidiNote == note, out Scene scene))
                ActivateScene(scene, note);

            bacNet.TriggerEvents(note);
        }

        public void ActivateScene(Scene scene, MidiNote note = null)
        {
            lock (incomingSceneLock)
            {
                incomingScene = scene;
                incomingNote = note;

                cancelTokenSource.Cancel();
                cancelTokenSource = new CancellationTokenSource();
            }
        }

        private bool TryGetIncomingScene(out Scene scene, out MidiNote note)
        {
            note = null;

            lock (incomingSceneLock)
            {
                scene = incomingScene;
                if(scene != null)
                {
                    note = incomingNote;

                    incomingScene = null;
                    incomingNote = null;
                }
            }

            return scene != null;
        }

        private async Task ProcessIncomingNote()
        {
            while(TryGetIncomingScene(out Scene newScene, out MidiNote note))
            {
                int newSceneIndex = newScene.Index;
                LogFile.Info("Activating scene " + newScene.Name);

                foreach (Scene s in scenes)
                    await s.DeactivateAsync(cancelTokenSource.Token);

                await newScene.ActivateAsync(cancelTokenSource.Token, note);
                activeScene = newScene;
                UpdateSceneUI(newSceneIndex);
                UpdateDmx(newScene);

                bacNet.TriggerEvents(newScene.BacnetEvents);
            }
        }

        private void UpdateDmx(Scene scene, bool useAnimation = true)
        {
            dmx.SetInputs(scene.Inputs, useAnimation ? scene.TransitionAnimation : new TransitionAnimation());
        }

        private bool TryFindScene(Func<Scene, bool> func, out Scene scene)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                Scene s = scenes[i];
                if (func(s))
                {
                    scene = s;
                    return true;
                }
            }
            scene = null;
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
                    SetSceneListSelection(count - 1);
                else
                    SetSceneListSelection(sceneIndex);
            });
        }

        public void Disable()
        {
            if(midiDevice != null)
                midiDevice.NoteEvent -= MidiDevice_NoteEvent;
        }

        private void SetSceneListSelection(int index)
        {
            sceneList.SelectionChanged -= SceneListChanged;
            sceneList.SelectedIndex = index;
            sceneList.SelectionChanged += SceneListChanged;
        }

        private void SceneListChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int prevSelection = -1;
            if (e.RemovedItems.Count > 0)
                prevSelection = sceneList.Items.IndexOf(e.RemovedItems[0]);
            
            int currSelection = -1;
            if(e.AddedItems.Count > 0)
                currSelection = sceneList.Items.IndexOf(e.AddedItems[0]);
            if (currSelection < 0 || currSelection >= sceneList.Items.Count || currSelection == prevSelection)
                return;

            ActivateScene(scenes[currSelection]);
            if(prevSelection < sceneList.Items.Count)
                SetSceneListSelection(prevSelection);
        }
    }
}
