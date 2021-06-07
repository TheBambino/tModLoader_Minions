﻿using AmuletOfManyMinions.Projectiles.Minions.MinonBaseClasses;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace AmuletOfManyMinions.Projectiles.Minions.TerrarianEnt
{
	public abstract class CompositeSpriteBatchDrawer
	{
		// these need to be injected internally each frame, a bit annoying
		internal int animationFrame;
		internal int spawnFrames;

		// This is technically specific to TileDrawer, but referenced by enough
		// other subclasses
		// Pixels from center of projectile to top of "the ground"
		internal readonly int TileTop = 16;


		internal virtual void Update(Projectile proj, int animationFrame, int spawnFrames)
		{
			this.animationFrame = animationFrame;
			this.spawnFrames = spawnFrames;
		}

		internal abstract void Draw(SpriteCompositionHelper helper, int frame, float cycleAngle);
	}

	public class TileDrawer : CompositeSpriteBatchDrawer
	{
		internal Texture2D groundTexture;
		internal int dustId = -1;

		public TileDrawer(Texture2D groundTexture, int dustId = -1)
		{
			this.groundTexture = groundTexture;
			this.dustId = dustId;
		}
		
		private (byte, byte)?[,,] groundTiles =
		{
			{
				{ null, null, null, null},
				{ null, (9,0), (12, 0), null},
				{ null, null, null, null},
			},

			{
				{ (7, 0), null, null, null},
				{ (7, 16), (7,4), (12, 0), null},
				{ null, null, null, null},
			},

			{
				{ (0, 3), (1, 0), (1, 3), null},
				{ (0, 4), (1,2), (1, 4), null},
				{ null, null, null, null},
			},
			{
				{ (0, 3), (1, 0), (2, 0), (1, 3) },
				{ (0, 4), (1, 2), (2, 2), (1, 4) },
				{ null, null, null, null},
			},
			{
				{ (0, 3), (1, 0), (2, 0), (1, 3) },
				{ (0, 4), (1, 1), (2, 1), (1, 4) },
				{ null, (0, 4), (1, 4), null},
			},
		};

		internal override void Update(Projectile proj, int animationFrame, int spawnFrames)
		{
			base.Update(proj, animationFrame, spawnFrames);
			if (dustId > - 1 && animationFrame <= spawnFrames && animationFrame > 0 && animationFrame % (spawnFrames / 4) == 0)
			{
				for (int i = 0; i < 3; i++)
				{
					Dust.NewDust(proj.TopLeft, 24, 24, dustId, 0, 0);
				}
			}
		}
		internal override void Draw(SpriteCompositionHelper helper, int frame, float cycleAngle)
		{
			int tileFrame = animationFrame > spawnFrames ? 4 : animationFrame / (spawnFrames / 4);
			helper.AddTileSpritesToBatch(groundTexture, tileFrame, groundTiles, Vector2.Zero, 0);
		}


	}
	public class ClutterDrawer : CompositeSpriteBatchDrawer
	{
		internal int[] clutterFrames;
		internal Texture2D clutterTexture;
		internal int clutterWidth = 16;
		internal int clutterHeight = 16;

		public ClutterDrawer(Texture2D texture, int[] clutterFrames, int width = 16, int height = 16)
		{
			this.clutterTexture = texture;
			this.clutterFrames = clutterFrames;
			this.clutterWidth = width;
			this.clutterHeight = height;
		}
		internal override void Draw(SpriteCompositionHelper helper, int frame, float cycleAngle)
		{
			if (animationFrame < spawnFrames)
			{
				return;
			}
			for (int i = 0; i < clutterFrames.Length; i++)
			{
				// TODO figure this out programatically
				int maxHeight = 30;
				int yOffset = Math.Min(maxHeight, 2 * (animationFrame - spawnFrames - 4 * i));
				if (yOffset < 0 || clutterFrames[i] == -1)
				{
					continue;
				}
				Vector2 offset = new Vector2(-24 + 16 * i, -yOffset);
				int idx = clutterFrames[i];
				Rectangle bounds = new Rectangle((clutterWidth + 2) * idx, 0, clutterWidth, clutterHeight);
				helper.AddSpriteToBatch(clutterTexture, bounds, offset, 0, 1);
			}
		}
	}

	public class MonumentDrawer : CompositeSpriteBatchDrawer
	{
		internal Rectangle monumentBounds;
		internal Texture2D monumentTexture;
		internal int growthRate = 4;
		internal (byte, byte)?[,,] tiles;

		public MonumentDrawer(Texture2D texture, Rectangle bounds, int growthRate = 4)
		{
			monumentTexture = texture;
			monumentBounds = bounds;
			this.growthRate = growthRate;
			tiles = new (byte, byte)?[1, monumentBounds.Height, monumentBounds.Width];
		}
		internal override void Draw(SpriteCompositionHelper helper, int frame, float cycleAngle)
		{
			if (animationFrame < spawnFrames)
			{
				return;
			}
			
			int heightToDraw = (int)Math.Min(monumentBounds.Height - 1, (animationFrame - spawnFrames - growthRate) * growthRate / 16f);
			int maxHeight = TileTop + 8 * monumentBounds.Height;
			int heightOffset = Math.Min(maxHeight, growthRate * (animationFrame - spawnFrames) - 32);
			// programmatic build here feels a bit iffy
			for(int j = 0; j < monumentBounds.Width; j++)
			{
				tiles[0, heightToDraw, j] = ((byte)(monumentBounds.X + j), (byte)(monumentBounds.Y + heightToDraw));
			}
			helper.AddTileSpritesToBatch(monumentTexture, 0, tiles, Vector2.UnitY * -heightOffset, 0);
		}
	}

	public class TreeDrawer : CompositeSpriteBatchDrawer
	{
		internal Rectangle folliageBounds;
		internal Texture2D folliageTexture;
		internal Texture2D woodTexture;
		internal Texture2D branchesTexture;
		// in tiles, not pixels
		internal int trunkHeight = 5;
		internal int folliageSpawnFrames = 20;
		// config for roots and branches orientations
		internal int rootsConfig = 0;
		internal int branchesConfig = 0;
		int treeTileSize = 20;
		int branchFrames = 3;
		internal (byte, byte)?[,,] tiles;

		// Used to draw trunk itself, branches, and roots
		private int trunkAnimFrame = -1;
		private int trunkTileRowToDraw;
		private int trunkHeightOffset;
		internal int trunkHeadstartFrames;

		internal virtual (byte, byte) TrunkTileForLocation(int row) => (0, (byte)(row % 3));

		internal override void Update(Projectile proj, int animationFrame, int spawnFrames)
		{
			base.Update(proj, animationFrame, spawnFrames);
			// the positioning info for the tree roots is shared across multiple methods, so only
			// compute it once
			int startFrame = spawnFrames + folliageSpawnFrames - trunkHeadstartFrames;
			// extremely hacky, smaller trunks lag behind a bit
			if(trunkHeight < 4)
			{
				startFrame -= 2;
			}
			if (animationFrame < startFrame)
			{
				return;
			}
			trunkAnimFrame = animationFrame - startFrame;
			int heightPerFrame = folliageBounds.Height / folliageSpawnFrames;
			trunkTileRowToDraw = (int)Math.Min(trunkHeight-1, trunkAnimFrame * heightPerFrame / (float)treeTileSize);
			int maxHeight = TileTop + treeTileSize / 2 * trunkHeight;
			trunkHeightOffset = Math.Min(maxHeight, heightPerFrame * trunkAnimFrame - treeTileSize);
		}

		public TreeDrawer(
			Texture2D folliageTexture, 
			Texture2D woodTexture, 
			Texture2D branchesTexture, 
			Rectangle folliageBounds, 
			int trunkHeight = 4, 
			int decorationConfig = -1,
			int branchFrames = 3,
			int trunkHeadstartFrames = 0)
		{
			this.folliageTexture = folliageTexture;
			this.woodTexture = woodTexture;
			this.branchesTexture = branchesTexture;
			this.folliageBounds = folliageBounds;
			this.trunkHeight = trunkHeight;
			this.trunkHeadstartFrames = trunkHeadstartFrames;
			this.branchFrames = branchFrames;
			decorationConfig = decorationConfig > -1 ? decorationConfig : Main.rand.Next(12);
			rootsConfig = decorationConfig % 3;
			branchesConfig = decorationConfig % 4;

			tiles = new (byte, byte)?[1, trunkHeight, 3];
		}

		internal void DrawTreeTop(SpriteCompositionHelper helper, int frame, float cycleAngle)
		{
			int heightPerFrame = folliageBounds.Height / folliageSpawnFrames;
			int animFrame = animationFrame - spawnFrames;
			int heightToDraw = Math.Min(folliageBounds.Height, animFrame * 2 * heightPerFrame);

			float scale = Math.Max(0.1f, Math.Min(1, animFrame / (float) folliageSpawnFrames));
			int maxHeight = TileTop + treeTileSize * trunkHeight + folliageBounds.Height/2;
			int heightOffset = Math.Min(maxHeight, heightPerFrame * animFrame - treeTileSize);
			Rectangle bounds = new Rectangle(folliageBounds.X, folliageBounds.Y, folliageBounds.Width, heightToDraw);
			helper.AddSpriteToBatch(folliageTexture, bounds, new Vector2(1, 2 - heightOffset) * scale, 0, scale);
		}

		internal void DrawTrunk(SpriteCompositionHelper helper, int frame, float cycleAngle)
		{
			if(trunkAnimFrame < 0)
			{
				return;
			}
			// bottom of tree configs
			if (trunkTileRowToDraw == trunkHeight - 1 && rootsConfig != 1)
			{
				// tile where roots connect
				tiles[0, trunkTileRowToDraw, 1] = ((byte)(rootsConfig == 0 ? 0 : 3), (byte)(6 + trunkTileRowToDraw % 3));
			} else
			{
				// rest of trunk
				tiles[0, trunkTileRowToDraw, 1] = TrunkTileForLocation(trunkTileRowToDraw);
			}
			helper.AddTileSpritesToBatch(woodTexture, 0, tiles, Vector2.UnitY * -trunkHeightOffset, tileSize: treeTileSize);
		}
		internal void DrawDecorations(SpriteCompositionHelper helper, int frame, float cycleAngle)
		{
			int rootsTile = trunkHeight - 1;
			int branchesTile = (trunkHeight -1) / 2;
			Vector2 trunkOffset = Vector2.UnitY * -trunkHeightOffset;
			// roots
			int xMinusPadding = treeTileSize - 4;
			if (trunkTileRowToDraw >= rootsTile && rootsConfig != 1)
			{
				int xOffset = (xMinusPadding) * (rootsConfig == 0 ? 1 : -1);
				float yOffset = (treeTileSize * (trunkHeight -1)) / 2f;
				Vector2 rootOffset = new Vector2(xOffset, yOffset);
				(byte, byte)?[, , ] rootTile = {{{((byte)(rootsConfig == 0 ?  1 : 2), (byte)(6 + trunkTileRowToDraw%3))}}};
				helper.AddTileSpritesToBatch(woodTexture, 0, rootTile,  trunkOffset + rootOffset, tileSize: treeTileSize);
			}
			// branches
			if (trunkTileRowToDraw >= branchesTile && branchesConfig < 2)
			{
				int branchWidth = branchesTexture.Width / 2;
				int branchHeight = branchesTexture.Height / branchFrames;
				int branchPadding = branchesConfig == 0 ? -2 : 4;
				int xOffset = branchPadding + (xMinusPadding + branchWidth) / 2 * (branchesConfig == 0 ? 1 : -1);
				float yOffset = treeTileSize * (branchesTile) / 2f - branchHeight/2f;
				Vector2 branchOffset = new Vector2(xOffset, yOffset);
				Rectangle bounds = new Rectangle((1-branchesConfig) * branchWidth, branchHeight * (branchesTile % branchFrames), branchWidth, branchHeight);
				helper.AddSpriteToBatch(branchesTexture, bounds, trunkOffset + branchOffset, 0, 1);
			}

		}

		internal override void Draw(SpriteCompositionHelper helper, int frame, float cycleAngle)
		{
			if (animationFrame < spawnFrames)
			{
				return;
			}

			// first, draw folliage
			DrawTreeTop(helper, frame, cycleAngle);
			// then, draw trunk
			DrawTrunk(helper, frame, cycleAngle);
			// then, draw branches/roots
			DrawDecorations(helper, frame, cycleAngle);
		}
	}

	public class PalmTreeDrawer : TreeDrawer
	{
		public PalmTreeDrawer(
			Texture2D folliageTexture, 
			Texture2D woodTexture, 
			Texture2D branchesTexture, 
			Rectangle folliageBounds, 
			int trunkHeight = 4, 
			int decorationConfig = -1, 
			int branchFrames = 3, 
			int trunkHeadstartFrames = 0) : 
			base(folliageTexture, woodTexture, branchesTexture, folliageBounds, trunkHeight, decorationConfig, branchFrames, trunkHeadstartFrames)
		{
			// no roots or branches
			rootsConfig = 1;
			branchesConfig = 2;
		}

		internal override (byte, byte) TrunkTileForLocation(int row)
		{
			return ((byte)(row % 3), 0);
		}
	}


}
