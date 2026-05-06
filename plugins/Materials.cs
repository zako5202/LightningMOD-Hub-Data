using Turbo.Plugins.Default;
using System.Globalization;
using System;
using System.Collections.Generic;

namespace Turbo.Plugins.RNN
{
    public class Materials : BasePlugin, ICustomizer, IInGameTopPainter
    {
		public const string Version = "1.0.0";
		
		private float x { get; set; }
		private float y { get; set; }
		private float Space { get; set; } = 0;
		private float sizeIcon { get; set; }

		private IBrush BorderBrushRed { get; set; }
		private IFont FontDefault { get; set; }

		private delegate long AmountFunc();
		private delegate bool WarningFunc();
		private List<Tuple<ITexture, IFont, AmountFunc, WarningFunc, bool>> ListMaterials { get; set; } = new List<Tuple<ITexture, IFont, AmountFunc, WarningFunc, bool>>();

		public float Xpor { get; set; }
		public float Ypor { get; set; }
		public float SizeMultiplier { get; set; }
		public float Separation { get; set; }
		public bool ColorText  { get; set; }
		public bool WarningBS { get; set; }
		public long RemainingBS { get; set; }
		public bool WarningIS { get; set; }
		public long RemainingIS { get; set; }
		public bool OnlyInTown  { get; set; }
		public bool DisableBSIPlugins { get; set; }

		public enum MatsEnum { ReusableParts = 1, ArcaneDust = 2, VeiledCrystal = 3, DeathsBreath = 4, ForgottenSoul = 5, GreaterRiftKeystone = 6, BloodShard = 7, PrimordialAshes =8, InventorySpace = 9 }
		public MatsEnum[] MaterialsOrderEnum { get; set; }

		public int[] MaterialsOrder { get; set; } = new int[]{1,2,3,4,5,8,6,7,9}; // Deprecated, 1=ReusableParts, 2=ArcaneDust, 3=VeiledCrystal, 4=DeathsBreath, 5=ForgottenSoul, 6=GreaterRiftKeystone, 7=BloodShard, 8=primordial Ashes, 9=InventorySpace

		public Materials()
		{
			Enabled = true;
		}

		public override void Load(IController hud)
		{
			base.Load(hud);
			Order = 30001;

			Xpor = 0.567f;				// Valid values: from 0 to 1
			Ypor = 0.983f;				// Valid values: from 0 to 1
			OnlyInTown = false;			// Show only in town
			ColorText = true;			// Different colors will be used for the text or white if ColorText = false
			SizeMultiplier = 1.1f;		// Size multiplier for text and icons
			Separation = 0.5f;			// Valid values: equal to or greater than zero . Separation between one material and the next
			WarningBS = true;			// Show Rectagle Red when Remaining Blood Shard < RemainingBS
			RemainingBS = 250;			// Limit Blood Shard
			WarningIS = true;			// Show Rectagle Red when Remaining free Inventory Space < RemainingIS
			RemainingIS = 5;			// Limit free Inventory Space
			DisableBSIPlugins = true;	// Disable default plugins BloodShardPlugin and InventoryFreeSpacePlugin
			MaterialsOrderEnum = new MatsEnum[]
			{
				MatsEnum.ReusableParts, MatsEnum.ArcaneDust, MatsEnum.VeiledCrystal, MatsEnum.DeathsBreath, MatsEnum.ForgottenSoul, MatsEnum.PrimordialAshes, MatsEnum.GreaterRiftKeystone, MatsEnum.BloodShard, MatsEnum.InventorySpace
			};

			BorderBrushRed = Hud.Render.CreateBrush(255, 255, 0, 0, 1);
		}

        public void Customize()
		{
			FontDefault = Hud.Render.CreateFont("tahoma", 7 * SizeMultiplier, 255, 255, 255, 255, false, false, 255, 0, 0, 0, true);

			sizeIcon = Hud.Texture.GetItemTexture(Hud.Sno.SnoItems.Crafting_AssortedParts_01).Width * 0.26f * SizeMultiplier;

			if (DisableBSIPlugins)	//if ((Xpor > 0.5f) && (Xpor < 0.65f) && (Ypor > 0.97f))
			{
				Hud.TogglePlugin<BloodShardPlugin>(false);
				Hud.TogglePlugin<InventoryFreeSpacePlugin>(false);
			}

			SortingOut(MaterialsOrderEnum);
		}

