using LightController.Config.Input;
using LightController.Midi;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightController.Config
{
    /// <summary>
    /// Recallable set of inputs that can be activated using midi
    /// </summary>
    public class Scene
    {
        public string Name { get; set; }

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
            foreach(InputBase input in Inputs)
                input.Init();
        }


        /// <summary>
        /// Called when the scene is activated
        /// </summary>
        public void Activate()
        {
            if(active)
                return;

            //if (SceneActivated != null)
            //    SceneActivated.Invoke(this);

            foreach (InputBase input in Inputs)
                input.Start();

            active = true;
        }

        /// <summary>
        /// Called when the scene is deactivated
        /// </summary>
        public void Deactivate()
        {
            if (!active)
                return;

            foreach (InputBase input in Inputs)
                input.Stop();

            active = false;
        }
    }
}
