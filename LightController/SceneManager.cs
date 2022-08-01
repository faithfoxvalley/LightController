using LightController.Config;
using LightController.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController
{
    public class SceneManager
    {
        private List<Scene> scenes;
        private Scene activeScene;
        private MidiInput midiDevice;

        public SceneManager(List<Scene> scenes, string midiDevice, string defaultScene)
        {
            this.scenes = scenes;

            MidiDeviceList midiDevices = new MidiDeviceList();

            if (string.IsNullOrWhiteSpace(midiDevice))
            {
                if(midiDevices.TryGetFirstDevice(out this.midiDevice))
                    this.midiDevice.NoteEvent += MidiDevice_NoteEvent;
            }
            else if (midiDevices.TryGetDevice(midiDevice, out this.midiDevice))
            {
                this.midiDevice.NoteEvent += MidiDevice_NoteEvent;
            }

            foreach (Scene s in scenes)
                s.Init();

            if (!string.IsNullOrWhiteSpace(defaultScene))
            {
                Scene scene = scenes.Find(x => x.Name == defaultScene.Trim());
                if (scene != null)
                {
                    activeScene = scene;
                    scene.Activate();
                }
            }

        }

        private void MidiDevice_NoteEvent(MidiNote note)
        {
            Scene newScene = scenes.Find(s => s.MidiNote == note);
            if (newScene != null)
            {
                foreach (Scene s in scenes)
                    s.Deactivate();

                activeScene = newScene;
                newScene.Activate();
            }
        }
    }
}