        public void SortingOut (params MatsEnum[] m )
		{
			ListMaterials.Clear();

			WarningFunc WarningBSFunc = null; WarningFunc WarningISFunc = null;
			if (WarningBS) {  WarningBSFunc = () => ((500 + (Hud.Game.Me.HighestSoloRiftLevel * 10) - Hud.Game.Me.Materials.BloodShard) < RemainingBS); }
			if (WarningIS) {  WarningISFunc = () => ((Hud.Game.Me.InventorySpaceTotal - Hud.Game.InventorySpaceUsed) < RemainingIS); }

			for (var i = 0; i < m.Length ; i++)
			{
				switch(m[i])
				{
					case MatsEnum.ReusableParts:
							ListMaterials.Add(
								new Tuple<ITexture, IFont, AmountFunc, WarningFunc, bool>  // texture, font, amount, warning, text right
								(
									Hud.Texture.GetItemTexture(Hud.Sno.SnoItems.Crafting_AssortedParts_01),
									ColorText? Hud.Render.CreateFont("tahoma", 7 * SizeMultiplier, 255, 175, 180, 175, false, false, 255, 0, 0, 0, true) : FontDefault ,
									() => Hud.Game.Me.Materials.ReusableParts,
									null,
									true
								)	);
							break;
					case MatsEnum.ArcaneDust:
							ListMaterials.Add(
								new Tuple<ITexture, IFont, AmountFunc, WarningFunc, bool>
								(
									Hud.Texture.GetItemTexture(Hud.Sno.SnoItems.Crafting_Magic_01),
									ColorText? 	Hud.Render.CreateFont("tahoma", 7 * SizeMultiplier, 255, 102, 102, 255, false, false, 255, 0, 0, 0, true) : FontDefault ,
									() => Hud.Game.Me.Materials.ArcaneDust,
									null,
									true
								)	);
							break;
					case MatsEnum.VeiledCrystal:
							ListMaterials.Add(
								new Tuple<ITexture, IFont, AmountFunc, WarningFunc, bool>
								(
									Hud.Texture.GetItemTexture(Hud.Sno.SnoItems.Crafting_Rare_01),
									ColorText? Hud.Render.CreateFont("tahoma", 7 * SizeMultiplier, 255, 255, 255,   0, false, false, 255, 0, 0, 0, true) : FontDefault ,
									() => Hud.Game.Me.Materials.VeiledCrystal,
									null,
									true
								)	);
							break;
					case MatsEnum.DeathsBreath:
							ListMaterials.Add(
								new Tuple<ITexture, IFont, AmountFunc, WarningFunc, bool>
								(
									Hud.Texture.GetItemTexture(Hud.Sno.SnoItems.Crafting_Looted_Reagent_01),
									ColorText? Hud.Render.CreateFont("tahoma", 7 * SizeMultiplier, 255, 108, 216, 187, false, false, 255, 0, 0, 0, true) : FontDefault ,
									() => Hud.Game.Me.Materials.DeathsBreath,
									null,
									true
								)	);
							break;
					case MatsEnum.ForgottenSoul:
							ListMaterials.Add(
								new Tuple<ITexture, IFont, AmountFunc, WarningFunc, bool>
								(
									Hud.Texture.GetItemTexture(Hud.Sno.SnoItems.Crafting_Legendary_01),
									ColorText? Hud.Render.CreateFont("tahoma", 7 * SizeMultiplier, 255, 255, 128,   0, false, false, 255, 0, 0, 0, true) : FontDefault ,
									() => Hud.Game.Me.Materials.ForgottenSoul,
									null,
									true
								)	);
							break;
					case MatsEnum.GreaterRiftKeystone:
							ListMaterials.Add(
								new Tuple<ITexture, IFont, AmountFunc, WarningFunc, bool>
								(
									Hud.Texture.GetItemTexture(Hud.Sno.SnoItems.GreaterLootRunKey),
									ColorText? Hud.Render.CreateFont("tahoma", 7 * SizeMultiplier, 255, 220, 135, 220, false, false, 255, 0, 0, 0, true) : FontDefault ,
									() => Hud.Game.Me.Materials.GreaterRiftKeystone,
									null,
									true
								)	);
							break;
					case MatsEnum.BloodShard:
							ListMaterials.Add(
								new Tuple<ITexture, IFont, AmountFunc, WarningFunc, bool>
								(
									Hud.Texture.GetItemTexture(Hud.Sno.SnoItems.HoradricRelic),
									ColorText? Hud.Render.CreateFont("tahoma", 7 * SizeMultiplier, 255, 245,  75,  75, false, false, 255, 0, 0, 0, true) : FontDefault ,
									() => Hud.Game.Me.Materials.BloodShard,
									WarningBSFunc,
									true
								)	);
							break;
					case MatsEnum.PrimordialAshes:
							ListMaterials.Add(
								new Tuple<ITexture, IFont, AmountFunc, WarningFunc, bool>
								(
									Hud.Texture.GetItemTexture(Hud.Sno.SnoItems.Crafting_Legendary_Primal_01),
									ColorText? Hud.Render.CreateFont("tahoma", 7 * SizeMultiplier, 255, 255, 128,   0, false, false, 255, 0, 0, 0, true) : FontDefault ,
									() => Hud.Game.Me.Materials.PrimordialAshes,
									null,
									true
								)	);
							break;
					case MatsEnum.InventorySpace:
							ListMaterials.Add(
								new Tuple<ITexture, IFont, AmountFunc, WarningFunc, bool>
								(
									Hud.Texture.Button2TextureBrown,
									ColorText? Hud.Render.CreateFont("tahoma", 7 * SizeMultiplier, 255, 225, 135,  80, false, false, 255, 0, 0, 0, true) : FontDefault ,
									() => Hud.Game.Me.InventorySpaceTotal - Hud.Game.InventorySpaceUsed,
									WarningISFunc,
									false
								)	);
							break;
				}
			}
		}

