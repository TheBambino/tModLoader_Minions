using AmuletOfManyMinions.Dusts;
using AmuletOfManyMinions.Projectiles.Minions;
using AmuletOfManyMinions.Projectiles.Squires.SquireBaseClasses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Squires.PumpkinSquire
{
	public class PumpkinSquireMinionBuff : MinionBuff
	{
		public PumpkinSquireMinionBuff() : base(ProjectileType<PumpkinSquireMinion>()) { }
		public override void SetDefaults()
		{
			base.SetDefaults();
			DisplayName.SetDefault("Pumpkin Squire");
			Description.SetDefault("An pumpkin squire will follow your orders!");
		}
	}

	public class PumpkinSquireMinionItem : SquireMinionItem<PumpkinSquireMinionBuff, PumpkinSquireMinion>
	{
		protected override string SpecialName => "Giant Pumpkin";
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Pumpkin Crest");
			Tooltip.SetDefault("Summons a squire\nA pumpkin squire will fight for you!\nClick and hold to guide its attacks");
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			item.knockBack = 3.5f;
			item.width = 24;
			item.height = 38;
			item.damage = 17;
			item.value = Item.sellPrice(0, 0, 1, 0);
			item.rare = ItemRarityID.Blue;
		}
		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.Pumpkin, 15);
			recipe.AddRecipeGroup("AmuletOfManyMinions:EvilBars", 12);
			recipe.AddTile(TileID.Anvils);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}

	public abstract class BasePumpkinBomb : ModProjectile
	{
		protected abstract int TimeToLive { get;  }
		protected abstract int FallAfterFrames { get;  }
		protected int bounces;
		protected bool startFalling;
		protected int dustCount;
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			SquireGlobalProjectile.isSquireShot.Add(projectile.type);
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			projectile.timeLeft = TimeToLive;
			projectile.friendly = true;
			projectile.tileCollide = true;
			startFalling = false;
		}

		public override void AI()
		{
			projectile.rotation += MathHelper.Pi / 16 * Math.Sign(projectile.velocity.X);
			if (projectile.timeLeft < TimeToLive - FallAfterFrames)
			{
				startFalling = true;
			}
			if (startFalling)
			{
				if(projectile.velocity.Y < 16)
				{
					projectile.velocity.Y += 0.5f;
				}
			}
		}

		protected abstract void OnFloorBounce(int bouncesLeft, Vector2 oldVelocity);
		protected abstract void OnWallBounce(int bouncesLeft, Vector2 oldVelocity);

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (oldVelocity.Y > 0 && projectile.velocity.Y == 0)
			{
				OnFloorBounce(bounces, oldVelocity);
				bounces--;
				Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 1, 1f, Main.rand.Next(1));
			}
			if (oldVelocity.Y < 0)
			{
				startFalling = true;
			}
			if (oldVelocity.X != 0 && projectile.velocity.X == 0)
			{
				OnWallBounce(bounces, oldVelocity);
			}
			return bounces == 0;
		}

		public override void Kill(int timeLeft)
		{
			// don't explode
			Main.PlaySound(new LegacySoundStyle(4, 1).WithPitchVariance(.5f), projectile.position);
			Vector2 direction = -projectile.velocity;
			direction.Normalize();
			for (int i = 0; i < dustCount; i++)
			{
				Dust.NewDust(projectile.position, 1, 1, DustType<PumpkinDust>(), -direction.X, -direction.Y, Alpha: 255, Scale: 2);
			}
		}

	}


	public class PumpkinBomb : BasePumpkinBomb
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			projectile.width = 16;
			projectile.height = 16;
			bounces = 3;
			projectile.penetrate = 3;
			dustCount = 3;
		}
		protected override int TimeToLive => 120;

		protected override int FallAfterFrames => 15;

		protected override void OnFloorBounce(int bouncesLeft, Vector2 oldVelocity)
		{
			projectile.velocity.Y = -3 * bouncesLeft;
			// make sure not to collide right away again
			projectile.position.Y -= 8;
			projectile.velocity.X *= 0.67f;
		}

		protected override void OnWallBounce(int bouncesLeft, Vector2 oldVelocity)
		{
			projectile.velocity.X = -Math.Sign(oldVelocity.X) * 1.5f * bouncesLeft;
		}
	}

	public class BigPumpkinBomb : BasePumpkinBomb
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			projectile.width = 24;
			projectile.height = 24;
			bounces = 12;
			projectile.penetrate = 20;
			dustCount = 6;
		}
		int spawnFrames = 30;
		protected override int TimeToLive => 360;

		protected override int FallAfterFrames => spawnFrames + 15;

		public override void AI()
		{
			if(projectile.timeLeft < TimeToLive - spawnFrames)
			{
				projectile.friendly = true;
				projectile.tileCollide = true;
				projectile.ai[0] = -1;
				base.AI();
			} else
			{
				projectile.friendly = false;
				projectile.tileCollide = false;
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			float spawnFrame = Math.Min(spawnFrames, TimeToLive - projectile.timeLeft);
			float scale = MathHelper.Lerp(0.25f, 1, spawnFrame / spawnFrames);
			Texture2D texture = Main.projectileTexture[projectile.type];
			spriteBatch.Draw(texture, projectile.Center - Main.screenPosition,
				texture.Bounds, lightColor, projectile.rotation,
				texture.Bounds.Center.ToVector2(), scale, 0, 0);
			return false;
		}

		protected override void OnFloorBounce(int bouncesLeft, Vector2 oldVelocity)
		{
			projectile.velocity.Y = -Math.Max(bouncesLeft / 2f, 2f);
			// make sure not to collide right away again
			projectile.position.Y -= 2;
		}

		protected override void OnWallBounce(int bouncesLeft, Vector2 oldVelocity)
		{
			projectile.velocity.X = -Math.Sign(oldVelocity.X) * Math.Max(1.5f, bouncesLeft / 4f);
		}
	}

	public class PumpkinSquireMinion : WeaponHoldingSquire
	{
		internal override int BuffId => BuffType<PumpkinSquireMinionBuff>();
		protected override int AttackFrames => 30;
		protected override string WingTexturePath => "AmuletOfManyMinions/Projectiles/Squires/Wings/SpookyWings";
		protected override string WeaponTexturePath => null;

		protected override float IdleDistanceMulitplier => 2.5f;
		protected override WeaponAimMode aimMode => WeaponAimMode.TOWARDS_MOUSE;

		protected override WeaponSpriteOrientation spriteOrientation => WeaponSpriteOrientation.VERTICAL;

		protected override Vector2 WingOffset => new Vector2(-4, 4);

		protected override Vector2 WeaponCenterOfRotation => new Vector2(0, 4);

		protected override LegacySoundStyle attackSound => new LegacySoundStyle(2, 19);
		protected override float projectileVelocity => 8;

		protected override bool travelRangeCanBeModified => false;

		protected override int SpecialDuration => 30;

		public PumpkinSquireMinion() : base(ItemType<PumpkinSquireMinionItem>()) { }

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("Pumpkin Squire");
			// Sets the amount of frames this minion has on its spritesheet
			Main.projFrames[projectile.type] = 5;
		}

		public sealed override void SetDefaults()
		{
			base.SetDefaults();
			projectile.width = 22;
			projectile.height = 32;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			return false;
		}

		public override void StandardTargetedMovement(Vector2 vectorToTargetPosition)
		{
			base.StandardTargetedMovement(vectorToTargetPosition);
			if (attackFrame == 0 && Main.myPlayer == player.whoAmI)
			{
				Vector2 vector2Mouse = UnitVectorFromWeaponAngle();
				vector2Mouse *= 1.5f *  ModifiedProjectileVelocity();
				Projectile.NewProjectile(projectile.Center,
					vector2Mouse,
					ProjectileType<PumpkinBomb>(),
					projectile.damage,
					projectile.knockBack,
					Main.myPlayer);
			}
		}

		public override void SpecialTargetedMovement(Vector2 vectorToTargetPosition)
		{
			base.StandardTargetedMovement(vectorToTargetPosition);
			int bigPumpkinType = ProjectileType<BigPumpkinBomb>();
			Projectile bigPumpkin = Main.projectile.Where(p =>
				p.active && p.owner == player.whoAmI && p.type == bigPumpkinType && p.ai[0] == projectile.whoAmI).FirstOrDefault();
			Vector2 vector2Mouse = UnitVectorFromWeaponAngle();
			if (bigPumpkin == default && Main.myPlayer == player.whoAmI)
			{
				Projectile.NewProjectile(projectile.Center,
					Vector2.Zero,
					bigPumpkinType,
					3 * projectile.damage / 2,
					projectile.knockBack,
					Main.myPlayer,
					ai0: projectile.whoAmI);
			} else if (bigPumpkin != default && specialFrame == SpecialDuration - 1)
			{
				vector2Mouse *= 1.5f * ModifiedProjectileVelocity();
				bigPumpkin.velocity = vector2Mouse;
			} else if(bigPumpkin != default)
			{
				vector2Mouse *= 32;
				bigPumpkin.Center = projectile.Center + vector2Mouse;
			}
		}

		protected override float WeaponDistanceFromCenter() => 12;

		public override float ComputeIdleSpeed() => 9;

		public override float ComputeTargetedSpeed() => 9;

		public override float MaxDistanceFromPlayer() => 50;
	}
}
