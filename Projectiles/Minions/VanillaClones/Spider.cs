﻿using AmuletOfManyMinions.Projectiles.Minions.MinonBaseClasses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.Projectiles.Minions.VanillaClones
{
	public class SpiderMinionBuff : MinionBuff
	{
		public SpiderMinionBuff() : base(ProjectileType<SpiderMinion>()) { }
		public override void SetDefaults()
		{
			base.SetDefaults();
		}

	}

	public class SpiderMinionItem : MinionItem<SpiderMinionBuff, SpiderMinion>
	{
		public override string Texture => "Terraria/Item_" + ItemID.SpiderStaff;
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault(Language.GetTextValue("ItemName.SlimeStaff"));
			Tooltip.SetDefault("Summons a vampire slime to fight for you!\nIgnores 10 enemy defense");
		}

		public override void SetDefaults()
		{
			item.CloneDefaults(ItemID.SpiderStaff);
			base.SetDefaults();
		}
	}
	public class SpiderMinion : SimpleGroundBasedMinion
	{
		public override string Texture => "Terraria/Projectile_" + ProjectileID.VenomSpider;
		protected override int BuffId => BuffType<SpiderMinionBuff>();

		bool isClinging = false;
		bool onWall = false;
		float clingDistanceTolerance = 24f;
		Vector2 targetOffset = default;
		private Dictionary<GroundAnimationState, (int, int?)> frameInfo = new Dictionary<GroundAnimationState, (int, int?)>
		{
			[GroundAnimationState.FLYING] = (8, 11),
			[GroundAnimationState.JUMPING] = (0, 0),
			[GroundAnimationState.STANDING] = (0, 0),
			[GroundAnimationState.WALKING] = (0, 4),
		};

		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();
			DisplayName.SetDefault("PricklyPear");
			Main.projFrames[projectile.type] = 11;
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			projectile.width = 26;
			projectile.height = 26;
			drawOffsetX = -2;
			drawOriginOffsetY = -6;
			attackFrames = 60;
			noLOSPursuitTime = 300;
			startFlyingAtTargetHeight = 96;
			startFlyingAtTargetDist = 64;
			defaultJumpVelocity = 4;
			searchDistance = 800;
			maxJumpVelocity = 12;
		}

		protected override void IdleGroundedMovement(Vector2 vector)
		{
			if(onWall)
			{
				IdleFlyingMovement(vector);
			} else
			{
				base.IdleGroundedMovement(vector);
			}
		}

		protected override void DoGroundedMovement(Vector2 vector)
		{

			if (vector.Y < -projectile.height && Math.Abs(vector.X) < startFlyingAtTargetHeight)
			{
				gHelper.DoJump(vector);
			}
			float xInertia = gHelper.stuckInfo.overLedge && !gHelper.didJustLand && Math.Abs(projectile.velocity.X) < 2 ? 1.25f : 7;
			int xMaxSpeed = 10;
			if (vectorToTarget is null && Math.Abs(vector.X) < 8)
			{
				projectile.velocity.X = player.velocity.X;
				return;
			}
			DistanceFromGroup(ref vector);
			if (animationFrame - lastHitFrame > 10)
			{
				projectile.velocity.X = (projectile.velocity.X * (xInertia - 1) + Math.Sign(vector.X) * xMaxSpeed) / xInertia;
			}
			else
			{
				projectile.velocity.X = Math.Sign(projectile.velocity.X) * xMaxSpeed * 0.75f;
			}
		}

		public override Vector2 IdleBehavior()
		{
			Tile tile = Framing.GetTileSafely((int)projectile.Center.X / 16, (int)projectile.Center.Y / 16);
			onWall = tile.wall > 0;
			return base.IdleBehavior();
		}

		public override void TargetedMovement(Vector2 vectorToTargetPosition)
		{
			if(vectorToTargetPosition.Length() < clingDistanceTolerance)
			{
				// slowly decrease the distance that we're allowed to cling
				if(clingDistanceTolerance > 8f)
				{
					clingDistanceTolerance *= 0.99f;
				}
				onWall = true;
				isClinging = true;
				projectile.Center += vectorToTargetPosition;
				projectile.velocity = Vector2.Zero;
			} else
			{
				isClinging = false;
				clingDistanceTolerance = 24;
				int oldMaxSpeed = maxSpeed;
				maxSpeed = 10;
				base.TargetedMovement(vectorToTargetPosition);
				maxSpeed = oldMaxSpeed;
			}
		}

		public override Vector2? FindTarget()
		{
			Vector2? target = base.FindTarget();
			if (targetNPCIndex is int idx && oldTargetNpcIndex != idx)
			{
				// choose a new preferred location on the enemy to cling to
				targetOffset = new Vector2(
					Main.rand.Next(Main.npc[idx].width) - Main.npc[idx].width / 2,
					Main.rand.Next(Main.npc[idx].height) - Main.npc[idx].height / 2);
			}
			if(target is Vector2 tgt)
			{
				return tgt + targetOffset;
			} else
			{
				return null;
			}
		}

		public override void Animate(int minFrame = 0, int? maxFrame = null)
		{
			if(onWall)
			{
				base.Animate(4, 8);
				if(vectorToTarget != null && isClinging)
				{
					if(animationFrame % 60 > 30)
					{
						projectile.rotation = MathHelper.PiOver2 + MathHelper.Pi / 8 - (MathHelper.PiOver4 * (animationFrame % 60) / 60f);
					} else
					{
						projectile.rotation = MathHelper.PiOver2 - MathHelper.Pi / 8 + (MathHelper.PiOver4 * (animationFrame % 60) / 60f);
					}
				} else
				{
					projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
				}
			} else
			{
				projectile.rotation = 0;
				GroundAnimationState state = gHelper.DoGroundAnimation(frameInfo, base.Animate);
			}
			if (projectile.velocity.X > 1)
			{
				projectile.spriteDirection = -1;
			} else if (projectile.velocity.X < -1)
			{
				projectile.spriteDirection = 1;
			}

		}
	}
}