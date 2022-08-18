using LightController.Config.Input;
using LightController.Midi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LightController.Config
{
    /// <summary>
    /// Recallable set of inputs that can be activated using midi
    /// </summary>
    public class Scene
    {
        public string Name { get; set; }

        public double? TransitionTime { get; set; }

        public MidiNote MidiNote { get; set; }

        public List<InputBase> Inputs { get; set; } = new List<InputBase>();

        private bool active = false;

        //public event Action<Scene> SceneActivated;

        public Scene()
        {

        }
        
        /// <summary>
        /// Called after the scene has been created, regardless of wether it is currently active
        /// </summary>
        public void Init()
        {
            if (Inputs == null)
                Inputs = new List<InputBase>();
            foreach(InputBase input in Inputs)
                input.Init();
        }

        /// <summary>
        /// Called when the scene is activated
        /// </summary>
        /// <param name="note">The note used to activate this scene</param>
        public Task ActivateAsync(MidiNote note = null)
        {
            if (active)
                return Task.CompletedTask;

            Task[] tasks = new Task[Inputs.Count];
            for (int i = 0; i < Inputs.Count; i++)
                tasks[i] = Inputs[i].StartAsync(note);

            active = true;

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Called when the scene is deactivated
        /// </summary>
        public Task DeactivateAsync()
        {
            if (!active)
                return Task.CompletedTask;

            Task[] tasks = new Task[Inputs.Count];
            for (int i = 0; i < Inputs.Count; i++)
                tasks[i] = Inputs[i].StopAsync();

            active = false;

            return Task.WhenAll(tasks);
        }

        public Task UpdateAsync()
        {
            if (!active)
                return Task.CompletedTask;

            Task[] tasks = new Task[Inputs.Count];
            for (int i = 0; i < Inputs.Count; i++)
                tasks[i] = Inputs[i].UpdateAsync();

            return Task.WhenAll(tasks);
        }

        public override string ToString()
        {
            if (MidiNote == null)
                return Name;
            return $"{MidiNote.Channel},{MidiNote.Note} - {Name}";
        }
    }
}
