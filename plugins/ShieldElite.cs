using System.Linq;
using Turbo.Plugins.Default;
using System;

namespace Turbo.Plugins.RNN
{
    public class ShieldElite : BasePlugin, IInGameWorldPainter, ICustomizer
    {
        public WorldDecoratorCollection ShieldDecorator { get; set; }
        public GroundLabelDecorator ShieldIconDecorator { get; set; }
        public bool DisableEMAffix { get; set; }

        public ShieldElite()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);
            Order = 30003;

            DisableEMAffix = true;

            // ⭐ Pulsing ground circle
            ShieldDecorator = new WorldDecoratorCollection(
                new GroundCircleDecorator(Hud)
                {
                    Brush = Hud.Render.CreateBrush(255, 0, 255, 255, 7),
                    Radius = -1,
                    HasShadow = true
                }
            );

            // ⭐ Shield icon with SAME label box style as Unpullable plugin
            ShieldIconDecorator = new GroundLabelDecorator(Hud)
            {
                TextFont = Hud.Render.CreateFont("tahoma", 7f, 255, 0, 200, 255, true, false, false),

                // Same background + border style as your Unpullable plugin
                BackgroundBrush = Hud.Render.CreateBrush(160, 0, 0, 0, 0),
                BorderBrush = Hud.Render.CreateBrush(200, 0, 200, 255, 2),

                CenterBaseLine = true,
                ForceOnScreen = true
            };
        }

        public void Customize()
        {
            if (DisableEMAffix)
                Hud.TogglePlugin<EliteMonsterAffixPlugin>(false);
        }

        public void PaintWorld(WorldLayer layer)
        {
            if (!Hud.Game.IsInGame || Hud.Game.Me.IsInTown)
                return;

            // ⭐ Smooth pulse animation
            float pulse = (float)(1.0 + 0.15 * Math.Sin(Hud.Game.CurrentGameTick / 12.0));
            float sizeMultiplier = 3.0f;

            foreach (var monster in Hud.Game.AliveMonsters)
            {
                bool shieldActive =
                    monster.AffixSnoList.Any(a => a.Affix == MonsterAffix.Shielding)
                    && monster.Invulnerable;

                if (!shieldActive)
                    continue;

                // ⭐ Pulsing radius
                float finalRadius = -1 * sizeMultiplier * pulse;

                foreach (var deco in ShieldDecorator.Decorators)
                {
                    if (deco is GroundCircleDecorator circle)
                        circle.Radius = finalRadius;
                }

                // Draw pulsing circle
                ShieldDecorator.Paint(layer, monster, monster.FloorCoordinate, null);

                // ⭐ Draw shield icon in a label box above elite
                if (layer == WorldLayer.Ground)
                {
                    var iconCoord = Hud.Window.CreateWorldCoordinate(
                        monster.FloorCoordinate.X,
                        monster.FloorCoordinate.Y,
                        monster.FloorCoordinate.Z + 6f // height offset
                    );

                    ShieldIconDecorator.Paint(monster, iconCoord, "⛊");
                }
            }
        }
    }
}
