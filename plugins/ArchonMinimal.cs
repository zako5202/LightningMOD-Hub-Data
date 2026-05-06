using Turbo.Plugins.Default;
using System.Text;

namespace Turbo.Plugins.Zy
{
    public class ArchonMinimal : BasePlugin, IInGameTopPainter
    {
        public const string Version = "1.0.0";
        
        private IBrush BarBrushInside;
        private IBrush BarBrushOutside;
        private IBrush BarBrushEmpty;
        private IBrush BarBorderBrush;
        private IBrush BackgroundBrush;
        private IFont BarCountdownFont;

        private IFont FontHA;      // purple
        private IFont FontF;       // red
        private IFont FontSec;     // orange
        private IFont FontStacks;  // green
        private IFont FontSep;     // white

        private readonly int[] _skillOrder = { 2, 3, 4, 5, 0, 1 };
        private int lastStacks = 0;

        public ArchonMinimal()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            BackgroundBrush = Hud.Render.CreateBrush(180, 0, 0, 0, 0);
            BackgroundBrush.Opacity = 0.35f;

            BarBrushInside  = Hud.Render.CreateBrush(100, 150, 80, 255, 0);
            BarBrushOutside = Hud.Render.CreateBrush(100, 255, 140, 0, 0);
            BarBrushEmpty   = Hud.Render.CreateBrush(120, 0, 0, 0, 0);      // dark empty portion, always visible
            BarBorderBrush  = Hud.Render.CreateBrush(180, 200, 200, 200, 1); // subtle grey border around bar
            BarCountdownFont = Hud.Render.CreateFont("tahoma", 7, 220, 255, 255, 255, true, false, true); // countdown inside bar

            FontHA     = Hud.Render.CreateFont("tahoma", 11, 255, 255, 0, 255, false, false, true);
            FontF      = Hud.Render.CreateFont("tahoma", 11, 255, 255, 0, 0, false, false, true);
            FontSec    = Hud.Render.CreateFont("tahoma", 11, 255, 255, 165, 0, false, false, true);
            FontStacks = Hud.Render.CreateFont("tahoma", 11, 255, 0, 255, 0, false, false, true);
            FontSep    = Hud.Render.CreateFont("tahoma", 11, 255, 255, 255, 255, false, false, true);
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (Hud.Render.UiHidden || Hud.Game.Me.IsInTown)
                return;

            // ⭐ LightningMOD: Hide overlay when Profile, Leaderboard, Social, Settings, Achievements, Collections, etc. are open
            if (Hud.Render.IsAnyBlockingUiElementVisible)
                return;

            // === MAXIMUM COVERAGE HIDE LOGIC ===
            bool hideOverlay = false;

