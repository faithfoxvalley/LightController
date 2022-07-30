using LightController.Config.Dmx;
using LightController.Config.Input;
using LightController.Config.Output;
using LightController.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LightController.Config
{
    public class ConfigFile
    {
        private const string FileName = "config.yml";

        public string MidiDevice
        {
            get
            {
                return midiDevice.Name;
            }

            set
            {
                if (midi.TryGetDevice(value, out MidiInput device))
                    midiDevice = device;
                else
                    midiDevice = null;
            }
        }
        public string DefaultScene { get; set; }

        [YamlMember(Alias = "Pro")]
        public ProPresenterConfig ProPresenter { get; set; }

        public DmxConfig Dmx { get; set; }

        public List<Scene> Scenes { get; set; } = new List<Scene>();


        private Scene activeScene;
        private MidiInput midiDevice;
        private MidiDeviceList midi = new MidiDeviceList();

        public ConfigFile() { }

        public async Task Init()
        {

            if (midiDevice != null)
            {
                midiDevice.NoteEvent += MidiDevice_NoteEvent;
                midiDevice.Input.Start();
            }

            await ProPresenter.AsyncInit();

            await Task.WhenAll(Scenes.Select(x => x.Init()));

            if (!string.IsNullOrWhiteSpace(DefaultScene))
            {
                Scene scene = Scenes.Find(x => x.Name == DefaultScene.Trim());
                if(scene != null)
                {
                    activeScene = scene;
                    scene.Activate();
                }
            }
        }

        private void MidiDevice_NoteEvent(MidiNote note)
        {
            Scene newScene = Scenes.Find(s => s.MidiNote == note);
            if (newScene != null)
            {
                foreach (Scene s in Scenes)
                    s.Deactivate();

                activeScene = newScene;
                newScene.Activate();
            }
        }


        public static string GetUserFileLocation()
        {
            string path = Path.Combine(MainWindow.ApplicationData, typeof(ConfigFile).Assembly.GetName().Name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return Path.Combine(path, FileName);
        }

        public static ConfigFile Load()
        {
            string file = GetUserFileLocation();
            if (File.Exists(file))
            {
                using(StreamReader reader = File.OpenText(file))
                {
                    DeserializerBuilder deserializer = new DeserializerBuilder()
                        .IgnoreFields()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance);
                    foreach (var tag in GetYamlTags())
                        deserializer.WithTagMapping(tag.Item1, tag.Item2);

                    return deserializer.Build().Deserialize<ConfigFile>(reader);
                }
            }
            ConfigFile newFile = new ConfigFile();
            newFile.Save();
            return newFile;
        }

        public void Save()
        {
            using(StreamWriter writer = File.CreateText(GetUserFileLocation()))
            {
                SerializerBuilder serializer = new SerializerBuilder()
                    .IgnoreFields()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance);
                foreach (var tag in GetYamlTags())
                    serializer.WithTagMapping(tag.Item1, tag.Item2);

                serializer.Build().Serialize(writer, this);
            }
        }

        private static IEnumerable<Tuple<TagName, Type>> GetYamlTags()
        {
            foreach(Type t in typeof(MainWindow).Assembly.GetTypes())
            {
                if(t.IsDefined(typeof(YamlTagAttribute), false))
                {
                    YamlTagAttribute attr = t.GetCustomAttribute<YamlTagAttribute>();
                    yield return new Tuple<TagName, Type>(attr.Tag, t);
                }
            }
        }
    }
}
