﻿using LightController.Config.Animation;
using LightController.Config.Input;
using LightController.Midi;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config
{
    /// <summary>
    /// Recallable set of inputs that can be activated using midi
    /// </summary>
    public class Scene
    {
        [YamlMember]
        public string Name { get; set; }

        [YamlMember]
        public double? TransitionTime { get; set; }

        [YamlMember(Alias = "Animation")]
        public string AnimationValue { get; set; }

        [YamlIgnore]
        public TransitionAnimation TransitionAnimation { get; private set; }

        [YamlMember]
        public MidiNote MidiNote { get; set; }

        [YamlMember]
        public List<string> BacnetEvents { get; set; } = new List<string>();

        [YamlMember]
        public List<InputBase> Inputs { get; set; } = new List<InputBase>();

        [YamlIgnore]
        public int Index { get; set; }

        private bool active = false;

        //public event Action<Scene> SceneActivated;

        public Scene()
        {

        }
        
        /// <summary>
        /// Called after the scene has been created, regardless of whether it is currently active
        /// </summary>
        public void Init(double defaultTransitionTime)
        {
            if (Inputs == null)
                Inputs = new List<InputBase>();
            foreach(InputBase input in Inputs)
                input.Init();
            double transitionTime = TransitionTime ?? defaultTransitionTime;
            TransitionAnimation = new TransitionAnimation(transitionTime, new AnimationOrder(AnimationValue));
        }

        /// <summary>
        /// Called when the scene is activated
        /// </summary>
        /// <param name="note">The note used to activate this scene</param>
        public Task ActivateAsync(CancellationToken cancelToken, MidiNote note = null)
        {
            if (active)
                return Task.CompletedTask;

            Task[] tasks = new Task[Inputs.Count];
            for (int i = 0; i < Inputs.Count; i++)
                tasks[i] = Inputs[i].StartAsync(note, cancelToken);

            active = true;

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Called when the scene is deactivated
        /// </summary>
        public Task DeactivateAsync(CancellationToken cancelToken)
        {
            if (!active)
                return Task.CompletedTask;

            Task[] tasks = new Task[Inputs.Count];
            for (int i = 0; i < Inputs.Count; i++)
                tasks[i] = Inputs[i].StopAsync(cancelToken);

            active = false;

            return Task.WhenAll(tasks);
        }

        public Task UpdateAsync(CancellationToken cancelToken)
        {
            if (!active)
                return Task.CompletedTask;

            Task[] tasks = new Task[Inputs.Count];
            for (int i = 0; i < Inputs.Count; i++)
                tasks[i] = Inputs[i].UpdateAsync(cancelToken);

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
