﻿using AmuletOfManyMinions.Projectiles.Minions;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace AmuletOfManyMinions.Core.Minions.Pathfinding
{
	delegate void ModifyPath(ref Vector2 target);
	public class MinionPathfindingHelper
	{
		private Projectile projectile;
		internal PathfindingHelper pathfinder;
		// number of nodes to check against before starting from the beginning
		static int HOMING_LOS_CHECKS = 4;
		// minimum travel speed before we start thinking we're stuck
		static float NO_PROGRESS_THRESHOLD = 1.25f;
		internal int nodeIndex = -1;

		internal int noProgressFrames = 0;
		internal int unstuckFrames = 0;
		internal bool isStuck = false;
		internal bool atStart => nodeIndex == 0;
		internal ModifyPath modifyPath;

		// how close to a node we have to be before progressing to the next node
		internal int nodeProximity = 24;

		internal MinionPathfindingHelper(Projectile projectile)
		{
			this.projectile = projectile;
			
		}

		internal void SetPathStartingPoint()
		{
			// find the current node closest to the projectile
			List<Vector2> orderedNodes = pathfinder.orderedPath.OrderBy(node => Vector2.DistanceSquared(projectile.Center, node)).ToList();
			nodeIndex = 0;
			for(int i = 0; i < Math.Min(orderedNodes.Count, HOMING_LOS_CHECKS); i++)
			{
				if(Collision.CanHitLine(projectile.Center, 1, 1, orderedNodes[i], 1, 1))
				{
					// probably a better way to find the permuted index
					nodeIndex = pathfinder.orderedPath.IndexOf(orderedNodes[i]);
					break;
				}
			}
		}

		// in case we need to leave the path for eg. attacking an enemy
		internal void DetachFromPath()
		{
			nodeIndex = -1;
			noProgressFrames = 0;
			unstuckFrames = 0;
		}

		internal void GetUnstuck()
		{
			// sometimes it needs a bit of help
			if(nodeIndex <0 || nodeIndex > pathfinder.orderedPath.Count)
			{
				// weird state bail out and reset state
				noProgressFrames = 0;
				unstuckFrames = 0;
				isStuck = false;
				return;
			}
			Vector2 target = pathfinder.orderedPath[nodeIndex] - projectile.position;
			Vector2 gridTarget;
			Vector2[] checkPositions;
			if(Math.Abs(target.X) > Math.Abs(target.Y))
			{
				gridTarget = new Vector2(16 * Math.Sign(target.X), 0);
				if(target.X > 0)
				{
					checkPositions = new Vector2[] { projectile.TopRight, projectile.BottomRight};
				} else
				{
					checkPositions = new Vector2[] { projectile.TopLeft, projectile.BottomLeft};
				}
			} else
			{
				gridTarget = new Vector2(0, 16 * Math.Sign(target.Y));
				if(target.Y > 0)
				{
					checkPositions = new Vector2[] { projectile.BottomLeft, projectile.BottomRight};
				} else
				{
					checkPositions = new Vector2[] { projectile.TopLeft, projectile.TopRight};
				}
			}
			bool canHitNegative = Collision.CanHitLine(checkPositions[0], 1, 1, checkPositions[0] + gridTarget, 1, 1);
			bool canHitPositive = Collision.CanHitLine(checkPositions[1], 1, 1, checkPositions[1] + gridTarget, 1, 1);

			Vector2 unstuckDirection = default; 
			if(canHitNegative && !canHitPositive)
			{
				unstuckDirection = checkPositions[0] - projectile.Center;
			} else if (canHitPositive && !canHitNegative)
			{
				unstuckDirection = checkPositions[1] - projectile.Center;
			}
			if(unstuckDirection != default)
			{
				unstuckDirection.SafeNormalize();
				unstuckDirection *= 4;
				projectile.velocity = unstuckDirection;
			}

			unstuckFrames++;
			if(unstuckFrames > 5)
			{
				noProgressFrames = 0;
				unstuckFrames = 0;
				isStuck = false;
			}
		}

		internal Vector2? NextPathfindingTarget()
		{
			// initialize late to avoid any lifecycle issues
			if(pathfinder is null)
			{
				DetachFromPath();
				pathfinder = Main.player[projectile.owner]?.GetModPlayer<MinionPathfindingPlayer>()?.pHelper;
			}
			if(pathfinder.searchFailed || !pathfinder.searchActive)
			{
				DetachFromPath();
				return null;
			} else if (!pathfinder.pathFinalized)
			{
				DetachFromPath();
				// idle while the algorithm is still running
				return Vector2.Zero;
			}
			// simple approach: Go towards a node until you get close enough, then go to the next node
			List<Vector2> path = pathfinder.orderedPath;
			if(path.Count <= nodeIndex || nodeIndex < 0)
			{
				SetPathStartingPoint();
			}
			if(Vector2.DistanceSquared(projectile.Center, path[nodeIndex]) < nodeProximity * nodeProximity)
			{
				nodeIndex = Math.Min(path.Count-1, nodeIndex + 1);
			} 
			if(Math.Abs(projectile.velocity.Length()) < NO_PROGRESS_THRESHOLD)
			{
				noProgressFrames++;
			}
			if(noProgressFrames > 5)
			{
				isStuck = true;
			}
			// make sure the target exceeds a certain lenght threshold,
			// so the AI will speed up the minions
			Vector2 target = path[nodeIndex] - projectile.position;
			if(target.Length() < 16)
			{
				target.SafeNormalize();
				target *= 16;
			}
			modifyPath?.Invoke(ref target);
			return target;
		}
	}
}