        public string MatsToString(long value)
        {
            if 		(value < 1000)		{	return value.ToString("#,0.#", CultureInfo.InvariantCulture);	}
			else if (value < 1000000)	{	return (Math.Truncate(value/100d)/10).ToString("#,0.#K", CultureInfo.InvariantCulture);			}	//else if (value < 1000000)	{	return (value / 1000.0f).ToString("#,0.#K", CultureInfo.InvariantCulture);		}
			else 						{	return (Math.Truncate(value/100000d)/10).ToString("#,0.#M", CultureInfo.InvariantCulture);		}	//else 						{	return (value / 1000000.0f).ToString("#,0.#M", CultureInfo.InvariantCulture);	}
        }

		public void PaintTopInGame(ClipState clipState)
		{
			if (clipState != ClipState.BeforeClip) return;
			if (!Hud.Game.IsInGame)  return;
			if (OnlyInTown && !Hud.Game.IsInTown) return;

			x = Hud.Window.Size.Width * Xpor;	y = Hud.Window.Size.Height * Ypor;	Space = FontDefault.GetTextLayout("0").Metrics.Height * Separation;

			foreach(var m in ListMaterials)
			{
				m.Item1.Draw(x, y, sizeIcon, sizeIcon);
				var layout = m.Item2.GetTextLayout( MatsToString(m.Item3()) );
				x += (m.Item5) ? sizeIcon : (sizeIcon - layout.Metrics.Width)/2f;
				var yt = y + (sizeIcon - layout.Metrics.Height)/2f ;
				m.Item2.DrawText(layout, x , yt );
				if (m.Item4 != null)
				{
					if (m.Item4()) BorderBrushRed.DrawRectangle(x - 2, yt + 1, layout.Metrics.Width + 4, layout.Metrics.Height - 1);
				}
				x += layout.Metrics.Width + Space;
			}
		}
	}
}
