using LightController.Config;
using LightController.Dmx;
using LightController.Midi;
using System;
using System.Collections.Generic;
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
        private System.Windows.Controls.ListBox sceneList;

        public SceneManager(List<Scene> scenes, string midiDevice, string defaultScene, DmxProcessor dmx, System.Windows.Controls.ListBox sceneList)
        {
            this.scenes = scenes;
            this.dmx = dmx;
            this.sceneList = sceneList;
            foreach (var scene in scenes)
                sceneList.Items.Add(scene.ToString());

            MidiDeviceList midiDevices = new MidiDeviceList();

            if (string.IsNullOrWhiteSpace(midiDevice))
            {
                while(!midiDevices.TryGetFirstDevice(out this.midiDevice))
                {
                    ErrorBox.ExitOnCancel("Midi device not found. Press OK to try again or Cancel to exit.");
                }

                this.midiDevice.NoteEvent += MidiDevice_NoteEvent;
                this.midiDevice.Input.Start();
            }
            else
            {
                if (midiDevices.TryGetDevice(midiDevice, out this.midiDevice))
                {
                    this.midiDevice.NoteEvent += MidiDevice_NoteEvent;
                    this.midiDevice.Input.Start();
                }
                else
                {
                    ErrorBox.Show("No Midi device found with name '" + midiDevice + "', please check your config.");
                    return;
                }
            }

            foreach (Scene s in scenes)
                s.Init();

            if (!string.IsNullOrWhiteSpace(defaultScene))
            {
                if (TryFindScene(x => x.Name == defaultScene.Trim(), out Scene scene, out int sceneIndex))
                {
                    LogFile.Info("Activating scene " + scene.Name);
                    activeScene = scene;
                    UpdateSceneUI(sceneIndex);
                    dmx.SetInputs(scene.Inputs);
                }
            }

        }

        public Task ActivateSceneAsync()
        {
            if (activeScene != null)
                return activeScene.ActivateAsync();
            return Task.CompletedTask;
        }

        public Task UpdateAsync()
        {
            if (activeScene != null)
                return activeScene.UpdateAsync();
            return Task.CompletedTask;
        }

        private async void MidiDevice_NoteEvent(MidiNote note)
        {
            if (TryFindScene(s => s.MidiNote == note, out Scene newScene, out int newSceneIndex))
            {
                LogFile.Info("Activating scene " + newScene.Name);

                foreach (Scene s in scenes)
                    await s.DeactivateAsync();

                await newScene.ActivateAsync(note);
                activeScene = newScene;
                UpdateSceneUI(newSceneIndex);
                dmx.SetInputs(newScene.Inputs);
            }
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
