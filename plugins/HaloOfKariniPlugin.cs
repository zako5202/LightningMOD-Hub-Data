using Turbo.Plugins;
using Turbo.Plugins.Default;
using System.Drawing;

namespace Turbo.Plugins.RNN
{
    // INewAreaHandler added to reset state on new game
    public class HaloOfKariniPlugin : BasePlugin, IInGameTopPainter, IInGameWorldPainter, INewAreaHandler
    {
        private IFont cyanFont;
        private IFont yellowFont;
        private IFont redFont;

        private ITexture kariniIcon;
        private ITexture stormArmorIcon; // cached — no longer fetched every frame

        // Cached brushes per state — no longer created inside PaintTopInGame every frame
        private IBrush brushCyan;
        private IBrush brushYellow;
        private IBrush brushRed;
        private IBrush brushFlash;

        private int lastProcTick = 0;
        private const int KariniDuration = 150; // 5 seconds = 150 ticks

        public HaloOfKariniPlugin()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            cyanFont   = Hud.Render.CreateFont("tahoma", 15, 255, 80, 200, 255, true, false, false);
            yellowFont = Hud.Render.CreateFont("tahoma", 15, 255, 255, 200, 50, true, false, false);
            redFont    = Hud.Render.CreateFont("tahoma", 15, 255, 255, 60, 60, true, false, false);

            // Cache all brushes once at load instead of every frame
            brushCyan   = Hud.Render.CreateBrush(255, 80,  200, 255, 2);
            brushYellow = Hud.Render.CreateBrush(255, 255, 200, 50,  2);
            brushRed    = Hud.Render.CreateBrush(255, 255, 60,  60,  2);
            brushFlash  = Hud.Render.CreateBrush(255, 255, 20,  20,  2);

            // Cache icon textures at load
            kariniIcon    = Hud.Texture.GetTexture("custom/icons/haloofkarini.png");
            stormArmorIcon = Hud.Texture.GetTexture(Hud.Sno.SnoPowers.Wizard_StormArmor.NormalIconTextureId);

            if (kariniIcon == null)
                Hud.TextLog.Log("Karini", "Karini icon FAILED to load!", false, false);
        }

        // Reset lastProcTick on new game so stale ticks from previous session never show
        public void OnNewArea(bool newGame, ISnoArea area)
        {
            if (newGame)
                lastProcTick = 0;
        }

        public void PaintWorld(WorldLayer layer)
        {
            if (!Enabled) return;

            var me = Hud.Game.Me;
            if (me == null || !me.IsInGame) return;

            // No point tracking procs in town
            if (me.IsInTown) return;

            var stormArmor = me.Powers.GetBuff(Hud.Sno.SnoPowers.Wizard_StormArmor.Sno);
            if (stormArmor == null || !stormArmor.Active)
                return;

            if (stormArmor.IconCounts[1] > 0)
                lastProcTick = Hud.Game.CurrentGameTick;

            var eb = me.Powers.GetBuff(Hud.Sno.SnoPowers.Wizard_ExplosiveBlast.Sno);
            if (eb != null && eb.IconCounts[1] > 0)
                lastProcTick = Hud.Game.CurrentGameTick;
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (!Enabled || clipState != ClipState.BeforeClip)
                return;

            // Hide in town and behind blocking UI elements
            if (Hud.Game.Me.IsInTown) return;
            if (Hud.Render.UiHidden) return;
            if (Hud.Render.IsAnyBlockingUiElementVisible) return;

            if (lastProcTick == 0)
                return;

            int ticksLeft = (lastProcTick + KariniDuration) - Hud.Game.CurrentGameTick;
            if (ticksLeft <= 0)
                return;

            float secondsLeft = ticksLeft / 30f;
            string text = secondsLeft.ToString("0.0");

            float centerX = Hud.Window.Size.Width * 0.5f;
            float centerY = Hud.Window.Size.Height * 0.70f;

            float radius    = 34f;
            float thickness = 5f;
            float progress  = secondsLeft / 5f;

            // Pick cached brush and font based on time remaining
            IBrush ringBrush;
            IFont timerFont;

            if (secondsLeft <= 1f && (Hud.Game.CurrentGameTick % 20 < 10))
            {
                ringBrush = brushFlash;
                timerFont = redFont;
            }
            else if (secondsLeft > 3f)
            {
                ringBrush = brushCyan;
                timerFont = cyanFont;
            }
            else if (secondsLeft > 1f)
            {
                ringBrush = brushYellow;
                timerFont = yellowFont;
            }
            else
            {
                ringBrush = brushRed;
                timerFont = redFont;
            }

            // Progress ring — starts at 12 o'clock (subtract π/2) and sweeps clockwise
            int segments  = 60;
            float angleStep = (float)(2 * System.Math.PI / segments);
            float endAngle  = (float)(2 * System.Math.PI * progress);
            float startOffset = -(float)(System.Math.PI / 2); // 12 o'clock

            for (int i = 0; i < segments; i++)
            {
                float angle = startOffset + i * angleStep;
                if (i * angleStep > endAngle) break;

                float x1 = centerX + (float)System.Math.Cos(angle) * radius;
                float y1 = centerY + (float)System.Math.Sin(angle) * radius;
                float x2 = centerX + (float)System.Math.Cos(angle) * (radius - thickness);
                float y2 = centerY + (float)System.Math.Sin(angle) * (radius - thickness);

                ringBrush.DrawLine(x1, y1, x2, y2);
            }

            // Timer text centered in ring
            var layout = timerFont.GetTextLayout(text);
            timerFont.DrawText(layout,
                centerX - layout.Metrics.Width  / 2f,
                centerY - layout.Metrics.Height / 2f);

            // Halo of Karini icon — left side
            if (kariniIcon != null)
            {
                float iconSize = 43f;
                kariniIcon.Draw(centerX - radius - iconSize - 16f, centerY - iconSize / 2f, iconSize, iconSize);
            }

            // Storm Armor icon — right side (cached, not fetched every frame)
            if (stormArmorIcon != null)
            {
                float iconSize = 43f;
                stormArmorIcon.Draw(centerX + radius + 16f, centerY - iconSize / 2f, iconSize, iconSize);
            }
        }
    }
}