            // Known working paths
            if (Hud.Render.GetUiElement("Root.NormalLayer.inventory_dialog_mainPage")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.map_dialog_mainPage")?.Visible == true) hideOverlay = true;

            // Act Map / Sanctuary + M key
            if (Hud.Render.GetUiElement("Root.NormalLayer.map_dialog_act")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.act_map")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.map_act")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.sanctuary_map")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.act_select")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.map_dialog_sanctuary")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.act_map_main")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.map_act_select")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.act_map_dialog")?.Visible == true) hideOverlay = true;

            // Skills (S key)
            if (Hud.Render.GetUiElement("Root.NormalLayer.skills_dialog_mainPage")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.skills_dialog")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.skill_selection")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.skill_tree")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.skills_dialog_main")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.skills")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.skill")?.Visible == true) hideOverlay = true;

            // Broad fallback paths
            if (Hud.Render.GetUiElement("Root.NormalLayer.map")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.map_container")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.map_dialog")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.act")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.sanctuary")?.Visible == true) hideOverlay = true;
            if (Hud.Render.GetUiElement("Root.NormalLayer.dialog")?.Visible == true) hideOverlay = true;

            if (hideOverlay)
                return;

            // ====================== Archon Overlay ======================
            IPlayer wiz = null;
            foreach (var player in Hud.Game.Players)
            {
                if (!player.HasValidActor || player.HeroClassDefinition.HeroClass != HeroClass.Wizard) continue;

                foreach (var skill in player.Powers.SkillSlots)
                {
                    if (skill != null && skill.SnoPower.Sno == 134872)
                    {
                        wiz = player;
                        break;
                    }
                }
                if (wiz != null) break;
            }

            if (wiz == null) return;

            float x = Hud.Window.Size.Width * 0.42f;
            float y = Hud.Window.Size.Height * 0.25f;
            float w = Hud.Window.Size.Width * 0.16f;
            float h = Hud.Window.Size.Height * 0.018f;

            double ATleft = 0;
            int stacks = lastStacks;
            bool inArchon = false;

            var buff = wiz.Powers.GetBuff(Hud.Sno.SnoPowers.Wizard_Archon.Sno);
            if (buff != null)
            {
                int liveStacks = buff.IconCounts[2];
                int persistentStacks = buff.IconCounts[5];
                stacks = liveStacks > 0 ? liveStacks : persistentStacks;
                lastStacks = stacks;

                if (buff.TimeLeftSeconds[2] > 0)
                {
                    inArchon = true;
                    ATleft = buff.TimeLeftSeconds[2];
                }
                else
                {
                    if (buff.TimeLeftSeconds.Length > 6 && buff.TimeLeftSeconds[6] > 0)
                        ATleft = buff.TimeLeftSeconds[6];
                    else if (buff.TimeLeftSeconds.Length > 5 && buff.TimeLeftSeconds[5] > 0)
                        ATleft = buff.TimeLeftSeconds[5];
                }
            }

            if (ATleft < 0) ATleft = 0;

            BackgroundBrush.DrawRectangle(x, y, w, h);

            // Empty bar background — always shows the full bar shape on any ground texture
            BarBrushEmpty.DrawRectangle(x, y, w, h);

            float pct = (float)(ATleft / 20.0);
            if (inArchon)
                BarBrushInside.DrawRectangle(x, y, w * pct, h);
            else
                BarBrushOutside.DrawRectangle(x, y, w * pct, h);

            // Border on top so it reads clearly on bright areas
            BarBorderBrush.DrawRectangle(x, y, w, h);

            // Countdown number centered inside the bar
            if (ATleft > 0)
            {
                string countdown = ((int)ATleft).ToString();
                var countdownLayout = BarCountdownFont.GetTextLayout(countdown);
                float cx = x + (w - countdownLayout.Metrics.Width) / 2f;
                float cy = y + (h - countdownLayout.Metrics.Height) / 2f;
                BarCountdownFont.DrawText(countdownLayout, cx, cy);
            }

            // CoE
            var coe = wiz.Powers.GetBuff(430674);
            int arcaneCD = -1;
            int fireCD = -1;

            if (coe != null)
            {
                int activeIcon = -1;
                double activeTime = 0;
                for (int icon = 1; icon <= 7; icon++)
                {
                    double t = coe.TimeLeftSeconds[icon];
                    if (t > 0)
                    {
                        activeIcon = icon;
                        activeTime = t;
                        break;
                    }
                }

                if (activeIcon != -1)
                {
                    if (activeIcon == 1)
                    {
                        arcaneCD = (int)(activeTime - 2);
                        if (arcaneCD < 0) arcaneCD = 0;
                    }
                    else
                    {
                        int baseOffset = 0;
                        switch (activeIcon)
                        {
                            case 2: baseOffset = 12; break;
                            case 3: baseOffset = 8; break;
                            case 5: baseOffset = 4; break;
                        }
                        arcaneCD = baseOffset - (int)(4 - activeTime);
                        if (arcaneCD < 0) arcaneCD += 16;
                    }

                    switch (activeIcon)
                    {
                        case 1: fireCD = 8 - (int)(4 - activeTime); break;
                        case 2: fireCD = 4 - (int)(4 - activeTime); break;
                        case 3: fireCD = 0; break;
                        case 5: fireCD = 12 - (int)(4 - activeTime); break;
                    }
                    if (fireCD < 0) fireCD += 16;
                }
            }

            // Text
            string haText = $"HA {arcaneCD}s";
            string fText = $"  F {fireCD}s";
            string stacksText = $"  {stacks} Stacks";

            var haLayout     = FontHA.GetTextLayout(haText);
            var fLayout      = FontF.GetTextLayout(fText);
            var stacksLayout = FontStacks.GetTextLayout(stacksText);
            var sepLayout    = FontSep.GetTextLayout("  |  ");

            float totalWidth = haLayout.Metrics.Width + sepLayout.Metrics.Width +
                               fLayout.Metrics.Width + sepLayout.Metrics.Width +
                               stacksLayout.Metrics.Width;

            float startX = x + w * 0.5f - totalWidth * 0.5f;
            float posY = y - haLayout.Metrics.Height - 2f;

            FontHA.DrawText(haLayout, startX, posY); startX += haLayout.Metrics.Width;
            FontSep.DrawText(sepLayout, startX, posY); startX += sepLayout.Metrics.Width;
            FontF.DrawText(fLayout, startX, posY); startX += fLayout.Metrics.Width;
            FontSep.DrawText(sepLayout, startX, posY); startX += sepLayout.Metrics.Width;
            FontStacks.DrawText(stacksLayout, startX, posY);
        }
    }
}