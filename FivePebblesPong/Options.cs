using Menu.Remix.MixedUI;
using UnityEngine;

namespace FivePebblesPong
{
    public class Options : OptionInterface
    {
        public static Configurable<bool> pacifyPebbles, hrPong;


        public Options()
        {
            pacifyPebbles = config.Bind("pacifyPebbles", defaultValue: false, info: new ConfigurableInfo("Change behavior to SlumberParty when kill-on-sight action is reached. Only works with the mark (because of limitations).\nThere may be exceptions if you don't use Downpour, but it shouldn't crash the game.", null, "", "Pacify Five Pebbles"));
            hrPong = config.Bind("hrPong", defaultValue: true, info: new ConfigurableInfo("Pebbles and Moon play Pong in Rubicon before you enter.", null, "", "HR Pong (SPOILER)"));
        }


        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[]
            {
                new OpTab(this, "Options")
            };
            AddTitle();
            AddCheckbox(pacifyPebbles, 500f);
            AddCheckbox(hrPong, 460f);
        }


        private void AddTitle()
        {
            OpLabel title = new OpLabel(new Vector2(150f, 560f), new Vector2(300f, 30f), Plugin.Name, bigText: true);
            OpLabel version = new OpLabel(new Vector2(150f, 540f), new Vector2(300f, 30f), $"Version {Plugin.Version}");

            Tabs[0].AddItems(new UIelement[]
            {
                title,
                version
            });
        }


        private void AddCheckbox(Configurable<bool> optionText, float y)
        {
            OpCheckBox checkbox = new OpCheckBox(optionText, new Vector2(220f, y))
            {
                description = optionText.info.description
            };

            OpLabel checkboxLabel = new OpLabel(220f + 40f, y + 2f, optionText.info.Tags[0] as string)
            {
                description = optionText.info.description
            };

            Tabs[0].AddItems(new UIelement[]
            {
                checkbox,
                checkboxLabel
            });
        }
    }
}
