using LightController.Pro;

namespace LightController.Config.Input
{
    [YamlTag("!propresenter_input")]
    public class ProPresenterInput : InputBase
    {
        private ProPresenter pro;

        public ProPresenterInput() { }

        public ProPresenterInput(ProPresenter pro, ValueRange channels) : base(channels)
        {
            this.pro = pro;
        }
    }
}
