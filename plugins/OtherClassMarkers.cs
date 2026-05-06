using Turbo.Plugins.Default;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Turbo.Plugins.RNN
{
    public class OtherClassMarkers : BasePlugin, ICustomizer, IInGameWorldPainter, INewAreaHandler
    {
        public Dictionary<HeroClass, int[]> HeroBrush { get; set; } = new Dictionary<HeroClass, int[]>();

        private Dictionary<HeroClass, WorldDecoratorCollection> DecoratorbyClass { get; set; } = new Dictionary<HeroClass, WorldDecoratorCollection>();

        private Dictionary<uint, uint> HeroIdTexture { get; set; } = new Dictionary<uint, uint>();

        private Dictionary<ActorSnoEnum, uint> ActorSnoTexture { get; set; } = new Dictionary<ActorSnoEnum, uint>
        {
            {(ActorSnoEnum) 75207, 3785199803}, {(ActorSnoEnum) 74706, 2939779782},
            {(ActorSnoEnum)  6544,   44435619}, {(ActorSnoEnum)  6526,  876580014},
            {(ActorSnoEnum)  6485, 3925954876}, {(ActorSnoEnum)  6481, 1603231623},
            {(ActorSnoEnum)  3301, 3921484788}, {(ActorSnoEnum)  3285, 1030273087},
            {(ActorSnoEnum)  4721, 2227317895}, {(ActorSnoEnum)  4717, 2918463890},
            {(ActorSnoEnum)238284, 3742271755}, {(ActorSnoEnum)238286, 3435775766},
            {(ActorSnoEnum)454021, 3285997023}, {(ActorSnoEnum)454402,  473831658}
        };

        private int MyIndex { get; set; } = -1;

        private ITexture HeroTexture { get; set; }

        private IBrush SancBrushOther { get; set; }
        private IBrush SancBrushMe { get; set; }
        private IFont TextFont { get; set; }
        private IBrush BrushDead { get; set; }

        // Shield pylon: shrinking ground circle (only on self) + countdown label
        private const float ShieldMaxDuration = 30f;
        public float ShieldCircleMaxRadius { get; set; } = 1.5f;
        private IBrush ShieldProgressBrush { get; set; }
        private IFont ShieldTimerFont { get; set; }
        private IBrush ShieldTimerBox { get; set; }

        // IP buff: lime green ground circle on self
        public float IpCircleRadius { get; set; } = 1.5f;
        private IBrush IpCircleBrush { get; set; }

        public bool NoGR { get; set; }
        public bool ShowInTown { get; set; }

        public bool MyCircle { get; set; }
        public bool CircleMapOthers { get; set; }
        public bool CircleGroundOthers { get; set; }
        public bool CircleGroundCenterOthers { get; set; }

        public bool SancIpOthers { get; set; }
        public bool MySancIP { get; set; }

        public float CircleGroundRadius { get; set; }

        public bool NamesGroundOthers { get; set; }
        public bool NamesMapOthers { get; set; }

        public bool AvatarGroundOthers { get; set; }
        public bool AvatarMapOthers { get; set; }

        public bool AvatarLeaderMapOthers { get; set; } = true;

        public OtherClassMarkers()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);
            Order = 300950;

            ShowInTown = true;
            NoGR = false;

            MyCircle = true;
            CircleMapOthers = true;
            CircleGroundOthers = true;
            CircleGroundRadius = 4f;
            CircleGroundCenterOthers = false;

            MySancIP = true;
            SancIpOthers = false;

            NamesGroundOthers = false;
            NamesMapOthers = false;

            AvatarGroundOthers = false;
            AvatarMapOthers = true;

            HeroBrush[HeroClass.Barbarian] =   new int[5] {255,237, 20, 20, 4};
            HeroBrush[HeroClass.Crusader] =    new int[5] {255,255,204,  0, 4};
            HeroBrush[HeroClass.DemonHunter] = new int[5] {255,  0,168,255, 4};
            HeroBrush[HeroClass.Monk] =        new int[5] {255, 35,225,  6, 4};
            HeroBrush[HeroClass.WitchDoctor] = new int[5] {255,255,132,  0, 4};
            HeroBrush[HeroClass.Wizard] =      new int[5] {255,220,150,255, 4};
            HeroBrush[HeroClass.Necromancer] = new int[5] {255, 64,192,176, 4};
            HeroBrush[HeroClass.None] =        new int[5] {255,255,255,255, 4};

            SancBrushOther = Hud.Render.CreateBrush(255, 255, 255, 255, 3);
            SancBrushMe = Hud.Render.CreateBrush(255, 64, 128, 255, 3);
            TextFont = Hud.Render.CreateFont("tahoma", 6, 255, 255, 255, 255, false, false, true);

            BrushDead = Hud.Render.CreateBrush(255, 255, 100, 100, -1);

            // Shield pylon progress circle: cyan
            ShieldProgressBrush = Hud.Render.CreateBrush(220, 0, 255, 255, 5f);
            ShieldTimerFont = Hud.Render.CreateFont("tahoma", 9, 255, 0, 0, 0, true, false, true);
            ShieldTimerBox = Hud.Render.CreateBrush(90, 0, 255, 255, 0);

            // IP buff ground circle: lime green
            IpCircleBrush = Hud.Render.CreateBrush(220, 50, 255, 50, 8f);

            // UI registration for leader icon
            for (var i = 0; i < 4; i++)
            {
                var text = "Root.NormalLayer.portraits.stack.party_stack.portrait_" + i + ".leaderIcon";
                Hud.Render.GetUiElement(text);
            }
        }

        public void OnNewArea(bool newGame, ISnoArea area)
        {
            if (newGame || (MyIndex != Hud.Game.Me.Index))
            {
                MyIndex = Hud.Game.Me.Index;
            }
        }

        public void Customize()
        {
            Hud.TogglePlugin<OtherPlayersPlugin>(false);
            if (AvatarMapOthers) Hud.TogglePlugin<HeadStonePlugin>(false);

            foreach (HeroClass heroClass in Enum.GetValues(typeof(HeroClass)))
            {
                if ((uint)heroClass == (uint.MaxValue - 1)) continue;

                DecoratorbyClass.Add(heroClass, new WorldDecoratorCollection());
                int[] c = HeroBrush[heroClass == HeroClass.None ? HeroClass.None : heroClass];

                addDecoratorbyClass(heroClass, c[0], c[1], c[2], c[3], c[4]);
            }
        }

        public void addDecoratorbyClass(HeroClass hero, int o, int r, int g, int b, int t)
        {
            if ((CircleGroundOthers && hero != HeroClass.None) || (MyCircle && hero == HeroClass.None))
            {
                DecoratorbyClass[hero].Decorators.Add(
                    new GroundCircleDecorator(Hud)
                    {
                        Brush = Hud.Render.CreateBrush(o, r, g, b, t),
                        Radius = CircleGroundRadius,
                    }
                );
            }

            if (hero != HeroClass.None)
            {
                if (CircleMapOthers)
                    DecoratorbyClass[hero].Decorators.Add(
                        new MapShapeDecorator(Hud)
                        {
                            Brush = Hud.Render.CreateBrush(o, r, g, b, t),
                            ShapePainter = new CircleShapePainter(Hud),
                            Radius = 3f
                        }
                    );
            }
            else if (MyCircle)
            {
                // Only the main class circle around self, no extra inner circles
            }
        }

        public void PaintWorld(WorldLayer layer)
        {
            if (layer != WorldLayer.Ground) return;
            if (!Hud.Game.IsInGame) return;
            if (NoGR && Hud.Game.Me.InGreaterRiftRank > 0) return;
            if (Hud.Game.IsInTown && !ShowInTown) return;

            var players = Hud.Game.Players.Where(p => p.CoordinateKnown);

            foreach (var pl in players)
            {
                if ((Hud.Game.Me.SnoArea.Sno != pl.SnoArea.Sno) &&
                    ((Hud.Game.Me.SnoArea.HostAreaSno == 288482) || (Hud.Game.Me.SnoArea.Sno == 288482)))
                    continue;

                if (Hud.Game.Me.IsInTown ^ pl.IsInTown) continue;

                // Colored class circles (unchanged)
                if (DecoratorbyClass.TryGetValue(pl.IsMe ? HeroClass.None : pl.HeroClassDefinition.HeroClass, out var decorator))
                {
                    decorator.Paint(layer, pl, pl.FloorCoordinate, pl.BattleTagAbovePortrait);
                }

                if (pl.IsMe)
                {
                    // Shield pylon: shrinking ground circle + countdown box, only on self
                    var shieldBuff = pl.Powers.GetBuff(Hud.Sno.SnoPowers.Generic_PagesBuffInvulnerable.Sno);
                    if (shieldBuff != null && shieldBuff.TimeLeftSeconds[0] > 0)
                    {
                        double timeLeft = shieldBuff.TimeLeftSeconds[0];

                        // Shrinking ground circle
                        float progress = (float)(timeLeft / ShieldMaxDuration);
                        float radius = ShieldCircleMaxRadius * progress;
                        if (radius > 0.1f)
                            ShieldProgressBrush.DrawWorldEllipse(radius, -1, pl.FloorCoordinate);

                        // Countdown box + text
                        string txt = $"{(int)timeLeft}s";
                        var layout = ShieldTimerFont.GetTextLayout(txt);
                        var screen = pl.FloorCoordinate.ToScreenCoordinate();
                        float x = screen.X - layout.Metrics.Width / 2;
                        float y = screen.Y - 125f;
                        ShieldTimerBox.DrawRectangle(x - 6, y - 4, layout.Metrics.Width + 12, layout.Metrics.Height + 8);
                        ShieldTimerFont.DrawText(layout, x, y);
                    }

                    // IP buff: lime green ground circle on self
                    if (MySancIP && pl.Powers.BuffIsActive(79528))
                    {
                        IpCircleBrush.DrawWorldEllipse(IpCircleRadius, -1, pl.FloorCoordinate);
                    }
                }
                else
                {
                    // Avatar on minimap + death cross + leader icon (unchanged)
                    if (!HeroIdTexture.TryGetValue(pl.HeroId, out var TextureSno))
                    {
                        if (pl.HasValidActor)
                            TextureSno = ActorSnoTexture[pl.SnoActor.Sno];
                        else
                            TextureSno = ActorSnoTexture[pl.HeroClassDefinition.MaleActorSno];

                        HeroIdTexture[pl.HeroId] = TextureSno;
                    }

                    HeroTexture = Hud.Texture.GetTexture(TextureSno);

                    if (AvatarMapOthers)
                    {
                        Hud.Render.GetMinimapCoordinates(pl.FloorCoordinate.X, pl.FloorCoordinate.Y, out float mapX, out float mapY);
                        float opacity = Hud.Game.AliveMonsters.Any(m =>
                            m.Rarity != ActorRarity.Normal &&
                            m.SummonerAcdDynamicId == 0 &&
                            m.FloorCoordinate.XYDistanceTo(pl.FloorCoordinate) < 25) ? 0.20f : 1.0f;

                        HeroTexture.Draw(mapX - HeroTexture.Width / 20, mapY - HeroTexture.Height / 17,
                            HeroTexture.Width / 10, HeroTexture.Height / 10, opacity);

                        if (pl.IsDeadSafeCheck)
                        {
                            BrushDead.DrawEllipse(mapX, mapY - HeroTexture.Height / 100, HeroTexture.Width / 25, HeroTexture.Width / 20);
                            BrushDead.DrawLine(mapX - HeroTexture.Width / 25, mapY - HeroTexture.Height / 100, mapX + HeroTexture.Width / 25, mapY - HeroTexture.Height / 100);
                            BrushDead.DrawLine(mapX, mapY - HeroTexture.Height / 20, mapX, mapY + HeroTexture.Height / 30);
                        }

                        if (AvatarLeaderMapOthers &&
                            Hud.Render.GetUiElement("Root.NormalLayer.portraits.stack.party_stack.portrait_" + pl.PortraitIndex + ".leaderIcon").Visible)
                        {
                            Hud.Texture.BuffFrameTexture.Draw(mapX - HeroTexture.Width / 20, mapY - HeroTexture.Height / 16,
                                HeroTexture.Width / 10, HeroTexture.Height / 10, 1.0f);
                        }
                    }

                    if (pl.IsOnScreen && SancIpOthers)
                    {
                        if (pl.Powers.BuffIsActive(79528))
                        {
                            var l = TextFont.GetTextLayout("Ip");
                            TextFont.DrawText(l, pl.FloorCoordinate.ToScreenCoordinate().X - l.Metrics.Width / 2,
                                pl.FloorCoordinate.ToScreenCoordinate().Y);
                        }
                    }
                }
            }
        }
    }
}