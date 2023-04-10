using LightController.Color;
using LightController.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Input
{
    [YamlTag("!animated_input")]
    public class AnimatedInput : InputBase
    {
        [YamlMember(Alias = "Colors")]
        public List<AnimatedInputFrame> Colors { get; set; }

        [YamlMember(Alias = "Loop")]
        public bool Loop { get; set; }

        private readonly List<AnimatedInputLoop> loops = new List<AnimatedInputLoop>();

        public AnimatedInput() { }

        public override void Init()
        {
            if (Colors == null)
                Colors = new List<AnimatedInputFrame>();
            loops.Add(new AnimatedInputLoop(Loop, Colors, 0)); // TODO: Add based on an animation property
        }

        public override Task StartAsync(MidiNote note)
        {
            // TODO: Test performance vs parallel.foreach
            foreach(AnimatedInputLoop loop in loops)
                loop.Reset();
            //Parallel.ForEach(loops, x => x.Reset());
            return Task.CompletedTask;
        }

        public override Task UpdateAsync()
        {
            // TODO: Test performance vs parallel.foreach
            foreach (AnimatedInputLoop loop in loops)
                loop.Update();
            //Parallel.ForEach(loops, x => x.Update());
            return Task.CompletedTask;
        }

        private AnimatedInputLoop GetLoopForFixture(int fixtureId)
        {
            return loops[0]; // TODO
        }

        public override ColorHSV GetColor(int fixtureId)
        {
            return GetLoopForFixture(fixtureId).GetColor();
        }

        public override double GetIntensity(int fixtureid, ColorHSV target)
        {
            return (intensity.Value ?? 1) * GetLoopForFixture(fixtureid).GetIntensity();

        }
    }
}
